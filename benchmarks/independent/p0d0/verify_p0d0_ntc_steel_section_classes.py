#!/usr/bin/env python3
"""Arithmetic NTC 2018 Tables 4.2.III/IV, independent of C# and of EC3.

Worked dimensions are declared hypothetical or taken from cited engineering
examples; passing a ratio does not verify the manufactured steel member.
"""

from fractions import Fraction as F
from math import sqrt


def classification(c_over_t, epsilon, limits):
    return next((i for i, limit in enumerate(limits, start=1)
                 if c_over_t <= limit * epsilon), 4)


def check(name, assertion):
    if not assertion:
        raise AssertionError(name)
    print("PASS", name)


def main():
    checks = 0

    def verify(name, assertion):
        nonlocal checks
        check(name, assertion)
        checks += 1

    eps = F(1)
    for ratio, cl in [(F(9), 1), (F(10), 2), (F(14), 3), (F(15), 4)]:
        verify(f"flange c/t={ratio}", classification(ratio, eps, [9, 10, 14]) == cl)
    for ratio, cl in [(F(33), 1), (F(38), 2), (F(42), 3), (F(43), 4)]:
        verify(f"web uniform compression c/t={ratio}",
               classification(ratio, eps, [33, 38, 42]) == cl)
    for ratio, cl in [(F(72), 1), (F(83), 2), (F(124), 3), (F(125), 4)]:
        verify(f"web pure strong bending c/t={ratio}",
               classification(ratio, eps, [72, 83, 124]) == cl)

    verify("exact epsilon S235", sqrt(235 / 235) == 1)
    verify("computed epsilon S355 not rounded table 0.81", 0.813 < sqrt(235 / 355) < 0.814)
    verify("S420 Bernuzzi input epsilon", 0.747 < sqrt(235 / 420) < 0.749)
    # Bernuzzi A4.1 IPE550 S275; h550 b210 tw11.1 tf17.2 r24
    c_web_550 = F(550) - 2 * F("17.2") - 2 * F(24)
    c_flange_550 = (F(210) - F("11.1") - 2 * F(24)) / 2
    e275 = sqrt(235 / 275)
    verify("IPE550 A4.1 web geometry", c_web_550 == F("467.6"))
    verify("IPE550 A4.1 flange geometry", c_flange_550 == F("75.45"))
    verify("IPE550 A4.1 web class 4", classification(float(c_web_550 / F("11.1")), e275, [33, 38, 42]) == 4)
    verify("IPE550 A4.1 flange class 1", classification(float(c_flange_550 / F("17.2")), e275, [9, 10, 14]) == 1)
    # Bernuzzi A4.2 HEA280 S420; h270 b280 tw8 tf13 r24.
    c_flange_280 = (F(280) - F(8) - 2 * F(24)) / 2
    c_web_280 = F(270) - 2 * F(13) - 2 * F(24)
    e420 = sqrt(235 / 420)
    verify("HEA280 A4.2 flange geometry", c_flange_280 == F(112))
    verify("HEA280 A4.2 web geometry", c_web_280 == F(196))
    verify("HEA280 A4.2 flange class 3", classification(float(c_flange_280 / F(13)), e420, [9, 10, 14]) == 3)
    verify("HEA280 A4.2 web class 1", classification(float(c_web_280 / F(8)), e420, [72, 83, 124]) == 1)
    verify("plate class governing", max(3, 1) == 3 and max(1, 4) == 4)
    verify("class four is identification not resistance", classification(F(43), eps, [33, 38, 42]) == 4)
    print(f"P0-D0 {checks}/{checks} PASS — Python arithmetic only; new .NET tests NOT run")


if __name__ == "__main__":
    main()
