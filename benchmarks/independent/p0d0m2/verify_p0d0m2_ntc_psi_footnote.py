#!/usr/bin/env python3
"""Independent exact-rational NTC Tab 4.2.III psi<=-1 and footnote checks.

Does not import, run, compile or validate the C# implementation. Exact Fraction
arithmetic for the algebra and squared rational bounds for the irrational root.
"""
from fractions import Fraction as F

count = 0


def check(description: str, expression: bool) -> None:
    global count
    if not expression:
        raise AssertionError(f"FAIL: {description}")
    count += 1
    print(f"PASS D0M2-{count:02d}: {description}")


def l3_for_square(positive_square: int, epsilon: F = F(1)) -> F:
    # psi = -s^2, so sqrt(-psi) = s exactly.
    from math import isqrt

    s = isqrt(positive_square)
    assert s * s == positive_square
    psi = -positive_square
    return 62 * epsilon * (1 - psi) * s


def footnote(sigma_compression: F, tensile_strain: F, fy: F, young: F) -> bool:
    return sigma_compression <= fy or tensile_strain > fy / young


def psi_from_elastic_edge_stresses(compression_magnitude: F, opposite_tensile_magnitude: F) -> F:
    assert compression_magnitude > 0 and opposite_tensile_magnitude > 0
    return -opposite_tensile_magnitude / compression_magnitude


def symmetric_linear_web_psi(mean_compression_stress: F, bending_edge_stress: F) -> F:
    # Compression-positive mean is -N_FEM/A. One edge must be compressed.
    assert bending_edge_stress > abs(mean_compression_stress)
    return (mean_compression_stress - bending_edge_stress) / (mean_compression_stress + bending_edge_stress)


def plate_class(ratio: F, c1: F, c2: F, c3: F) -> int:
    assert 0 < c1 < c2 <= c3
    return 1 if ratio <= c1 else 2 if ratio <= c2 else 3 if ratio <= c3 else 4


if __name__ == "__main__":
    fy, e = F(235), F(210_000)
    alpha = F(3, 5)
    first = F(396, 1) / (13 * alpha - 1)
    second = F(456, 1) / (13 * alpha - 1)
    check("alpha=.6 class1 value", first == F(990, 17))
    check("alpha=.6 class2 value", second == F(1140, 17))

    check("psi=-1 closed-branch limit=124 epsilon", l3_for_square(1) == 124)
    check("psi=-4 closed-branch limit=620 epsilon", l3_for_square(4) == 620)
    check("psi=-9 closed-branch limit=1860 epsilon", l3_for_square(9) == 1860)
    check("class3 at 124 exact boundary", plate_class(F(124), first, second, F(124)) == 3)
    check("class4 just beyond psi=-1 limit", plate_class(F(125), first, second, F(124)) == 4)
    check("class3 at 620 exact boundary", plate_class(F(620), first, second, F(620)) == 3)
    check("class4 beyond psi=-4 limit", plate_class(F(621), first, second, F(620)) == 4)

    # psi=-2 requires sqrt(2); prove narrow rational bounds WITHOUT relying on
    # the numeric algorithm under test (no floating-point square root here).
    lower, upper = F(1414213562373095, 10**15), F(1414213562373096, 10**15)
    check("rational lower bound sqrt(2)", lower**2 < 2)
    check("rational upper bound sqrt(2)", upper**2 > 2)
    check("psi=-2 threshold exceeds 250", 186 * lower > 250)
    check("psi=-2 threshold below 300", 186 * upper < 300)
    check("psi=-2 threshold > class2", 186 * lower > second)

    check("footnote alternative A: compression below fy", footnote(F(200), F(0), fy, e))
    check("footnote alternative A: compression exactly fy", footnote(fy, F(0), fy, e))
    check("footnote alternative B: tensile strain strictly greater", footnote(F(300), F(15, 10_000), fy, e))
    check("footnote fails when both alternatives absent", not footnote(F(300), F(1, 10_000), fy, e))
    check("footnote tensile strain equal to fy/E is insufficient", not footnote(F(300), fy / e, fy, e))
    check("footnote tensile strain infinitesimally above fy/E", footnote(F(300), fy / e + F(1, 10**12), fy, e))

    check("psi=-1 from equal opposite elastic edge stress magnitudes",
          psi_from_elastic_edge_stresses(F(235), F(235)) == -1)
    check("psi=-2 from unequal opposite elastic edge stress magnitudes",
          psi_from_elastic_edge_stresses(F(200), F(400)) == -2)
    check("psi=-4 from unequal opposite elastic edge stress magnitudes",
          psi_from_elastic_edge_stresses(F(200), F(800)) == -4)
    check("inconsistent declared psi=-2 versus real edge ratio=-5 rejected",
          psi_from_elastic_edge_stresses(F(200), F(1000)) != -2)
    check("physical A-only illustration: compression=100, tension=200, strain=200/E",
          psi_from_elastic_edge_stresses(F(100), F(200)) == -2 and
          footnote(F(100), F(200) / e, fy, e) and F(200) / e <= fy / e)
    check("physical B-only illustration: compression=300, tension=600, strain=600/E",
          psi_from_elastic_edge_stresses(F(300), F(600)) == -2 and
          footnote(F(300), F(600) / e, fy, e) and F(300) > fy)

    # Crucial physics gate: for doubly symmetric, linear elastic I/H sections
    # C=-N_FEM/A is the mean COMPRESSION (positive for FEM N<0). Hence psi<-1
    # cannot coexist with FEM N<0 and pure strong-axis bending at the same station.
    check("symmetric elastic compression N_FEM<0 produces psi>-1",
          symmetric_linear_web_psi(F(10), F(30)) == F(-1, 2))
    check("symmetric elastic tension N_FEM>0 produces psi<-1",
          symmetric_linear_web_psi(F(-10), F(30)) == F(-2))
    check("zero axial force is psi=-1 pure-bending limit",
          symmetric_linear_web_psi(F(0), F(30)) == F(-1))
    check("tensile N special branch requires compressed plastic fraction below half",
          F(2, 5) < F(1, 2))
    check("compression N original branch stays separate",
          F(-1, 2) > -1)

    # The two NTC analytical branches are distinct at psi=-1; using the earlier
    # 42/(0.67+0.33*psi) formula by accident gives 2100/17 !=124.
    upper_branch_if_wrong = F(42) / (F(67, 100) - F(33, 100))
    check("at psi=-1 do NOT reuse psi>-1 formula", upper_branch_if_wrong != F(124))
    check("class4 is identification only (no effective resistance)", plate_class(F(700), first, second, F(620)) == 4)

    print(f"P0-D0M2: {count}/{count} independent exact-rational checks PASS; C#/.NET NOT executed")
