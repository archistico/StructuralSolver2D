# Expected benchmark results

This file documents the first StructuralSolver2D benchmark expected values.

All values use the internal unit system:

- length: m
- force: kN
- moment: kNm
- elastic modulus: kN/m²
- area: m²
- second moment of area: m⁴

For the beam benchmarks the common elastic data are:

```text
E = 210000000 kN/m²
I = 0.00002 m⁴
EI = 4200 kNm²
```

---

## B01 - Simply supported beam with uniform distributed load

Model:

```text
L = 5 m
q = 10 kN/m downward
```

Closed-form checks:

```text
RA = qL / 2 = 25 kN
RB = qL / 2 = 25 kN
Vmax = qL / 2 = 25 kN
Mmax = qL² / 8 = 31.25 kNm
v_mid = 5qL⁴ / (384EI) = 0.0193769841269841 m downward
```

Expected sign convention in solver output:

```text
midspan displacement Y = -0.0193769841269841 m
```

---

## B02 - Simply supported beam with point load at midspan

Model:

```text
L = 6 m
P = 30 kN downward at x = L / 2
```

Closed-form checks:

```text
RA = P / 2 = 15 kN
RB = P / 2 = 15 kN
Vmax = P / 2 = 15 kN
Mmax = PL / 4 = 45 kNm
v_mid = PL³ / (48EI) = 0.0321428571428571 m downward
```

Expected sign convention in solver output:

```text
midspan displacement Y = -0.0321428571428571 m
```

---

## B03 - Cantilever with tip point load

Model:

```text
L = 4 m
P = 12 kN downward at the free end
```

Closed-form checks:

```text
RAy = P = 12 kN
MA = PL = 48 kNm
v_tip = PL³ / (3EI) = 0.060952380952381 m downward
rz_tip = PL² / (2EI) = 0.0228571428571429 rad clockwise/downward-sign convention
```

Expected sign convention in solver output:

```text
tip displacement Y = -0.060952380952381 m
tip rotation Z = -0.0228571428571429 rad
```

---

## B04 - Cantilever with uniform distributed load

Model:

```text
L = 4 m
q = 8 kN/m downward
```

Closed-form checks:

```text
RAy = qL = 32 kN
MA = qL² / 2 = 64 kNm
v_tip = qL⁴ / (8EI) = 0.060952380952381 m downward
rz_tip = qL³ / (6EI) = 0.0203174603174603 rad clockwise/downward-sign convention
```

Expected sign convention in solver output:

```text
tip displacement Y = -0.060952380952381 m
tip rotation Z = -0.0203174603174603 rad
```

---

## T01 - Symmetric triangular truss

Model:

```text
A = (0, 0)
B = (4, 0)
C = (2, 3)
P = 30 kN downward at C
```

Geometry:

```text
AC = BC = sqrt(13) m
sin(theta) = 3 / sqrt(13)
cos(theta) = 2 / sqrt(13)
```

Closed-form checks:

```text
RAy = 15 kN
RBy = 15 kN
N_AC = N_BC = -15 / sin(theta) = -5 sqrt(13) = -18.0277563773199 kN
N_AB = 10 kN tension
```

Sign convention:

```text
positive N = tension
negative N = compression
```

The top node C is not expected to have exactly zero horizontal displacement when support B is a roller. The bottom tie elongates and the structure can show a small horizontal displacement depending on the support layout.

---

## F01 - Portal frame with symmetric gravity load

Model:

```text
single-bay fixed-base portal frame
span = 6 m
height = 4 m
beam load = 10 kN/m downward over 6 m
```

Primary checks:

```text
total vertical load = 60 kN
RAy + RBy = 60 kN
RAy = RBy by symmetry
horizontal reactions equal and opposite by symmetry
solution must be stable
```

This benchmark intentionally starts with equilibrium and symmetry checks rather than hard-coded moment values. More detailed frame benchmark values should be added after comparison with an independent reference source or a hand-checked stiffness-method derivation.
