#!/usr/bin/env python3
"""P0-B1: reference arithmetic/negative cases; independent of the .NET solver.

The expected terms and totals are MANUALLY derived in the JSON golden, not
computed by an FEM model. This script evaluates them using exact decimals.
It does not validate the engineering judgment behind the favorable list,
nor does it prove an exhaustive normative combination set.
"""

from __future__ import annotations

import copy
import json
from decimal import Decimal
from pathlib import Path


REFERENCE = Path(__file__).with_name("P0B1_NTC2018_ACTION_GOLDEN.json")
FAMILIES = {"ULS_STR_A1", "SLS_RARE", "SLS_FREQUENT", "SLS_QUASI_PERMANENT"}


class GoldenError(ValueError):
    """Invalid action request or a discrepancy against independent golden."""


def d(value: str) -> Decimal:
    if not isinstance(value, str):
        raise GoldenError("Coefficients must be canonical decimal strings.")
    try:
        result = Decimal(value)
    except Exception as exc:
        raise GoldenError(f"Invalid decimal: {value!r}") from exc
    if not result.is_finite():
        raise GoldenError("Non-finite coefficient.")
    return result


def evaluate(reference: dict, case: dict) -> None:
    actions = reference["actions"]
    profile = case["family"]
    if profile not in FAMILIES:
        raise GoldenError(f"Unsupported combination: {profile}")

    present = case["present"]
    if len(present) != len(set(present)) or not present:
        raise GoldenError("Duplicate or empty action list.")
    if any(action_id not in actions for action_id in present):
        raise GoldenError("Unregistered action id.")
    if len(set(case["favorable"])) != len(case["favorable"]) or not set(case["favorable"]) <= set(present):
        raise GoldenError("Favorable list duplicates or references an absent action.")

    excluded_by_evidence = case.get("absentByEvidence", {})
    for action_id, explanation in excluded_by_evidence.items():
        if action_id in present or action_id not in actions or not str(explanation).strip():
            raise GoldenError("Physically absent action lacks valid evidence.")
    if "G1" in present and "G2" not in present and "G2" not in excluded_by_evidence:
        raise GoldenError("State why the example has no nonstructural G2 action.")

    groups: dict[str, str] = {}
    variables = []
    for action_id in present:
        action = actions[action_id]
        group = action.get("alternativeGroup")
        if group:
            if group in groups:
                raise GoldenError(f"Mutually exclusive group {group}: {groups[group]} and {action_id}")
            groups[group] = action_id
        if action["kind"] == "Q":
            category = action.get("category")
            if category in reference["unsupportedPsiWithoutExternalDetermination"] or category not in reference["psi"]:
                raise GoldenError(f"Variable category {category} needs external determination.")
            variables.append(action_id)
        elif action["kind"] not in {"G1", "G2"}:
            raise GoldenError("Unsupported action type.")

    leader = case["lead"]
    if profile == "SLS_QUASI_PERMANENT" and leader is not None:
        raise GoldenError("Quasi-permanent combination has no distinguished leader.")
    if profile != "SLS_QUASI_PERMANENT" and variables and leader not in variables:
        raise GoldenError("Leader must be one of the present variable actions.")
    if not variables and leader is not None:
        raise GoldenError("Permanent-only combination cannot have a variable leader.")

    actual_factors: dict[str, Decimal] = {}
    actual_omissions: dict[str, str] = {}
    for action_id in present:
        action = actions[action_id]
        kind = action["kind"]
        favorable = action_id in case["favorable"]
        if kind in {"G1", "G2"}:
            if profile == "ULS_STR_A1":
                factor = d(reference["gammaSTR_A1"][kind]["favorable" if favorable else "unfavorable"])
            else:
                factor = Decimal("1")
        elif favorable:
            factor = Decimal("0")
            actual_omissions[action_id] = "Favorable"
        else:
            psi0, psi1, psi2 = [d(x) for x in reference["psi"][action["category"]]]
            if profile == "ULS_STR_A1":
                factor = d(reference["gammaSTR_A1"]["Q"]["unfavorable"]) * (Decimal("1") if action_id == leader else psi0)
            elif profile == "SLS_RARE":
                factor = Decimal("1") if action_id == leader else psi0
            elif profile == "SLS_FREQUENT":
                factor = psi1 if action_id == leader else psi2
            else:
                factor = psi2
            if factor == 0:
                actual_omissions[action_id] = "PsiZero"

        if factor != 0:
            if factor < 0:
                raise GoldenError("Normative gamma*psi must not be negative; action effect carries its sign.")
            actual_factors[action_id] = factor

    expected_factors = {key: d(value) for key, value in case["factors"].items()}
    if any(value == 0 for value in expected_factors.values()):
        raise GoldenError("A zero FEM term is forbidden; record an omission instead.")
    if expected_factors != actual_factors:
        raise GoldenError(f"Factor mismatch: expected {expected_factors}, got {actual_factors}")
    if case["omitted"] != actual_omissions:
        raise GoldenError(f"Omission mismatch: expected {case['omitted']}, got {actual_omissions}")

    total = sum((d(actions[action_id]["effect"]) * factor for action_id, factor in actual_factors.items()), Decimal("0"))
    if total != d(case["expectedEffect"]):
        raise GoldenError(f"Expected scalar effect {case['expectedEffect']}, got {total}")


def run_negative_controls(reference: dict) -> int:
    """Mutate isolated fixtures; each must be detected, never modify the golden."""
    scenarios = {case["id"]: case for case in reference["cases"]}
    mutations = []

    altered = copy.deepcopy(reference)
    altered["gammaSTR_A1"]["G1"]["unfavorable"] = "1.4"
    mutations.append(("wrong gamma", altered, altered["cases"][0]))

    altered = copy.deepcopy(reference)
    altered["psi"]["WIND"][0] = "0.5"
    mutations.append(("wrong psi", altered, altered["cases"][0]))

    for title, fixture, change in [
        ("wrong leader", "B1-01-ULS-LEAD-A", lambda c: c.update(lead="WIND_PLUS")),
        ("zero model term", "B1-06-SLE-FREQ-LEAD-A", lambda c: c["factors"].update(WIND_PLUS="0")),
        ("unexplained omission", "B1-09-ULS-UPLIFT", lambda c: c["omitted"].clear()),
        ("both wind directions", "B1-01-ULS-LEAD-A", lambda c: c["present"].append("WIND_MINUS")),
        ("duplicate case", "B1-01-ULS-LEAD-A", lambda c: c["present"].append("QA")),
        ("no evidence for absent G2", "B1-10-ULS-G2-PHYSICALLY-ABSENT", lambda c: c.pop("absentByEvidence")),
        ("unknown snow elevation", "B1-01-ULS-LEAD-A", lambda c: None),
        ("unsupported roof category I", "B1-13-SLE-RARE-ROOF-H-LEAD", lambda c: None),
    ]:
        case = copy.deepcopy(scenarios[fixture])
        change(case)
        altered = copy.deepcopy(reference)
        if title == "unknown snow elevation":
            altered["actions"]["SNOW"]["category"] = "SNOW_UNSPECIFIED"
        elif title == "unsupported roof category I":
            altered["actions"]["QH"]["category"] = "I"
        mutations.append((title, altered, case))

    for title, altered, scenario in mutations:
        try:
            evaluate(altered, scenario)
        except GoldenError:
            continue
        raise AssertionError(f"Negative fixture erroneously accepted: {title}")
    return len(mutations)


def main() -> None:
    with REFERENCE.open("r", encoding="utf-8") as stream:
        golden = json.load(stream)
    ids = [case["id"] for case in golden["cases"]]
    if len(ids) != len(set(ids)):
        raise GoldenError("Duplicate golden case ID.")
    if golden["schema"] != "p0b1-independent-action-arithmetic-v1":
        raise GoldenError("Unsupported golden schema.")
    for case in golden["cases"]:
        evaluate(golden, case)
        print(f"PASS {case['id']:37s} effect={case['expectedEffect']}")
    n_negative = run_negative_controls(golden)
    print(f"P0-B1 reference arithmetic PASSED: {len(ids)} golden cases and {n_negative} negative controls.")
    print("No .NET/CLI gate, FEM result, full normative generator, or member adequacy verified.")


if __name__ == "__main__":
    main()
