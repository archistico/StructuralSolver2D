#!/usr/bin/env python3
"""NTC 2018 4.2.III mixed N/M table, exact rational independent goldens.

No C# code is called. Stress fields alpha/psi here are hypothetical input:
they are not derived from a real FEM or validated as a physical stress state.
"""

from fractions import Fraction as F


def limits(alpha, psi):
    if not F(0) < alpha < F(1) or not F(-1) < psi < F(1):
        return None
    plastic = ((F(396) / (13 * alpha - 1), F(456) / (13 * alpha - 1))
               if alpha > F(1, 2) else (F(36) / alpha, F(83, 2) / alpha))
    elastic = F(42) / (F(67, 100) + F(33, 100) * psi)
    return plastic + (elastic,)


def classify(ratio, trio):
    return next((cl for cl, boundary in enumerate(trio, 1) if ratio <= boundary), 4)


def main():
    checks = 0

    def check(label, ok):
        nonlocal checks
        checks += 1
        if not ok:
            raise AssertionError(label)
        print("PASS", label)

    above = limits(F(3, 5), F(-1, 2))
    check("D0M1-01 alpha 0.6 class1", above[0] == F(990, 17))
    check("D0M1-02 alpha 0.6 class2", above[1] == F(1140, 17))
    check("D0M1-03 psi -0.5 class3", above[2] == F(8400, 101))
    check("D0M1-04 ordered alpha>0.5", above[0] < above[1] < above[2])
    for i, (ratio, expected) in enumerate(((55, 1), (60, 2), (70, 3), (90, 4)), 5):
        check(f"D0M1-{i:02d} web c/t {ratio}", classify(F(ratio), above) == expected)

    half = limits(F(1, 2), F(-3, 5))
    check("D0M1-09 alpha 0.5 left class1", half[0] == 72)
    check("D0M1-10 alpha 0.5 left class2", half[1] == 83)
    check("D0M1-11 psi -0.6 class3", half[2] == F(5250, 59))
    for i, (ratio, expected) in enumerate(((70, 1), (76, 2), (85, 3), (90, 4)), 12):
        check(f"D0M1-{i:02d} alpha=.5 web {ratio}", classify(F(ratio), half) == expected)
    check("D0M1-16 inclusive class1", classify(half[0], half) == 1)
    check("D0M1-17 inclusive class2", classify(half[1], half) == 2)
    check("D0M1-18 above class1 boundary", classify(half[0] + F(1, 100000), half) == 2)
    check("D0M1-19 above class2 boundary", classify(half[1] + F(1, 100000), half) == 3)

    inconsistent = limits(F(3, 5), F(4, 5))
    check("D0M1-20 inconsistent pair rejected", inconsistent[2] < inconsistent[1])
    check("D0M1-21 alpha zero unsupported", limits(F(0), F(-1, 2)) is None)
    check("D0M1-22 alpha one pure domain excluded", limits(F(1), F(-1, 2)) is None)
    check("D0M1-23 psi -1 dedicated branch excluded", limits(F(3, 5), F(-1)) is None)
    check("D0M1-24 psi +1 pure domain excluded", limits(F(3, 5), F(1)) is None)
    check("D0M1-25 S235 epsilon exactly one", F(235, 235) == 1)
    check("D0M1-26 flange limits remain separate", (9, 10, 14) != tuple(above))
    print(f"P0-D0M1: {checks}/{checks} independent exact-rational arithmetic PASS; no .NET gate")


if __name__ == "__main__":
    main()
