#!/usr/bin/env python3
"""Independent D0M6 geometry checks: numerical integration vs C# circular primitives.

This is NOT a .NET test, a design-resistance calculation, nor any member check.
"""

import json
import math
from pathlib import Path


CHECKS = 0


def check(condition, reason):
    global CHECKS
    if not condition:
        raise AssertionError(reason)
    CHECKS += 1


def close(actual, expected, tol=1e-8):
    return math.isclose(actual, expected, rel_tol=tol, abs_tol=tol)


def dimensions(record):
    return tuple(record[key] for key in (
        "heightMm", "widthMm", "webThicknessMm", "flangeThicknessMm", "rootRadiusMm"
    ))


def width_from_compression_face(z, h, b, tw, tf, r):
    """Independent piecewise width at z from lower compression-side flange."""
    depth = min(z, h-z)
    if depth <= tf:
        return b
    if depth >= tf+r:
        return tw
    t = depth-tf
    return tw + 2*(r - math.sqrt(max(0, r*r - (r-t)*(r-t))))


def integrate_piecewise_width(low, high, dims, steps=2000):
    """Angle-transformed numerical quadrature; no C# circle primitive is used.

    The fillet corner has a vertical tangent, so ordinary midpoint sampling
    in z converges poorly. Substituting t=r*cos(theta) smooths its integrand.
    """
    h, b, tw, tf, r = dims

    def corner_integral(u):
        theta_max = math.acos(max(-1.0, min(1.0, 1-u/r)))
        step = theta_max/steps
        return r*r*step*sum(
            (1-math.sin((i+.5)*step))*math.sin((i+.5)*step)
            for i in range(steps)
        )

    half = b*tf + tw*(h/2-tf) + 2*corner_integral(r)

    def cumulative(depth):
        if depth > h/2:
            return 2*half-cumulative(h-depth)
        if depth <= tf:
            return b*depth
        if depth <= tf+r:
            u = depth-tf
            return b*tf+tw*u+2*corner_integral(u)
        return b*tf+tw*(depth-tf)+2*corner_integral(r)

    return cumulative(high)-cumulative(low)


def invert_area_by_quadrature(force_kn, dims, fy=235):
    h, b, tw, tf, r = dims
    area = integrate_piecewise_width(0, h, dims)
    target = (area - 1000*force_kn/fy)/2
    if not 0 < target < area:
        return None
    lo, hi = 0.0, h
    for _ in range(56):
        mid = (lo+hi)/2
        if integrate_piecewise_width(0, mid, dims, steps=3000) < target:
            lo = mid
        else:
            hi = mid
    return (lo+hi)/2


def region(depth, dims):
    h, b, tw, tf, r = dims
    if depth < tf:
        return "compression flange"
    if depth < tf+r:
        return "compression fillet"
    if depth <= h-tf-r:
        return "straight web"
    if depth <= h-tf:
        return "tension fillet"
    return "tension flange"


def run():
    directory = Path(__file__).resolve().parents[3]
    data = json.loads((directory / "catalogs/m88/steel-oppo-ipe-hea-heb-inp.json").read_text(encoding="utf-8"))
    profiles = {s["id"]: s for s in data["sections"]
                if s["family"] in ("IPE", "HEA", "HEB")}
    check(len(profiles) == 30, "Expected 30 source-checked nominal IPE/HEA/HEB rows")

    max_deviation = (None, 0.0)
    for name, record in profiles.items():
        h, b, tw, tf, r = dimensions(record)
        geometric = 2*b*tf+tw*(h-2*tf)+4*r*r*(1-math.pi/4)
        published = record["areaCm2"]*100
        deviation = abs(geometric-published)/geometric
        check(deviation < 0.001, f"{name} nominal root area does not match published A")
        if deviation > max_deviation[1]:
            max_deviation = (name, deviation)
    check(max_deviation[1] < 0.0005, "OPPO maximum geometric discrepancy should remain below 0.05%")

    d = dimensions(profiles["IPE-200"])
    h, b, tw, tf, r = d
    c = h-2*(tf+r)
    model_a = 2*b*tf+tw*(h-2*tf)+4*r*r*(1-math.pi/4)
    quadrature_a = integrate_piecewise_width(0, h, d)
    check(close(quadrature_a, model_a, 3e-6), "Analytic nominal area disagrees with independent quadrature")

    # The straight-web result is independent of the fillet area because
    # the opposite, symmetric half areas cancel in axial force balance.
    for n in (-100, 100):
        depth = invert_area_by_quadrature(n, d)
        exact_depth = h/2-1000*n/(2*235*tw)
        alpha = (depth-tf-r)/c
        exact_alpha = .5-1000*n/(2*235*tw*c)
        check(close(depth, exact_depth, 1e-5), f"n={n}: flat NA mismatch")
        check(close(alpha, exact_alpha, 1e-6), f"n={n}: flat-web alpha mismatch")
        check(region(depth, d) == "straight web", f"n={n}: incorrect flat-web region")

    coordinates = {}
    for n, expected_region, expected_alpha in (
        (-220, "tension fillet", 1.0),
        (-300, "tension flange", 1.0),
        (220, "compression fillet", 0.0),
        (300, "compression flange", 0.0),
    ):
        depth = invert_area_by_quadrature(n, d)
        coordinates[n] = depth
        check(region(depth, d) == expected_region, f"n={n}: wrong fillet/flange")
        check(close(max(0, min(1, (depth-tf-r)/c)), expected_alpha), f"n={n}: alpha")
        check(close(integrate_piecewise_width(0, depth, d),
                    (quadrature_a-1000*n/235)/2, 2e-5), f"n={n}: axial equilibrium")
        check(abs(depth-h/2) > c/2, f"n={n}: not outside flat web")
        if abs(n) == 300:
            # Independent closed form, valid only when PNA lies within a flange.
            analytical = (h-(model_a+1000*n/235)/(2*b)) if n < 0 else \
                (model_a-1000*n/235)/(2*b)
            check(close(depth, analytical, 1e-5), f"n={n}: independent flange formula")

    for n in (220, 300):
        check(close(coordinates[-n]+coordinates[n], h, 2e-5),
              f"n={n}: tension/compression symmetry broken")

    n_flat = 235*tw*c/1000
    for sign in (-1, +1):
        boundary = invert_area_by_quadrature(sign*n_flat, d)
        check(close(boundary, tf+r if sign > 0 else h-tf-r, 1e-5),
              "Flat-web/fillet transition should be continuous")
    # Use the SAME independently integrated area in the endpoint test to
    # avoid interpreting sub-micrometric quadrature roundoff as extra metal.
    n_yield = 235*quadrature_a/1000
    check(invert_area_by_quadrature(n_yield, d) is None,
          "Gross tensile plastic yield is outside internal NA domain")
    check(invert_area_by_quadrature(-n_yield, d) is None,
          "Gross compressive plastic yield is outside internal NA domain")

    print(f"P0-D0M6: {CHECKS}/{CHECKS} golden geometrici indipendenti PASS; "
          f"max delta area OPPO {max_deviation[0]} {max_deviation[1]*100:.5f}%; "
          "NON sono test C# o verifiche di resistenza.")
    for n in (-300, -220, 220, 300):
        print(f"IPE200 N={n:+g} kN: NA depth from compression face="
              f"{coordinates[n]:.8f} mm ({region(coordinates[n], d)})")


if __name__ == "__main__":
    run()
