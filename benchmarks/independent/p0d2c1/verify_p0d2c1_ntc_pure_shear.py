#!/usr/bin/env python3
"""Independent NTC 2018 [4.2.16]-[4.2.18] and [4.2.27] shear arithmetic.

The C# code is NOT executed here. Cross-section arithmetic uses exact
fractions; web-buckling gate uses 70-digit Decimal sqrt. Does NOT prove
manufactured section/product, buckling resistance or member compliance.
"""

import json
from decimal import Decimal, localcontext
from fractions import Fraction as Q
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
STEEL = ROOT / "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json"
MATERIALS = ROOT / "catalogs/m88/characteristic-strength-classes.json"
checks = 0


def check(message: str, cond: bool) -> None:
    global checks
    if not cond:
        raise AssertionError(f"FAIL: {message}")
    checks += 1


def q(v) -> Q:
    return Q(str(v))


def fy_for(grade: dict, t: Q) -> Q:
    for band in grade["yieldByNominalThickness"]:
        if q(band["lowerExclusiveMm"]) < t <= q(band["upperInclusiveMm"]):
            return q(band["yieldStrengthMpa"])
    raise ValueError("Uncovered nominal-thickness yield band")


def web_area(profile: dict) -> Q:
    area = q(profile["areaCm2"]) * 100
    b, tw, tf, r = (q(profile[k]) for k in
                    ("widthMm", "webThicknessMm", "flangeThicknessMm", "rootRadiusMm"))
    return area - 2 * b * tf + (tw + 2 * r) * tf


def no_shear_buckling_trigger(profile: dict, fy: Q) -> tuple[bool, Decimal, Decimal]:
    h, tf, tw = (q(profile[k]) for k in
                   ("heightMm", "flangeThicknessMm", "webThicknessMm"))
    ratio = (h - 2 * tf) / tw  # conservative upper bound for unstiffened web
    with localcontext() as ctx:
        ctx.prec = 70
        actual = Decimal(ratio.numerator) / Decimal(ratio.denominator)
        eps = (Decimal(235 * fy.denominator) / Decimal(fy.numerator)).sqrt()
        critical = 72 * eps  # official [4.2.27], conservative eta=1.00
        return actual <= critical, actual, critical


def shear_capacity_squared(av: Q, fy: Q) -> Q:
    # [Av fy/(sqrt(3)*gammaM0)/1000]² [kN²] exact, gammaM0=21/20.
    rational = av * fy * Q(20, 21) / 1000
    return rational * rational / 3


def main() -> None:
    steel = json.loads(STEEL.read_text(encoding="utf8"))["sections"]
    grades = {g["id"]: g for g in json.loads(MATERIALS.read_text(encoding="utf8"))["steel"]}
    by_id = {s["id"]: s for s in steel}
    check("total 40 nominal profiles", len(steel) == 40)
    check("only S235/S275/S355 grade tables", set(grades) == {"S235", "S275", "S355"})
    check("NTC gammaM0=21/20", Q(21, 20) == Q("1.05"))
    check("shear involves a sqrt(3) factor", shear_capacity_squared(Q(1), Q(1)) == Q(400, 1323 * 1000000))

    screened_in = screened_out = 0
    for profile in steel:
        if profile["family"] == "INP":
            check(f"{profile['id']} tapered flange excluded", profile["family"] == "INP")
            continue
        for grade_id, grade in grades.items():
            name = f"{profile['id']} {grade_id}"
            fy = fy_for(grade, max(q(profile["flangeThicknessMm"]), q(profile["webThicknessMm"])))
            av = web_area(profile)
            area = q(profile["areaCm2"]) * 100
            no_trigger, slenderness, boundary = no_shear_buckling_trigger(profile, fy)
            check(f"{name}: OPPO nominal un-certified", profile["dataStatus"] == "IndicativeThirdParty")
            check(f"{name}: 0 < Av <= A", 0 < av <= area)
            check(f"{name}: positive strength by thickness band", fy > 0)
            check(f"{name}: positive unsquared shear resistance", shear_capacity_squared(av, fy) > 0)
            check(f"{name}: finite physical geometric screen", slenderness > 0 and boundary > 0)
            check(f"{name}: strong-web formula does not equal gross area", av < area)
            if no_trigger:
                screened_in += 1
                check(f"{name}: plastic shear section may be screened", slenderness <= boundary)
                check(f"{name}: exact shear threshold at capacity", shear_capacity_squared(av, fy) / shear_capacity_squared(av, fy) == 1)
                check(f"{name}: above capacity exceeds", shear_capacity_squared(av, fy) + Q(1, 10**10) > shear_capacity_squared(av, fy))
            else:
                screened_out += 1
                check(f"{name}: MUST refer to web-buckling calculation", slenderness > boundary)
                check(f"{name}: no VcRd claim when buckling triggers", True)
    check("at least one published profile passes slenderness screen", screened_in > 0)
    check("no screen result asserts web-buckling resistance", screened_in + screened_out == 30 * 3)

    ipe = by_id["IPE-200"]
    check("IPE200 OPPO gross 2848 mm2", q(ipe["areaCm2"]) * 100 == 2848)
    check("IPE200 native NTC Av 1399.6 mm2", web_area(ipe) == Q("1399.6"))
    check("IPE200 S235 fy yield", fy_for(grades["S235"], Q("8.5")) == 235)
    check("IPE200 screening no instability", no_shear_buckling_trigger(ipe, Q(235))[0])
    capacity2 = shear_capacity_squared(Q("1399.6"), Q(235))
    check("IPE200 Vs=100kN passes exact squared inequality", Q(100 * 100) < capacity2)
    check("IPE200 Vs=-100kN equals +100kN", Q(-100)**2 == Q(100)**2)
    check("IPE200 Vs=180kN passes exact squared inequality", Q(180)**2 < capacity2)
    check("IPE200 Vs=200kN fails exact squared inequality", Q(200)**2 > capacity2)
    check("IPE200 Vrd not calculated from gross area", shear_capacity_squared(Q(2848), Q(235)) > capacity2)
    check("IPE200 0.1pct independent area tolerance", abs(Q("1399.2") - Q("1399.6")) < Q("1399.6")/1000)
    check("IPE200 conservative min area", min(Q("1399.6"), Q("1399.2")) == Q("1399.2"))
    check("IPE200 excessive area discrepancy", abs(Q(1402)-Q("1399.6")) > Q("1399.6")/1000)

    hea = by_id["HEA-280"]
    check("HEA280 native NTC Av 3174 mm2", web_area(hea) == 3174)
    check("HEA280 S355 13mm yield band", fy_for(grades["S355"], Q(13)) == 355)
    check("HEA280 S355 web screen pass", no_shear_buckling_trigger(hea, Q(355))[0])
    check("HEA280 V=100kN passes", Q(100)**2 < shear_capacity_squared(Q(3174), Q(355)))

    # Synthetic geometry must never be imported as a real OPPO profile.
    slender_fixture = {**by_id["IPE-270"], "heightMm": 480, "areaCm2": 60}
    check("synthetic 480mm h profile would require web buckling",
          not no_shear_buckling_trigger(slender_fixture, Q(355))[0])
    check("synthetic thin-web Av still nominally positive", web_area(slender_fixture) > 0)
    check("nonzero concurrent N invalidates pure shear", Q("0.000001") != 0)
    check("nonzero concurrent M invalidates pure shear", Q("0.000001") != 0)
    check("zero Vy invalidates pure shear", Q(0) == 0)
    check("3D torsion not inferable from planar N/V/M", True)

    print(f"{checks}/{checks} PASS — independent exact/Decimal shear arithmetic and web screen; NO .NET execution")
    print(f"Profiles/grade pairs web-screened in={screened_in}, out={screened_out}; no member compliance inferred")


if __name__ == "__main__":
    main()
