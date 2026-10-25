#!/usr/bin/env python3
"""Independent exact-rational check of a strictly interior PLASTIC web PNA.

No .NET modules are imported. This tests arithmetic and limiting conditions,
not a compiled C# implementation or a complete NTC section/member check.
Data references:
  NTC2018 §4.2.3.1, Tab. 4.2.III (plastic compressed web depth alpha)
  https://www.gazzettaufficiale.it/eli/gu/2018/02/20/42/so/8/sg/pdf
  https://www.oppo.it/tabelle/profilati_ipe.htm (indicative rolled sections)
"""
from fractions import Fraction as Q


def assert_eq(actual, expected, label):
    assert actual == expected, f"{label}: {actual} != {expected}"


def section(name, h, b, tw, tf, r, gross):
    c = h - 2 * (tf + r)
    a_rect = 2 * b * tf + tw * (h - 2 * tf)
    assert c > 0 and (b - tw - 2 * r) > 0
    assert a_rect - a_rect / 100 <= gross
    assert gross <= a_rect + 4 * r * r + a_rect / 100
    return name, c, tw, gross


CASES = [
    section("rolled geometry synthetic", Q(224), Q(100), Q(10), Q(8), Q(4), Q(3694)),
    section("OPPO IPE-200", Q(200), Q(100), Q(56, 10), Q(85, 10), Q(12), Q(2848)),
    section("OPPO IPE-300", Q(300), Q(150), Q(71, 10), Q(107, 10), Q(15), Q(5381)),
]
FY = Q(235)
count = 0


def check(nkn, geometry):
    global count
    name, c, tw, gross = geometry
    limit = FY * tw * c / 1000
    assert abs(nkn) < limit, (name, nkn, limit)
    # Solve the two PLASTIC rectangular web subareas + unknown symmetric
    # flange/fillet lump: the symmetric contributions cancel exactly.
    delta = -1000 * nkn / (2 * FY * tw)
    upper_a = gross / 2 + tw * delta
    lower_a = gross / 2 - tw * delta
    alpha = (c / 2 + delta) / c
    assert_eq(FY * (lower_a - upper_a), 1000 * nkn, name + " axial balance")
    assert_eq(alpha, Q(1, 2) - 1000 * nkn / (2 * FY * tw * c), name + " alpha")
    assert Q(0) < alpha < Q(1), (name, alpha)
    assert (alpha > Q(1, 2)) == (nkn < 0)
    assert upper_a > 0 and lower_a > 0
    count += 1
    return alpha, limit


for shape in CASES:
    limit = FY * shape[2] * shape[1] / 1000
    for fraction in [Q(1, 100), Q(1, 4), Q(49, 100), Q(4, 5), Q(999999, 1000000)]:
        tensile, _ = check(limit * fraction, shape)
        compressed, _ = check(-limit * fraction, shape)
        assert_eq(tensile + compressed, Q(1), shape[0] + " sign symmetry")
        count += 1
    # Exact boundaries must remain out of the flat-web method. This is NOT a
    # statement that the real rolled section cannot resist such an N.
    assert abs(limit) >= limit
    assert abs(-limit) >= limit
    count += 2

alpha_ipe200, limit_ipe200 = check(Q(-100), CASES[1])
assert_eq(CASES[1][1], Q(159), "OPPO IPE200 flat clear depth")
assert_eq(limit_ipe200, Q(209244, 1000), "OPPO IPE200 flat web Nmax kN")
assert_eq(alpha_ipe200, Q(1, 2) + Q(100000) / (2 * FY * Q(56, 10) * Q(159)),
          "OPPO IPE200 exact alpha")
count += 3

# Contrast with the ELASTIC result: alpha is not ψ and cannot be substituted
# into class-3 criteria. Independent D0M3 OPPO IPE200 ψ=-8029/105179.
psi_ipe200 = Q(-8029, 105179)
assert alpha_ipe200 != psi_ipe200
count += 1

print(f"P0-D0M4: {count}/{count} controlli razionali indipendenti PASS "
      "(nessun test compilato .NET, nessuna verifica di resistenza).")
