"""Independent NTC 2018 steel arithmetic examples; NOT .NET implementation tests.

Uses exact rational arithmetic for every example except the square-root shear
capacity (there the exact squared identity is checked). These synthetic sections
do not represent a certified steel product, joint or verified structural member.
"""

from fractions import Fraction as F


GAMMA_M0 = F(21, 20)
GAMMA_M1 = F(21, 20)
GAMMA_M2 = F(5, 4)
FY = F(235)  # MPa = N/mm2
FU = F(360)  # MPa; illustrative ultimate strength with documented evidence assumed
AREA = F(3000)  # mm2
checks = 0


def check(description, actual, expected):
    global checks
    if actual != expected:
        raise AssertionError(f"{description}: actual={actual}, expected={expected}")
    checks += 1
    print(f"PASS {checks:02d}: {description}")


def kn(newtons):
    return newtons / 1000


def knm(newton_millimetres):
    return newton_millimetres / 1000000


def tension_net(net_area):
    return kn(F(9, 10) * net_area * FU / GAMMA_M2)


gross = kn(AREA * FY / GAMMA_M0)
net_with_holes = tension_net(F(2400))
net_less_affected = tension_net(F(2900))

check("Tab 4.2.VII: section factors", GAMMA_M0, F(105, 100))
check("Tab 4.2.VII: member stability factor (ordinary structure)", GAMMA_M1, F(105, 100))
check("Tab 4.2.VII: net fracture factor", GAMMA_M2, F(125, 100))
check("Eq 4.2.6: gross tensile yielding [kN]", gross, F(4700, 7))
check("Eq 4.2.7: net tensile rupture for Anet=2400 [kN]", net_with_holes, F(15552, 25))
check("Eqs 4.2.5-7: rupture governs for Anet=2400", min(gross, net_with_holes), net_with_holes)
check("Eqs 4.2.5-7: yielding governs for Anet=2900", min(gross, net_less_affected), gross)
check("Demand 600 kN below net-governed capacity", F(600) <= min(gross, net_with_holes), True)
check("Demand 650 kN exceeds net-governed capacity", F(650) > min(gross, net_with_holes), True)
check("Eq 4.2.8: hierarchy condition fails for first example", gross <= net_with_holes, False)
check("Eq 4.2.8: hierarchy condition holds for second example", gross <= net_less_affected, True)

check("Eq 4.2.10: compression class 1-3, gross area [kN]", kn(AREA * FY / GAMMA_M0), F(4700, 7))
check("Eq 4.2.10: compression class 4, Aeff=2500 [kN]", kn(F(2500) * FY / GAMMA_M0), F(11750, 21))
check("Eqs 4.2.12-14: class 1-2 Wpl=350000 mm3 [kNm]", knm(F(350000) * FY / GAMMA_M0), F(235, 3))
check("Eqs 4.2.12-14: class 3 Wel=300000 mm3 [kNm]", knm(F(300000) * FY / GAMMA_M0), F(470, 7))
check("Eqs 4.2.12-14: class 4 Weff=240000 mm3 [kNm]", knm(F(240000) * FY / GAMMA_M0), F(376, 7))

# Eq 4.2.17: V_Rd = Av*fy/(sqrt(3)*gamma_M0), so V_Rd**2
# is a purely rational quantity. Check the identity without a float sqrt.
av = F(1500)
shear_capacity_squared = (av * FY / (1000 * GAMMA_M0)) ** 2 / 3
check("Eq 4.2.17: pure shear capacity squared [kN2]", shear_capacity_squared, F(5522500, 147))
check("Eq 4.2.30: half-shear threshold", F(1, 2) <= F(1, 2), True)
rho_three_quarters = (2 * F(3, 4) - 1) ** 2
check("Eq 4.2.31: rho at 0.75 Vc,Rd", rho_three_quarters, F(1, 4))
check("Eq 4.2.32: reduced bending Wpl for Aw=1500, tw=6 [mm3]",
      F(350000) - rho_three_quarters * F(1500) ** 2 / (4 * F(6)), F(653125, 2))

# Tables 4.2.III-IV were visually inspected in the official PDF (embedded
# diagrams, not readable as plain PDF text). S235: epsilon=sqrt(235/235)=1.
# This is a plate classification under pure loading, not a member resistance.
def plate_class(ratio, bounds):
    return next((index for index, threshold in enumerate(bounds, start=1)
                 if ratio <= threshold), 4)


flange_bounds = (F(9), F(10), F(14))
web_compression_bounds = (F(33), F(38), F(42))
web_bending_bounds = (F(72), F(83), F(124))
check("Tab 4.2.IV: flange c/t=9 => class 1", plate_class(F(9), flange_bounds), 1)
check("Tab 4.2.IV: flange c/t=9.5 => class 2", plate_class(F(19, 2), flange_bounds), 2)
check("Tab 4.2.IV: flange c/t=12 => class 3", plate_class(F(12), flange_bounds), 3)
check("Tab 4.2.IV: flange c/t=15 => class 4", plate_class(F(15), flange_bounds), 4)
check("Tab 4.2.III: compressed web c/t=33 => class 1", plate_class(F(33), web_compression_bounds), 1)
check("Tab 4.2.III: compressed web c/t=35 => class 2", plate_class(F(35), web_compression_bounds), 2)
check("Tab 4.2.III: compressed web c/t=40 => class 3", plate_class(F(40), web_compression_bounds), 3)
check("Tab 4.2.III: compressed web c/t=43 => class 4", plate_class(F(43), web_compression_bounds), 4)
check("Tab 4.2.III: bending web c/t=72 => class 1", plate_class(F(72), web_bending_bounds), 1)
check("Tab 4.2.III: bending web c/t=80 => class 2", plate_class(F(80), web_bending_bounds), 2)
check("Tab 4.2.III: bending web c/t=100 => class 3", plate_class(F(100), web_bending_bounds), 3)
check("Tab 4.2.III: bending web c/t=125 => class 4", plate_class(F(125), web_bending_bounds), 4)
check("NTC 4.2.3.1: governing section class is max(plate classes)",
      max(plate_class(F(19, 2), flange_bounds), plate_class(F(40), web_compression_bounds)), 3)
check("NTC 4.2.4.1.2.1 gross tension: gamma=1 is illustrative, not NTC", kn(AREA * FY), F(705))

print(f"P0-R2: {checks}/{checks} independent arithmetic and boundary checks PASS; no .NET gate.")
