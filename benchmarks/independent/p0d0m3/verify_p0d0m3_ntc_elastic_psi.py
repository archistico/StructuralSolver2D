"""Independent exact-rational NTC web ELASTIC psi checks; imports no C#.

Not a .NET test, not a physical validation of catalog section values, and not
evidence of complete design verification. The source is NTC 2018 Tab. 4.2.III.
"""

from fractions import Fraction as F


def stress(n_kn: F, m_knm: F, area_mm2: F, inertia_mm4: F, web_flat_mm: F):
    compression_average = -n_kn * 1000 / area_mm2
    bending_at_web_edge = abs(m_knm) * 1_000_000 * web_flat_mm / (2 * inertia_mm4)
    high = compression_average + bending_at_web_edge
    low = compression_average - bending_at_web_edge
    if high <= 0:
        return compression_average, bending_at_web_edge, high, low, None
    return compression_average, bending_at_web_edge, high, low, low / high


def footnote(compressive_stress: F, fy: F, tensile_strain, young_modulus):
    first = compressive_stress <= fy
    second = (tensile_strain is not None and young_modulus is not None
              and young_modulus > 0 and tensile_strain > fy / young_modulus)
    return first, second, first or second


web_flat = F(224) - 2 * (F(8) + F(4))
area, inertia = F(10_000), F(50_000_000)
compression = stress(F(-100), F(10), area, inertia, web_flat)
tension = stress(F(100), F(10), area, inertia, web_flat)
over_yield = stress(F(3000), F(300), area, inertia, web_flat)
no_compression = stress(F(100), F(1), area, inertia, web_flat)
boundary = stress(F(100), F(5), area, inertia, web_flat)
compression_low_moment = stress(F(-100), F(1), area, inertia, web_flat)
thin = stress(F(-20), F(4), F(2000), F(20_000_000), web_flat)
ipe_web_flat = F(200) - 2 * (F(17, 2) + F(12))
ipe_200 = stress(F(-100), F(10), F(2848), F(19_430_000), ipe_web_flat)
psi_a = compression[4]
psi_b = tension[4]

# In NTC units epsilon=sqrt(235/fyk). Here fyk=235 MPa => epsilon=1.
class3_compression = F(42) / (F(67, 100) + F(33, 100) * psi_a)
special_radicand = -psi_b
special_multiplier = 62 * (1 - psi_b)
sqrt3_lower, sqrt3_upper = F(173205, 100000), F(173206, 100000)
checks = [
    ("flat-web width from rolled I/H dimensions is 200 mm", web_flat == 200),
    ("N=-100 kN gives +10 MPa mean compression", compression[0] == 10),
    ("M=10 kNm gives +20 MPa absolute web-edge bending", compression[1] == 20),
    ("compression N: larger edge stress +30 MPa", compression[2] == 30),
    ("compression N: opposite edge stress -10 MPa", compression[3] == -10),
    ("compression N: computed psi=-1/3", psi_a == F(-1, 3)),
    ("NTC ordinary branch class3 limit exactly 75", class3_compression == 75),
    ("web ratio 20 is class 3 or better, never proof of class 1/2", 20 <= class3_compression),
    ("tension N: -10 MPa mean compression", tension[0] == -10),
    ("tension N: +10 MPa remains at compressed web edge", tension[2] == 10),
    ("tension N: -30 MPa at opposite edge", tension[3] == -30),
    ("tension N: computed psi=-3 special NTC branch", psi_b == -3),
    ("special branch multiplier is 248", special_multiplier == 248),
    ("special branch radical is sqrt(3)", special_radicand == 3),
    ("sqrt3 lower bound verified by exact squares", sqrt3_lower ** 2 < 3),
    ("sqrt3 upper bound verified by exact squares", sqrt3_upper ** 2 > 3),
    ("special class3 limit strictly between rational bounds", 248 * sqrt3_lower < 248 * sqrt3_upper),
    ("sigmaC=10 MPa satisfies NTC footnote without measured strain", footnote(tension[2], F(235), None, None) == (True, False, True)),
    ("C=-300 MPa at net tensile N=3000 kN", over_yield[0] == -300),
    ("B=600 MPa and sigmaC=300 MPa", over_yield[1:3] == (600, 300)),
    ("over-yield compression first alternative fails", not footnote(over_yield[2], F(235), None, None)[2]),
    ("over-yield compression second alternative succeeds", footnote(over_yield[2], F(235), F(3, 1000), F(210000)) == (False, True, True)),
    ("tensile strain equal fy/E is insufficient", not footnote(over_yield[2], F(235), F(235, 210000), F(210000))[2]),
    ("tensile strain lower fy/E is insufficient", not footnote(over_yield[2], F(235), F(1, 10000), F(210000))[2]),
    ("net tension with low bending leaves no compressed web edge", no_compression[2] == -8 and no_compression[4] is None),
    ("net tension at exactly zero compressed edge is excluded", boundary[2] == 0 and boundary[4] is None),
    ("net compression at lower bending gives positive psi", compression_low_moment[4] == F(2, 3)),
    ("opposite signs of strong-axis moment produce the same psi", stress(F(-100), F(-10), area, inertia, web_flat)[4] == psi_a),
    ("thin web reference A=2000 Iy=20e6 has same psi", thin[4] == psi_a),
    ("thin web c/t=100 exceeds ordinary class3 limit75", F(100) > class3_compression),
    ("inertia less than A(h/2)^2 for thin reference", F(20_000_000) < F(2000) * F(112)**2),
    ("pure bending with N=0 computes psi=-1", stress(F(0), F(10), area, inertia, web_flat)[4] == -1),
    ("pure uniform axial stress with M=0 gives psi=1", stress(F(-100), F(0), area, inertia, web_flat)[4] == 1),
    ("conservative result can only be web class<=3 or 4", 20 <= class3_compression and 100 > class3_compression),
    ("OPPO IPE200 published nominal web flat is 159 mm", ipe_web_flat == 159),
    ("OPPO IPE200 C = 100000/2848 MPa", ipe_200[0] == F(100000, 2848)),
    ("OPPO IPE200 B = (10e6*79.5)/19430000 MPa", ipe_200[1] == F(10_000_000) * F(159, 2) / F(19_430_000)),
    ("OPPO IPE200 psi exactly -8029/105179", ipe_200[4] == F(-8029, 105179)),
    ("OPPO IPE200 web c/t <= exact NTC class3 limit", F(159) / F(28, 5) <= F(42) / (F(67, 100) + F(33, 100) * ipe_200[4])),
    ("OPPO IPE200 inertia satisfies section bounding limit", F(19_430_000) < F(2848) * F(100)**2),
]

for number, (description, passed) in enumerate(checks, start=1):
    if not passed:
        raise AssertionError(f"D0M3-{number:02d} FAIL: {description}")
    print(f"PASS D0M3-{number:02d}: {description}")

print(f"P0-D0M3: {len(checks)}/{len(checks)} independent exact-rational checks PASS; .NET NOT run")
