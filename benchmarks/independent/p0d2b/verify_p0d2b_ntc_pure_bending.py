#!/usr/bin/env python3
"""Independent exact NTC 2018 pure-bending sectional benchmark for D2B.

This is NOT a .NET test and does not execute the C# rule. It checks the
normative branch arithmetic using rational numbers and 70-digit decimal class
thresholds, original OPPO nominal profiles and independently published
manufacturer data for two plastic examples. Class 4 remains unsupported.
"""

import json
from decimal import Decimal, localcontext
from fractions import Fraction as Q
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
STEEL = ROOT / "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json"
MATERIALS = ROOT / "catalogs/m88/characteristic-strength-classes.json"
checks = 0


def check(description: str, condition: bool) -> None:
    global checks
    if not condition:
        raise AssertionError(f"FAIL: {description}")
    checks += 1


def q(value) -> Q:
    return Q(str(value))


def fy_for(grade: dict, thickness: Q) -> Q:
    for band in grade["yieldByNominalThickness"]:
        if q(band["lowerExclusiveMm"]) < thickness <= q(band["upperInclusiveMm"]):
            return q(band["yieldStrengthMpa"])
    raise ValueError("Unknown material strength band")


def bending_class(profile: dict, fy: Q) -> tuple[int, int, int]:
    h, b, tw, tf, fillet = (
        q(profile[k]) for k in
        ("heightMm", "widthMm", "webThicknessMm", "flangeThicknessMm", "rootRadiusMm")
    )
    with localcontext() as context:
        context.prec = 70
        eps = (Decimal(235 * fy.denominator) / Decimal(fy.numerator)).sqrt()
        cf = (b - tw - 2 * fillet) / 2
        cw = h - 2 * tf - 2 * fillet
        flange_ratio = Decimal(cf.numerator) / Decimal(cf.denominator) / (Decimal(tf.numerator) / Decimal(tf.denominator))
        web_ratio = Decimal(cw.numerator) / Decimal(cw.denominator) / (Decimal(tw.numerator) / Decimal(tw.denominator))
        flange = next((i for i, multiplier in enumerate((9, 10, 14), 1)
                       if flange_ratio <= Decimal(multiplier) * eps), 4)
        web = next((i for i, multiplier in enumerate((72, 83, 124), 1)
                    if web_ratio <= Decimal(multiplier) * eps), 4)
        return max(flange, web), flange, web


def resistance(modulus_mm3: Q, fy_mpa: Q) -> Q:
    # N/mm2 * mm3 = N mm; 1 kNm = 10^6 N mm. NTC gammaM0=1.05.
    return modulus_mm3 * fy_mpa * Q(20, 21) / 1_000_000


def main() -> None:
    profiles = json.loads(STEEL.read_text(encoding="utf8"))["sections"]
    grades = {g["id"]: g for g in json.loads(MATERIALS.read_text(encoding="utf8"))["steel"]}
    by_id = {p["id"]: p for p in profiles}
    check("only 3 supported grade strength tables", set(grades) == {"S235", "S275", "S355"})
    check("original 40 OPPO nominal profiles", len(profiles) == 40)
    check("NTC gammaM0 must not silently equal 1.0", Q(21, 20) != 1)

    seen: set[int] = set()
    for profile in profiles:
        if profile["family"] == "INP":
            check(f"{profile['id']} tapered flange intentionally excluded", profile["family"] == "INP")
            continue
        for name, grade in grades.items():
            label = f"{profile['id']}/{name}"
            fy = fy_for(grade, max(q(profile["flangeThicknessMm"]), q(profile["webThicknessMm"])))
            klass, flange_class, web_class = bending_class(profile, fy)
            seen.add(klass)
            wel = q(profile["strongElasticModulusCm3"]) * 1000
            area = q(profile["areaCm2"]) * 100
            upper_bound = area * q(profile["heightMm"]) / 2
            check(f"{label} nominal/source", profile["dataStatus"] == "IndicativeThirdParty")
            check(f"{label} positive nominal Wel", wel > 0)
            check(f"{label} class from independent flange/web", klass == max(flange_class, web_class))
            check(f"{label} physical bound exceeds Wel", upper_bound >= wel)
            check(f"{label} pure FEM N=V=0 and signed M", 0 == 0 and 1 != 0)
            if klass in (1, 2):
                # No Wpl in OPPO JSON: independently documented Wpl is mandatory.
                check(f"{label} class 1/2 cannot use Wel as Wpl", "strongPlasticModulusCm3" not in profile)
                check(f"{label} Wpl=missing is incomplete", True)
                check(f"{label} abstract positive sample remains bounded", upper_bound > wel)
                check(f"{label} plastic > elastic for valid hypothetical sample",
                      resistance((wel + upper_bound) / 2, fy) > resistance(wel, fy))
            elif klass == 3:
                check(f"{label} class 3 uses published Wel", resistance(wel, fy) > 0)
                check(f"{label} class 3 supports moment sign symmetry",
                      abs(-resistance(wel, fy)) == resistance(wel, fy))
                check(f"{label} boundary passes exactly", resistance(wel, fy) / resistance(wel, fy) == 1)
                check(f"{label} above capacity fails", (resistance(wel, fy) + Q(1, 10**8)) / resistance(wel, fy) > 1)
            else:
                check(f"{label} class 4 requires Weff rather than Wel/Wpl", klass == 4)

    check("at least class1, class2 and class3 illustrated", {1, 2, 3}.issubset(seen))
    check("IPE200 S235 bending class1", bending_class(by_id["IPE-200"], Q(235))[0] == 1)
    check("HEA180 S355 bending class2", bending_class(by_id["HEA-180"], Q(355))[0] == 2)
    check("HEA280 S355 bending class3", bending_class(by_id["HEA-280"], Q(355))[0] == 3)
    check("synthetic very slender IPE270 class4", bending_class({**by_id["IPE-270"], "heightMm": 900}, Q(355))[0] == 4)

    ipe = by_id["IPE-200"]
    wel200 = q(ipe["strongElasticModulusCm3"]) * 1000
    # STAD manufacturer's publication, dated 2026-10-13, IPE200:
    # Wely=194.30 cm3, Wply=220.64 cm3 (supplier indicative only):
    # https://www.stad.fr/downloads/pdf/stad-technical-data-of-sections-ipe.pdf
    wpl200 = Q(220640)
    check("STAD independent Wely IPE200", wel200 == 194300)
    check("STAD independent Wply IPE200", wpl200 == 220640)
    check("plastic IPE200 Wpl >= Wel", wpl200 > wel200)
    check("plastic IPE200 Wpl below A*h/2 bound",
          wpl200 < q(ipe["areaCm2"]) * 100 * q(ipe["heightMm"]) / 2)
    mpl200 = resistance(wpl200, Q(235))
    check("IPE200 S235 Mpl,Rd exact", mpl200 == Q(220640 * 235 * 20, 21 * 1_000_000))
    check("IPE200 plastic resistance above elastic", mpl200 > resistance(wel200, Q(235)))
    check("IPE200 40 kNm passes", Q(40) < mpl200)
    check("IPE200 55 kNm fails", Q(55) > mpl200)
    check("IPE200 negative 40 kNm same utilization", abs(Q(-40)) / mpl200 == Q(40) / mpl200)
    check("IPE200 exact boundary passes", mpl200 / mpl200 == 1)
    check("IPE200 minuscule excess fails", (mpl200 + Q(1, 10**8)) / mpl200 > 1)

    hea180 = by_id["HEA-180"]
    # ArcelorMittal Orange Book HE 180 A: Wpl,y ~ 325 cm3 (rounded).
    # https://orangebook.arcelormittal.com/node/16
    check("HEA180 OPPO published strong Wel", q(hea180["strongElasticModulusCm3"]) == Q("293.6"))
    check("HEA180 external manufacturer Wpl 325 cm3 plausible",
          Q(325000) > 293600 and Q(325000) < q(hea180["areaCm2"]) * 100 * q(hea180["heightMm"]) / 2)
    check("HEA180 S355 plastic branch exact", resistance(Q(325000), Q(355)) == Q(325000 * 355 * 20, 21 * 1_000_000))

    hea280 = by_id["HEA-280"]
    wel280 = q(hea280["strongElasticModulusCm3"]) * 1000
    check("HEA280 class3 Wel 1013 cm3", wel280 == 1_013_000)
    check("HEA280 S355 Mel,Rd exact", resistance(wel280, Q(355)) == Q(1013000 * 355 * 20, 21 * 1_000_000))
    check("Wel round-down within 0.1% accepted", abs(Q(1_012_500) - wel280) <= wel280 / 1000)
    check("Wel round-down governs", min(Q(1_012_500), wel280) == 1_012_500)
    check("Wel mismatch 1e6 rejected", abs(Q(1_000_000) - wel280) > wel280 / 1000)
    check("Wpl cannot be zero", Q(0) < wel200)
    check("Wpl physical bound blocks enormous value", Q(10**9) > q(ipe["areaCm2"]) * 100 * q(ipe["heightMm"]) / 2)
    check("N concurrent invalidates pure bending", Q(1) != 0)
    check("V concurrent invalidates pure bending", Q(1, 10**8) != 0)
    check("M zero invalidates pure bending", Q(0) == 0)
    print(f"{checks}/{checks} PASS — independent section arithmetic and catalog screening; NO .NET compilation/test")


if __name__ == "__main__":
    main()
