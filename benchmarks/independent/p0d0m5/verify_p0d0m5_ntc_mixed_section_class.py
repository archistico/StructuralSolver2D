#!/usr/bin/env python3
"""Independent exact-rational NTC 2018 I/H mixed N/M classifier reference.

No C# code, .NET assembly, or project outputs are imported. The arithmetic
follows NTC 2018 §4.2.3.1 Tab. 4.2.III/IV and a separate fully plastic axial
force-balance construction with its neutral axis strictly in the flat web.
Purely mathematical goldens do NOT verify member or section resistance.

Primary source: https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf
Indicative profile: https://www.oppo.it/tabelle/profilati_ipe.htm
"""
from fractions import Fraction as Q
from math import sqrt


passed = 0


def check(condition, label):
    global passed
    if not condition:
        raise AssertionError(label)
    passed += 1


def classify(c, tw, b, tf, r, area, inertia, fy, nkn, mknm,
             tensile_strain=None, e_mpa=None):
    assert c > 0 and tw > 0 and area > 0 and inertia > 0 and fy > 0
    if nkn == 0 or mknm == 0:
        return None
    n_limit = fy * tw * c / 1000
    if abs(nkn) >= n_limit:
        return None

    alpha = Q(1, 2) - 1000 * nkn / (2 * fy * tw * c)
    c_stress = -1000 * nkn / area
    b_stress = abs(mknm) * 1000000 * (c / 2) / inertia
    sigma_c = c_stress + b_stress
    sigma_opp = c_stress - b_stress
    if sigma_c <= 0:
        return None
    psi = sigma_opp / sigma_c
    if psi == -1 or psi >= 1:
        return None
    if (nkn < 0 and psi <= -1) or (nkn > 0 and psi >= -1):
        return None
    if psi < -1:
        stress_alternative = sigma_c <= fy
        strain_alternative = (tensile_strain is not None and e_mpa is not None
                              and e_mpa > 0 and tensile_strain > fy / e_mpa)
        if not (stress_alternative or strain_alternative):
            return None

    eps = sqrt(float(Q(235) / fy))
    if alpha > Q(1, 2):
        l1 = Q(396) * eps / (13 * float(alpha) - 1)
        l2 = Q(456) * eps / (13 * float(alpha) - 1)
    else:
        l1 = Q(36) * eps / float(alpha)
        l2 = Q(83, 2) * eps / float(alpha)
    l3 = (42 * eps / (0.67 + 0.33 * float(psi))
          if psi > -1 else 62 * eps * (1 - float(psi)) * sqrt(-float(psi)))
    if not (0 < l1 < l2 <= l3):
        return None
    wr = float(c / tw)
    fr = float((b - tw - 2 * r) / (2 * tf))
    wc = 1 if wr <= l1 else 2 if wr <= l2 else 3 if wr <= l3 else 4
    fc = 1 if fr <= 9 * eps else 2 if fr <= 10 * eps else 3 if fr <= 14 * eps else 4
    return dict(alpha=alpha, psi=psi, sigma_c=sigma_c, opposite=sigma_opp,
                web=wc, flange=fc, overall=max(wc, fc),
                class3_limit=l3, class2_limit=l2, n_limit=n_limit,
                needs_effective=max(wc, fc) == 4)


def synthetic(c):
    tw, tf, r, b, fy = Q(10), Q(8), Q(4), Q(100), Q(235)
    h = c + 2 * (tf + r)
    web_h = h - 2 * tf
    y_flange, y_root = h / 2 - tf / 2, h / 2 - tf - r / 2
    area = 2 * b * tf + tw * web_h + 14
    inertia = (tw * web_h ** 3 / 12 + 2 *
               (b * tf ** 3 / 12 + b * tf * y_flange ** 2) +
               14 * y_root ** 2)
    n = Q(-100)
    m = 3 * (-1000 * n / area) * inertia / (1000000 * c / 2)
    return classify(c, tw, b, tf, r, area, inertia, fy, n, m)


for c, expected in ((Q(600), 1), (Q(700), 2), (Q(800), 3), (Q(900), 4)):
    r = synthetic(c)
    check(r is not None, f"synthetic c={c} supported")
    check(r["psi"] == Q(-1, 2), f"c={c} exact elastic psi")
    check(r["web"] == expected, f"c={c} NTC web class")
    check(r["flange"] == 1, f"c={c} NTC flange class")
    check(r["overall"] == expected, f"c={c} governing class")
    check(r["needs_effective"] == (expected == 4), f"c={c} Aeff required")
    check(r["class2_limit"] <= r["class3_limit"], f"c={c} ordered limits")

# OPPO IPE 200: h=200, b=100, tw=5.6, tf=8.5, r=12,
# A=28.48 cm2, strong I=1943 cm4. S235, same N/M FEM station.
g = (Q(159), Q(56, 10), Q(100), Q(85, 10), Q(12),
     Q(2848), Q(1943) * 10000, Q(235))
compression = classify(*g, Q(-100), Q(10))
check(compression is not None, "IPE200 compressive mixed supported")
check(compression["alpha"] == Q(1, 2) + Q(100000) / (2 * Q(235) * Q(56, 10) * 159),
      "IPE200 exact plastic alpha")
check(compression["psi"] == Q(-8029, 105179), "IPE200 exact elastic psi")
check(compression["overall"] == 1, "IPE200 compression section class1")
check(compression["web"] == 1 and compression["flange"] == 1,
      "IPE200 two plate classes")
check(compression["alpha"] != compression["psi"], "plastic alpha not elastic psi")

tension = classify(*g, Q(100), Q(10))
check(tension is not None, "IPE200 tensile mixed supported without E/strain")
check(tension["alpha"] == 1 - compression["alpha"],
      "IPE200 same |N| plastic sign symmetry")
check(tension["psi"] < -1, "IPE200 tensile psi special branch")
check(tension["sigma_c"] <= Q(235), "IPE200 NTC stress-only OR footnote")
check(tension["overall"] == 1, "IPE200 tension section class1")

over = classify(*g, Q(100), Q(137, 2))
check(over is None, "sigmaC>fy with no external strain/E must fail")
over_with_strain = classify(*g, Q(100), Q(137, 2), Q(2, 1000), Q(210000))
check(over_with_strain is not None, "strain>fy/E independently supplied accepted")
check(over_with_strain["sigma_c"] > 235, "external strain path needed for sigmaC>fy")
check(over_with_strain["overall"] in (1, 2, 3, 4), "external strain yields class only")
check(classify(*g, Q(100), Q(137, 2), Q(235, 210000), Q(210000)) is None,
      "equal yield strain is not greater")

check(classify(*g, Q(100), Q(1)) is None, "tension without compressed edge unsupported")
check(classify(*g, Q(0), Q(10)) is None, "pure bending remains separate D0")
check(classify(*g, Q(-100), Q(0)) is None, "pure axial compression separate D0")
check(classify(*g, Q(-235 * Q(56, 10) * 159 / 1000), Q(10)) is None,
      "PNA tangent to fillets is unsupported")
check(classify(*g, Q(235 * Q(56, 10) * 159 / 1000), Q(10)) is None,
      "PNA tangent to opposite fillet unsupported")

print(f"P0-D0M5: {passed}/{passed} golden aritmetici indipendenti PASS; "
      "non sono test C#, resistenza di sezione o verifica di membratura.")
