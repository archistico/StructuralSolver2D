#!/usr/bin/env python3
"""Independent first-order elastic stress-region checks for D0M8.

For alpha=0 (plastic neutral axis outside the straight web on the nominal
compression side), NTC 2018 Table 4.2.III plastic web limits 36*eps/alpha
and 41.5*eps/alpha are not evaluated at alpha=0. This script does NOT
produce a section class, plastic resistance, buckling or member compliance.
"""

import json
import math
from fractions import Fraction as Q
from pathlib import Path

COUNT = 0


def check(condition, label):
    global COUNT
    if not condition:
        raise AssertionError(label)
    COUNT += 1


def near(a, b, tol=1e-11):
    return math.isclose(a, b, rel_tol=tol, abs_tol=tol)


def stress(n_kn, m_knm, depth_mm, area_mm2, iy_mm4):
    return -n_kn * 1000 / area_mm2 + abs(m_knm) * 1e6 * depth_mm / iy_mm4


def moment_for_zero_stress(n, area, iy, y):
    return n * 1000 * iy / (area * 1e6 * y)


def run():
    # Explicit NTC Table 4.2.III alpha<=0.5 formula: 0 is not its domain.
    check(Q(36) / Q(1, 2) == 72, "alpha=0.5 class1 plastic limit")
    check(Q(83, 2) / Q(1, 2) == 83, "alpha=0.5 class2 plastic limit")
    check(Q(36) / Q(1, 10) == 360, "alpha tends to zero: class1 grows")
    check(Q(83, 2) / Q(1, 10) == 415, "alpha tends to zero: class2 grows")
    for numerator in (Q(36), Q(83, 2)):
        check(numerator / Q(1, 1000) > numerator / Q(1, 100),
              "plastic limit increasing toward undefined alpha=0 endpoint")

    catalog = json.loads((Path(__file__).resolve().parents[3] /
        "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json").read_text(encoding="utf-8"))
    profiles = [x for x in catalog["sections"] if x["family"] in ("IPE", "HEA", "HEB")]
    check(len(profiles) == 30, "30 independently sourced nominal I/H profiles")
    for p in profiles:
        h,tw,tf,r = (p[k] for k in ("heightMm", "webThicknessMm",
                                      "flangeThicknessMm", "rootRadiusMm"))
        c = h-2*(tf+r)
        a = p["areaCm2"] * 100
        iy = p["strongInertiaCm4"] * 10000
        y_flange = h/2 - tf/2
        min_iy = 2*(p["widthMm"]*tf**3/12 + p["widthMm"]*tf*y_flange**2) + \
            tw*(h-2*tf)**3/12
        if not (min_iy * (1 - .001) <= iy < a*(h/2)**2):
            raise AssertionError(p["id"]+": impossible published gross inertia")
        nflat = 235*tw*c/1000
        n = nflat + 5
        mo = moment_for_zero_stress(n, a, iy, h/2)
        mw = moment_for_zero_stress(n, a, iy, c/2)
        no_outer = stress(n, mo*0.5, h/2, a, iy)
        no_web = stress(n, mo*0.5, c/2, a, iy)
        flange_outer = stress(n, (mo+mw)/2, h/2, a, iy)
        flange_web = stress(n, (mo+mw)/2, c/2, a, iy)
        web_outer = stress(n, 1.2*mw, h/2, a, iy)
        web_high = stress(n, 1.2*mw, c/2, a, iy)
        web_low = -n*1000/a - 1.2*mw*1e6*(c/2)/iy
        psi = web_low/web_high
        check(n > nflat and n < 235*a/1000,
              p["id"]+": alpha=0 physical plastic tension-dominant domain")
        check(mo < mw, p["id"]+": extreme flange compresses before flat web")
        check(no_outer < 0 and no_web < 0,
              p["id"]+": purely tensile nominal elastic cross-section")
        check(flange_outer > 0 and flange_web < 0,
              p["id"]+": compression confined to flange/root")
        check(web_outer > 0 and web_high > 0 and web_low < 0,
              p["id"]+": elastic compression reaches flat web")
        check(psi < -1 and near(psi, -11),
              p["id"]+": independent tensile psi special branch at 1.2 threshold")
        check(0 < web_high < 235 and 62*(1-psi)*math.sqrt(-psi) >= 124,
              p["id"]+": D0M3 special class3 bound/footnote stress alternative")

    p = next(x for x in profiles if x["id"] == "IPE-200")
    h = p["heightMm"]
    c = h - 2*(p["flangeThicknessMm"]+p["rootRadiusMm"])
    a = p["areaCm2"]*100
    iy = p["strongInertiaCm4"]*10000
    n = 220
    mo = moment_for_zero_stress(n,a,iy,h/2)
    mw = moment_for_zero_stress(n,a,iy,c/2)
    check(14 < mo < 16, "IPE200 first elastic compressed extreme threshold")
    check(18 < mw < 20, "IPE200 first elastic compressed web threshold")
    check(near(stress(n,mo,h/2,a,iy),0,1e-9), "exact elastic extreme boundary")
    check(near(stress(n,mw,c/2,a,iy),0,1e-9), "exact elastic flat-web boundary")
    for moment, expected in ((10,"none"),(17,"flange"),(20,"web"),(-20,"web")):
        outer = stress(n,moment,h/2,a,iy)
        web = stress(n,moment,c/2,a,iy)
        actual = "none" if outer<0 else "flange" if web<0 else "web"
        check(actual == expected, f"IPE200 N=+220 M={moment}: {expected}")

    m_large = 200
    comp = stress(n,m_large,c/2,a,iy)
    check(comp > 235, "large mixed moment: D0M3 footnote stress branch false")
    check(Q(5,1000) > Q(235,210000),
          "an externally proven tensile strain could satisfy alternative B")
    check(Q(235,210000) > Q(0), "footnote material strain ratio positive")

    # Neither all tensile elastic stresses nor a web <= class3 is a global
    # NTC class or a resistance proof. Domain transitions are explicit.
    for x in (-300,-220,0,100):
        check(not (x > 235*5.6*159/1000), f"non-alpha-zero example N={x}")

    if COUNT != 232:
        raise AssertionError(f"Expected 232 independently checked cases; got {COUNT}")
    print(f"P0-D0M8: {COUNT}/232 golden indipendenti PASS; "
          "solo regioni elastiche nominali alpha=0, non .NET/classi/resistenza/asta.")


if __name__ == "__main__":
    run()
