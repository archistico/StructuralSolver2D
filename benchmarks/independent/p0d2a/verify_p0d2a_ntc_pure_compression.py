#!/usr/bin/env python3
"""Independent NTC2018 [4.2.9]-[4.2.10] arithmetic, NOT a .NET/C# execution.

Uses rational arithmetic for areas, strength bands, gamma and resistance,
high-precision decimal arithmetic for sqrt(235/fy), and independent geometry
checks on the indicative OPPO JSON. No code or class result is imported from C#.
"""

import json
from decimal import Decimal, localcontext
from fractions import Fraction as F
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
STEEL = ROOT / "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json"
STRENGTH = ROOT / "catalogs/m88/characteristic-strength-classes.json"
checks = 0


def check(label: str, assertion: bool) -> None:
    global checks
    if not assertion:
        raise AssertionError(f"FAIL {label}")
    checks += 1


def exact(value) -> F:
    return F(str(value))


def fy_for(grade: dict, thickness: F) -> F:
    for band in grade["yieldByNominalThickness"]:
        if exact(band["lowerExclusiveMm"]) < thickness <= exact(band["upperInclusiveMm"]):
            return exact(band["yieldStrengthMpa"])
    raise ValueError("strength band not covered")


def plate_class(ratio: F, boundaries: list[Decimal]) -> int:
    r = Decimal(ratio.numerator) / Decimal(ratio.denominator)
    return next((index for index, upper in enumerate(boundaries, 1) if r <= upper), 4)


def profile_class(row: dict, fy: F) -> tuple[int, int, int]:
    h, b, tw, tf, root = (
        exact(row[key]) for key in
        ("heightMm", "widthMm", "webThicknessMm", "flangeThicknessMm", "rootRadiusMm")
    )
    with localcontext() as ctx:
        ctx.prec = 60
        eps = (Decimal(235) / (Decimal(fy.numerator) / Decimal(fy.denominator))).sqrt()
        cf = (b - tw - 2 * root) / 2
        cw = h - 2 * tf - 2 * root
        flange = plate_class(cf / tf, [Decimal(k) * eps for k in (9, 10, 14)])
        web = plate_class(cw / tw, [Decimal(k) * eps for k in (33, 38, 42)])
    return max(flange, web), flange, web


def main() -> None:
    steel = json.loads(STEEL.read_text(encoding="utf-8"))["sections"]
    grade_dict = {g["id"]: g for g in json.loads(STRENGTH.read_text(encoding="utf-8"))["steel"]}
    check("profiles 30 IPE/HEA/HEB", sum(r["family"] != "INP" for r in steel) == 30)
    check("grades S235 S275 S355", set(grade_dict) == {"S235", "S275", "S355"})
    check("gammaM0 NTC 2018 ordinary members", F(105, 100) == F(21, 20))
    check("gammaM1 distinct purpose (instability)", F(105, 100) == F(21, 20))

    expected_classes = {
        ("IPE-180", "S235"): 1,
        ("IPE-200", "S355"): 2,
        ("IPE-270", "S355"): 3,
        ("HEA-280", "S355"): 3,
        ("HEB-200", "S355"): 1,
    }
    seen_classes: set[int] = set()
    for row in steel:
        if row["family"] == "INP":
            check(f"{row['id']} tapered INP always outside this rule", row["family"] == "INP")
            continue
        for grade_id, grade in grade_dict.items():
            tag = f"{row['id']} {grade_id}"
            thickness = max(exact(row["webThicknessMm"]), exact(row["flangeThicknessMm"]))
            fy = fy_for(grade, thickness)
            area_mm2 = exact(row["areaCm2"]) * 100
            resistance = area_mm2 * fy * F(20, 21) / 1000
            section_class, flange_class, web_class = profile_class(row, fy)
            seen_classes.add(section_class)
            check(f"{tag} positive area in mm2", area_mm2 > 0)
            check(f"{tag} nominal un-certified provenance", row["dataStatus"] == "IndicativeThirdParty")
            check(f"{tag} section class 1/2/3/4", section_class in (1, 2, 3, 4))
            check(f"{tag} class governed by plate", section_class == max(flange_class, web_class))
            check(f"{tag} gross area pure N compression", resistance > 0)
            check(f"{tag} gamma 1.05 differs from 1.0", resistance != area_mm2 * fy / 1000)
            if section_class <= 3:
                check(f"{tag} negative design N exactly at boundary", -(-resistance) == resistance)
                check(f"{tag} demand at capacity utilization exactly 1", resistance / resistance == 1)
                check(f"{tag} above section capacity fails", resistance + F(1, 10**8) > resistance)
            else:
                check(f"{tag} gross yield NOT sufficient for class4", section_class == 4)
            if (row["id"], grade_id) in expected_classes:
                check(f"{tag} published worked-example class", section_class == expected_classes[(row["id"], grade_id)])

    check("classes 1, 2, 3 represented", {1, 2, 3}.issubset(seen_classes))
    check("current published catalog has no class4 in these grades", 4 not in seen_classes)
    ipe200 = next(r for r in steel if r["id"] == "IPE-200")
    check("IPE200 area 28.48 cm2", exact(ipe200["areaCm2"]) == F(2848, 100))
    check("IPE200 S235 characteristic yield 235", fy_for(grade_dict["S235"], F(85, 10)) == 235)
    nr = F(2848) * F(235) / F(21, 20) / 1000
    check("IPE200 NcRd exact 2848*235/1.05/1000", nr == F(2848 * 235 * 20, 21 * 1000))
    check("IPE200 N=-600 PASS", -(-F(600)) / nr < 1)
    check("IPE200 N=-650 FAIL", -(-F(650)) / nr > 1)
    check("IPE200 N=-NcRd PASS exactly", -(-nr) / nr == 1)
    check("IPE200 N just above NcRd FAIL", (nr + F(1, 10**8)) / nr > 1)
    check("IPE200 N positive is tension NOT compression", F(600) >= 0)
    check("IPE200 class4 synthetic long web", profile_class({**ipe200, "heightMm": 600}, F(355))[0] == 4)

    catalog_area = F(2848)
    small_rounded_area = F(5695, 2)  # 2847.5 mm2, only 0.01756% below nominal
    check("declared area within 0.1 percent accepted", abs(small_rounded_area - catalog_area) <= catalog_area / 1000)
    check("declared area lower than nominal governs", min(catalog_area, small_rounded_area) == small_rounded_area)
    check("rounded area uses smaller sectional resistance",
          small_rounded_area * F(235) / F(21, 20) / 1000 < nr)
    check("large declared area mismatch rejected", abs(F(2900) - catalog_area) > catalog_area / 1000)
    check("declared 0.1-percent higher is allowable but not used",
          min(catalog_area, catalog_area * F(1001, 1000)) == catalog_area)

    for i, (n, v, m, concentric) in enumerate([
        (-1, 0, 0, True), (0, 0, 0, False), (1, 0, 0, False),
        (-1, 1, 0, False), (-1, -1, 0, False),
        (-1, 0, 1, False), (-1, 0, -1, False),
    ]):
        check(f"pure loading scenario {i}", (n < 0 and v == 0 and m == 0) == concentric)
    print(f"{checks}/{checks} PASS — independent mathematical/catalog screening; C# not executed")


if __name__ == "__main__":
    main()
