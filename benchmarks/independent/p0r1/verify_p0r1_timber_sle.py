#!/usr/bin/env python3
"""P0-R1: independent exact-fraction NTC timber SLE arithmetic references.

Sources: NTC 2018 §4.4.7/Table 4.4.V and §2.5.3/Table 2.5.I;
Circular 7/2019 C4.4.7, Eq. [C4.4.3]. This script does not invoke C#,
certify FEM input/evidence, or establish overall member compliance.
"""

from fractions import Fraction as F


def check(label: str, actual: F, expected: F) -> None:
    if actual != expected:
        raise AssertionError(f"{label}: got {actual}, expected {expected}")
    print(f"PASS {label:<29} {actual} = {float(actual):.9f} mm")


def final_without_camber(g: F, q: F, companion: F,
                         kdef: F, psi2_leading: F,
                         psi0_companion: F, psi2_companion: F) -> F:
    # Circular 7/2019 [C4.4.3], homogeneous viscoelastic components only.
    return (g * (1 + kdef)
            + q * (1 + psi2_leading * kdef)
            + companion * (psi0_companion + psi2_companion * kdef))


def main() -> None:
    # Independent simply supported Euler–Bernoulli beam reference:
    # L=4000 mm; b=100 mm; h=200 mm; Emean=11000 N/mm²;
    # I=b*h^3/12; qG=0.5, qQ=0.7, qW=0.2 N/mm.
    # delta=5*q*L^4/(384*E*I), hence these exact rational millimetres.
    g, q, w = F(25, 11), F(35, 11), F(10, 11)
    psi0_wind, psi2_q = F(3, 5), F(3, 10)
    kdef_1, kdef_3, kdef_wet = F(3, 5), F(2), F(13, 5)
    camber = F(1)

    check("rare-total-before-camber", g + q + psi0_wind * w, F(6))
    check("rare-variable-instant", q + psi0_wind * w, F(41, 11))
    check("rare-total-net", g + q + psi0_wind * w - camber, F(5))
    check("final-dry-class-1", final_without_camber(
        g, q, w, kdef_1, psi2_q, psi0_wind, F(0)) - camber, F(763, 110))
    check("final-class-3", final_without_camber(
        g, q, w, kdef_3, psi2_q, psi0_wind, F(0)) - camber, F(126, 11))
    check("final-wet-class-1-plus-2", final_without_camber(
        g, q, w, kdef_wet, psi2_q, psi0_wind, F(0)) - camber, F(1473, 110))
    check("final-opposing-wind", final_without_camber(
        g, q, -w, kdef_1, psi2_q, psi0_wind, F(0)) - camber, F(643, 110))
    check("final-snow-above-1000m", final_without_camber(
        g, q, w, kdef_1, psi2_q, F(7, 10), F(1, 5)) - camber, F(157, 22))
    check("final-roof-H-psi-zero", final_without_camber(
        g, q, w, kdef_1, psi2_q, F(0), F(0)) - camber, F(703, 110))

    # NTC §4.4.7: L/300 rare-variable, L/200 final; L=2*projection for
    # cantilevers. These are indicative criteria, not mandatory for all uses.
    check("rare-variable-limit-L300", F(4000, 300), F(40, 3))
    check("final-limit-L200", F(4000, 200), F(20))
    check("cantilever-limit-2L300", F(2 * 4000, 300), F(80, 3))
    print("PASS P0-R1: 12 exact independent arithmetic references; no .NET gate")


if __name__ == "__main__":
    main()
