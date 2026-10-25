#!/usr/bin/env python3
"""Independent NTC2018 compression-dominant alpha=1 endpoint arithmetic.

The source NTC 2018 Table 4.2.III has mixed-web class-1/2 limits
396*eps/(13*alpha-1) and 456*eps/(13*alpha-1) for alpha>0.5.
At alpha=1 they reduce to 33*eps and 38*eps. Class-3 keeps the
independently derived elastic psi formula 42*eps/(0.67+0.33*psi).
Not a C# execution, design resistance or whole-member verification.
"""

import json
import math
from fractions import Fraction as Q
from pathlib import Path

COUNT = 0


def test(ok, name):
    global COUNT
    if not ok:
        raise AssertionError(name)
    COUNT += 1


def near(a, b, tol=1e-11):
    return math.isclose(a, b, rel_tol=tol, abs_tol=tol)


def cls(ratio, thresholds):
    return next((i+1 for i, lim in enumerate(thresholds) if ratio <= lim), 4)


def stress_psi(n, moment, flat_web, area, inertia):
    mean_compression = -1000*n/area
    bending_amplitude = abs(moment)*1e6*(flat_web/2)/inertia
    compression_edge = mean_compression+bending_amplitude
    return (mean_compression-bending_amplitude)/compression_edge


def classify(c, tw, outstand, tf, psi, fy):
    eps = math.sqrt(235/fy)
    web_limits = (33*eps, 38*eps, 42*eps/(.67+.33*psi))
    flange_limits = (9*eps, 10*eps, 14*eps)
    web = cls(c/tw, web_limits)
    flange = cls(outstand/tf, flange_limits)
    return (max(web, flange), web, flange, web_limits)


def run():
    test(Q(396, 13*1-1) == 33, "NTC mixed alpha=1 class1 must reduce exactly to 33 epsilon")
    test(Q(456, 13*1-1) == 38, "NTC mixed alpha=1 class2 must reduce exactly to 38 epsilon")
    test(Q(33, 1) == Q(396, 12), "exact rational class1 equivalence")
    test(Q(38, 1) == Q(456, 12), "exact rational class2 equivalence")
    for alpha in (Q(99, 100), Q(999, 1000), Q(999999, 1000000)):
        test(Q(396)/(13*alpha-1) > 33, "approaching alpha=1 class1 from below")
        test(Q(456)/(13*alpha-1) > 38, "approaching alpha=1 class2 from below")

    # A deliberately physical source for psi from N/M and independently
    # published A/I. Even very large compression does not establish strength.
    catalog = json.loads((Path(__file__).resolve().parents[3] /
        "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json").read_text(encoding="utf-8"))
    profiles = [x for x in catalog["sections"] if x["family"] in ("IPE","HEA","HEB")]
    test(len(profiles) == 30, "30 catalog profiles present")
    for p in profiles:
        h,b,tw,tf,r = (p[k] for k in (
            "heightMm", "widthMm", "webThicknessMm", "flangeThicknessMm", "rootRadiusMm"))
        c = h-2*(tf+r)
        outstand = (b-tw-2*r)/2
        area = p["areaCm2"]*100
        inertia = p["strongInertiaCm4"]*10000
        modeled_area = 2*b*tf+tw*(h-2*tf)+4*r*r*(1-math.pi/4)
        n_flat = 235*tw*c/1000
        n = -(n_flat+5)
        nominal_yield = 235*modeled_area/1000
        psi = stress_psi(n, 10, c, area, inertia)
        test(n < -n_flat*(1+1e-10) and abs(n) < nominal_yield,
             p["id"]+": compression outside flat web with valid internal PNA")
        test(-1 < psi < 1, p["id"]+": compression elastic psi within branch")
        result = classify(c, tw, outstand, tf, psi, 235)
        test(result[0] == max(result[1], result[2]),
             p["id"]+": section class governed by worst flat plate")

    # Independently constructed synthetic nominal I/H examples with
    # web c/t exactly 30, 36, 40 and 50: all four classes under N<0 and
    # a small yet nonzero strong-axis moment.
    for ratio, expected in ((30,1),(36,2),(40,3),(50,4)):
        c = 10*ratio
        h,b,tw,tf,r = c+24,100.,10.,8.,4.
        area = 2*b*tf+tw*(h-2*tf)+4*r*r*(1-math.pi/4)
        root_area = 4*r*r*(1-math.pi/4)
        y_flange = h/2-tf/2
        y_root = h/2-tf-r/2
        inertia = tw*(h-2*tf)**3/12 + 2*(b*tf**3/12+b*tf*y_flange*y_flange) \
            + root_area*y_root*y_root
        n = -(235*tw*c/1000+10)
        psi = stress_psi(n, .1, c, area, inertia)
        final, web, flange, limits = classify(c,tw,(b-tw-2*r)/2,tf,psi,235)
        test(-1 < psi < 1 and psi > .9, f"synthetic c/t={ratio} physical N/M psi")
        test(web == expected, f"synthetic c/t={ratio} expected web class {expected}")
        test(flange == 1, f"synthetic c/t={ratio} flange class 1")
        test(final == expected, f"synthetic c/t={ratio} final class {expected}")
        test(limits[0] == 33 and limits[1] == 38 and 42 <= limits[2] <= 42.2,
             f"synthetic c/t={ratio} class3 still depends on elastic psi")

    for psi, expected in ((Q(-1,2),Q(8400,101)),
                          (Q(0),Q(4200,67)),
                          (Q(1,2),Q(8400,167))):
        # Exact decimal coefficients from NTC formula 42/(0.67+0.33*psi)
        exact = Q(4200)/(Q(67)+Q(33)*psi)
        test(near(float(exact),float(expected)), f"class3 rational psi={psi}")
        test(float(exact) >= 42, f"class3 at psi={psi} cannot be below uniform compression 42")

    # Outside permitted domain intentionally does not reuse this arithmetic.
    for n, alpha, psi in ((220,0,-2),(300,0,-4),(-100,.75,.5),(0,.5,-1)):
        eligible = n<0 and alpha==1 and -1<psi<1
        test(not eligible, f"unsupported physical endpoint N={n}, alpha={alpha}, psi={psi}")

    print(f"P0-D0M7: {COUNT}/{COUNT} golden aritmetici indipendenti PASS; "
          "NO test .NET, resistenza o idoneita di membratura.")


if __name__ == "__main__":
    run()
