#!/usr/bin/env python3
"""Golden aritmetici indipendenti NTC 2018 [4.2.5–8], NON eseguono il C#.

Si usano Fraction e confronti esatti; le sezioni nette e ftk sono dati
ipotetici documentati come prova del ramo, NON geometrie di giunti reali.
"""

from fractions import Fraction as F


def check(name, condition):
    if not condition:
        raise AssertionError(name)
    print("PASS", name)


def main():
    a = F(2848)
    fy = F(235)
    fu = F(360)
    gm0 = F(105, 100)
    gm2 = F(125, 100)
    gross = a * fy / gm0 / 1000
    net = F(9, 10) * F(2400) * fu / gm2 / 1000
    high_net = F(9, 10) * F(2700) * fu / gm2 / 1000
    check("P0D1-01: gammaM0=1.05", gm0 == F(21, 20))
    check("P0D1-02: gammaM2=1.25", gm2 == F(5, 4))
    check("P0D1-03: area OPPO IPE200 28.48 cm2 -> 2848 mm2", a == F(2848))
    check("P0D1-04: pure gross yield", gross == F(2848 * 235 * 20, 21 * 1000))
    check("P0D1-05: net ultimate 2400", net == F(62208, 100))
    check("P0D1-06: net governs when holed 2400", net < gross)
    check("P0D1-07: gross only would pass at 630", F(630) < gross)
    check("P0D1-08: net check must fail at 630", F(630) > net)
    check("P0D1-09: unweakened passes at 630", F(630) < gross)
    check("P0D1-10: net boundary passes", net / min(net, gross) == 1)
    check("P0D1-11: beyond boundary fails", (net + F(1, 10**8)) / min(net, gross) > 1)
    check("P0D1-12: high net 2700", high_net == F(69984, 100))
    check("P0D1-13: gross governs if net 2700", gross < high_net)
    check("P0D1-14: hierarchy required fails with 2400", gross > net)
    check("P0D1-15: hierarchy required passes with 2700", gross < high_net)
    check("P0D1-16: gamma=1.0 is not NTC gamma", a * fy / 1000 != gross)
    print("16/16 PASS — standalone arithmetic only; xUnit C# not executed")


if __name__ == "__main__":
    main()
