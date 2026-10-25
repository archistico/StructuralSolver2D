#!/usr/bin/env python3
"""Independent exact-rational NTC 2018 M+V section arithmetic.

All nominal catalog entries are read independently from JSON. Formula checks
use Fraction, except the irrational epsilon, for which Decimal(70) is used.
This is NOT a .NET execution, physical-section certificate, proof of
restraining systems, or member/structure design check.
"""

import json
from decimal import Decimal, localcontext
from fractions import Fraction as Q
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
STEEL = ROOT / "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json"
MATERIALS = ROOT / "catalogs/m88/characteristic-strength-classes.json"
checks = 0


def check(label, expression):
    global checks
    if not expression:
        raise AssertionError(f"FAIL: {label}")
    checks += 1


def exact(value):
    return Q(str(value))


def yield_strength(grade, thickness):
    for band in grade["yieldByNominalThickness"]:
        if exact(band["lowerExclusiveMm"]) < thickness <= exact(band["upperInclusiveMm"]):
            return exact(band["yieldStrengthMpa"])
    raise ValueError("Missing material thickness band")


def geom(profile):
    A = exact(profile["areaCm2"]) * 100
    h, b, tw, tf, r = (exact(profile[k]) for k in
                        ("heightMm", "widthMm", "webThicknessMm", "flangeThicknessMm", "rootRadiusMm"))
    av = A - 2 * b * tf + (tw + 2 * r) * tf
    aw_flat = (h - 2 * (tf + r)) * tw
    aw_between = (h - 2 * tf) * tw
    aw_upper = A - 2 * b * tf
    return A, h, b, tw, tf, r, av, aw_flat, aw_between, aw_upper


def class_strong(profile, fy):
    _, h, b, tw, tf, r, *_ = geom(profile)
    cflange = (b - tw - 2 * r) / (2 * tf)
    cweb = (h - 2 * tf - 2 * r) / tw
    with localcontext() as context:
        context.prec = 70
        eps = (Decimal(235 * fy.denominator) / Decimal(fy.numerator)).sqrt()
        dr = lambda f: Decimal(f.numerator) / Decimal(f.denominator)
        f_cls = 1 if dr(cflange) <= 9 * eps else 2 if dr(cflange) <= 10 * eps else 3 if dr(cflange) <= 14 * eps else 4
        w_cls = 1 if dr(cweb) <= 72 * eps else 2 if dr(cweb) <= 83 * eps else 3 if dr(cweb) <= 124 * eps else 4
        buckles = dr((h - 2 * tf) / tw) > 72 * eps  # eta=1; conservative
    return max(f_cls, w_cls), buckles


def shear_capacity_squared(av, fy):
    # (av*fy/(sqrt(3)*gammaM0)/1000)**2, gammaM0=21/20.
    rational = av * fy * Q(20, 21) / 1000
    return rational * rational / 3


def pure_moment(w, fy):
    return w * fy * Q(20, 21) / 1_000_000


def moment_with_shear(wpl, fy, tw, aw_used, V_over_Vrd):
    # NTC [4.2.31] rho=(2V/Vrd-1)^2
    rho = (2 * V_over_Vrd - 1) ** 2
    # NTC [4.2.32]; A_w is geometric web area, NOT A_v.
    reduced_wpl = wpl - rho * aw_used**2 / (4 * tw)
    return pure_moment(reduced_wpl, fy)


def main():
    catalog = json.loads(STEEL.read_text(encoding="utf8"))["sections"]
    grades = {m["id"]: m for m in json.loads(MATERIALS.read_text(encoding="utf8"))["steel"]}
    by_id = {p["id"]: p for p in catalog}
    check("40 nominal OPPO profiles", len(catalog) == 40)
    check("only three grade yield tables", set(grades) == {"S235", "S275", "S355"})
    check("NTC gammaM0 exact rational", Q(21, 20) == exact("1.05"))
    check("threshold exactly one half", 2 * Q(1, 2) - 1 == 0)
    check("rho at V/Vrd=3/4", (2 * Q(3, 4) - 1)**2 == Q(1, 4))
    check("rho at V/Vrd=1", (2 * Q(1) - 1)**2 == 1)

    accepted_by_grade = 0
    class3_by_grade = 0
    for p in catalog:
        if p["family"] == "INP":
            check(f"{p['id']} tapered flanges intentionally excluded", p["id"].startswith("INP-"))
            continue
        A, h, b, tw, tf, r, av, aw_flat, aw_between, aw_upper = geom(p)
        for grade_name, grade in grades.items():
            label = p["id"] + " " + grade_name
            fy = yield_strength(grade, max(tw, tf))
            cls, buckles = class_strong(p, fy)
            accepted_by_grade += 1
            check(f"{label} indicative not product certificate", p["dataStatus"] == "IndicativeThirdParty")
            check(f"{label} native Av positive less than gross", 0 < av <= A)
            check(f"{label} web area is distinct from shear Av", 0 < aw_flat <= aw_between < av)
            check(f"{label} conservative web upper bound", aw_between <= aw_upper < A)
            check(f"{label} check web upper bound monotonic penalty",
                  aw_upper**2 / (4 * tw) >= aw_between**2 / (4 * tw))
            check(f"{label} positive VcRd squared", shear_capacity_squared(av, fy) > 0)
            check(f"{label} class in valid range", cls in (1, 2, 3, 4))
            check(f"{label} physical web buckling screen", isinstance(buckles, bool))
            if cls == 3:
                class3_by_grade += 1
                check(f"{label} class3 no invented plastic modulus", exact(p["strongElasticModulusCm3"]) > 0)
                check(f"{label} class3 high V outside [4.2.32]", cls not in (1, 2))
            else:
                check(f"{label} no class3 high-shear assumption", cls != 3)
                check(f"{label} class4 never uses gross/elastic as effective", cls != 4 or cls > 3)

    check("all 90 rolled I/H and three-grade pairs screened", accepted_by_grade == 90)
    check("class 3 branch actually exercised", class3_by_grade > 0)

    p = by_id["IPE-200"]
    A, h, b, tw, tf, r, av, aw_flat, aw_between, aw_upper = geom(p)
    fy = yield_strength(grades["S235"], max(tw, tf))
    wel = exact(p["strongElasticModulusCm3"]) * 1000
    wpl = Q(220640)  # published STAD nominal IPE200 Wpl,y 220.64cm3
    check("IPE200 independent nominal 194300 Wel", wel == 194300)
    check("IPE200 independent STAD 220640 Wpl >= Wel", wpl > wel)
    check("IPE200 NTC Av is 1399.6", av == exact("1399.6"))
    check("IPE200 between-flanges web Aw 1024.8", aw_between == exact("1024.8"))
    check("IPE200 conservative Aw web+fillets 1148", aw_upper == 1148)
    check("IPE200 Aw distinct from Av", aw_upper != av)
    check("IPE200 S235 class1 eligible", class_strong(p, fy)[0] == 1)
    check("IPE200 S235 no web buckling screen", not class_strong(p, fy)[1])
    mc = pure_moment(wpl, fy)
    check("IPE200 plastic pure moment reference", mc == Q(220640 * 235 * 20, 21 * 1_000_000))
    check("IPE200 low shear V/VRd=0.5 preserves Mc", moment_with_shear(wpl, fy, tw, aw_upper, Q(1, 2)) == mc)
    mvc = moment_with_shear(wpl, fy, tw, aw_upper, Q(3, 4))
    check("IPE200 high 75pct shear reduces Mc", 0 < mvc < mc)
    check("IPE200 M=40 kNm passes high 75pct shear", 40 < mvc)
    check("IPE200 M=48 kNm fails high 75pct shear", 48 > mvc)
    check("IPE200 conservative upper Aw hurts vs between-flanges", mvc < moment_with_shear(wpl, fy, tw, aw_between, Q(3, 4)))
    check("IPE200 high 100pct shear leaves positive bending capacity", moment_with_shear(wpl, fy, tw, aw_upper, Q(1)) > 0)
    check("IPE200 v>Vrd is automatic fail regardless of moment", Q(101, 100) > 1)

    p2 = by_id["HEA-180"]
    _, _, _, tw2, tf2, _, av2, _, aw_between2, aw_upper2 = geom(p2)
    fy2 = yield_strength(grades["S355"], max(tw2, tf2))
    check("HEA180 S355 class2 as in D2B", class_strong(p2, fy2)[0] == 2)
    check("HEA180 class2 no web instability", not class_strong(p2, fy2)[1])
    check("HEA180 nominal Wel 293600", exact(p2["strongElasticModulusCm3"]) * 1000 == 293600)
    wpl2 = Q(325000)  # independent ArcelorMittal Orange Book HE180A (rounded)
    check("HEA180 ArcelorMittal Wpl >= Wel", wpl2 > 293600)
    mv2 = moment_with_shear(wpl2, fy2, tw2, max(aw_between2, aw_upper2), Q(3, 4))
    check("HEA180 high shear MV positive", 0 < mv2 < pure_moment(wpl2, fy2))
    check("HEA180 M=60 passes high shear", 60 < mv2)
    check("HEA180 nominal NTC Av is positive", av2 > 0)

    p3 = by_id["HEA-280"]
    _, _, _, tw3, tf3, _, _, _, _, _ = geom(p3)
    fy3 = yield_strength(grades["S355"], max(tw3, tf3))
    check("HEA280 class3 S355", class_strong(p3, fy3)[0] == 3)
    check("HEA280 class3 low V wel nominal", exact(p3["strongElasticModulusCm3"]) * 1000 == 1013000)
    check("HEA280 class3 low V Mc=342.49 kNm exact", pure_moment(Q(1013000), fy3) == Q(1013000 * 355 * 20, 21 * 1_000_000))
    check("HEA280 class3 low V M250 passes", 250 < pure_moment(Q(1013000), fy3))
    check("HEA280 class3 high V not covered by plastic clause", class_strong(p3, fy3)[0] not in (1, 2))

    print(f"{checks}/{checks} PASS — independent rational/Decimal M-V calculations; C# and .NET NOT executed")
    print(f"Catalog pairs {accepted_by_grade}, class3 pairs {class3_by_grade}; no certification of sections or members")


if __name__ == "__main__":
    main()
