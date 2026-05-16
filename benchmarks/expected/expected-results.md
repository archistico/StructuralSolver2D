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

---

## F02 - Inclined cantilever with LocalY uniform load

Model:

```text
A = (0, 0)
B = (3, 4)
L = 5 m
cos(theta) = 0.6
sin(theta) = 0.8
q_local_y = -10 kN/m
```

For an `A -> B` member, the local y axis is:

```text
ey_local = (-sin(theta), cos(theta)) = (-0.8, 0.6)
```

The total local load is:

```text
Q_local_y = -10 * 5 = -50 kN
```

Projected into global coordinates:

```text
Fx_total = +40 kN
Fy_total = -30 kN
```

The resultant acts at the midpoint `(1.5, 2.0)`.

Expected fixed support reaction at A:

```text
RAx = -40 kN
RAy = +30 kN
MA = +125 kNm
```

This benchmark validates local-to-global load projection for inclined Frame2D members.

---

## F03 - Inclined cantilever with GlobalY uniform load

Model:

```text
A = (0, 0)
B = (3, 4)
L = 5 m
q_global_y = -10 kN/m
```

The total global vertical load is:

```text
Fy_total = -10 * 5 = -50 kN
```

The resultant acts at the member midpoint `(1.5, 2.0)`.

Expected fixed support reaction at A:

```text
RAx = 0 kN
RAy = +50 kN
MA = +75 kNm
```

This benchmark validates that global load directions remain global and are not affected by the member inclination.

---

## B05 - Simply supported beam with eccentric nodal point load

Model:

```text
L = 6 m
P = 24 kN downward
load position a = 2 m from A
b = L - a = 4 m
```

Closed-form checks:

```text
RA = P b / L = 24 * 4 / 6 = 16 kN
RB = P a / L = 24 * 2 / 6 = 8 kN
Mmax = P a b / L = 32 kNm
v_load = P a² b² / (3 E I L) = 0.0203174603174603 m downward
```

Expected sign convention in solver output:

```text
node C displacement Y = -0.0203174603174603 m
```

This benchmark is deliberately unsymmetric. It protects against regressions that would still pass symmetric beam checks.

---

## B06 - Simply supported beam with factored load combination

Model:

```text
L = 5 m
G = 10 kN/m downward
Q = 4 kN/m downward
ULS1 = 1.35 G + 1.50 Q
```

Equivalent factored load:

```text
qd = 1.35 * 10 + 1.50 * 4 = 19.5 kN/m
```

Closed-form checks:

```text
RA = qd L / 2 = 48.75 kN
RB = qd L / 2 = 48.75 kN
Vmax = qd L / 2 = 48.75 kN
Mmax = qd L² / 8 = 60.9375 kNm
v_mid = 5 qd L⁴ / (384 E I) = 0.0377836681547619 m downward
```

Expected sign convention in solver output:

```text
midspan displacement Y = -0.0377836681547619 m
```

This benchmark validates combination arithmetic in the analyzer. It is not only a graphical-viewer test.

---

## T02 - Horizontal truss bar axial tension

Model:

```text
L = 5 m
A = 0.004 m²
E = 210000000 kN/m²
P = 20 kN in global +X at node B
```

Closed-form checks:

```text
N_AB = +20 kN tension
u_Bx = P L / (E A) = 0.000119047619047619 m
RAx = -20 kN
```

Expected sign convention in solver output:

```text
positive N = tension
node B displacement X = +0.000119047619047619 m
```

This benchmark also protects the validator change that allows `MomentOfInertia = 0` for sections used only by `Truss2D` members.


---

## B07 - Continuous two-span beam with uniform distributed load

Model:

```text
two equal spans, L = 4 m each
q = 8 kN/m downward on both spans
A and C simple vertical supports, B intermediate vertical support
```

Classical continuous-beam checks for two equal spans loaded identically:

```text
RA = RC = 3 q L / 8 = 12 kN
RB = 5 q L / 4 = 40 kN
|M_B| = q L² / 8 = 16 kNm
```

This benchmark is important because it is statically indeterminate. It validates stiffness assembly and rotational continuity at the internal support, not only basic equilibrium.

---

## B08 - Cantilever frame member with axial tip force

Model:

```text
L = 4 m
A = 0.003 m²
E = 210000000 kN/m²
P = 30 kN in global +X at node B
```

Closed-form axial check:

```text
u_Bx = P L / (E A) = 0.00019047619047619 m
RAx = -30 kN
```

This isolates Frame2D axial stiffness from bending behavior.

---

## T03 - Cantilever triangular truss with horizontal top load

Model:

```text
A = pinned support
B = vertical roller
C = top node at (4, 3)
H = 20 kN in global +X at C
```

Method-of-joints checks for the 3-4-5 triangle:

```text
RAx = -20 kN
RAy = -15 kN
RBy = +15 kN
N_AC = +25 kN tension
N_BC = -15 kN compression
N_AB = 0 kN
```

This test deliberately includes an uplift reaction and a zero-force member. It is useful because symmetric trusses alone do not catch these cases.

---

## M02 - Two-storey braced mixed frame validator

This is not a closed-form textbook solution. It is a larger regression validator for the mixed analyzer.

The applied loads are:

```text
Fx = +25 kN
Fy = -20 kN nodal load + -5 kN/m * 6 m = -50 kN total
```

Therefore the support-reaction resultants must satisfy:

```text
ΣRx = -25 kN
ΣRy = +50 kN
```

The automated benchmark also checks that the solution is stable, all nodal displacements are finite, at least one member force is non-zero and at least one support reaction is non-zero. This type of benchmark is meant to catch regressions in model assembly and mixed Frame2D/Truss2D handling.

---

## B09 - Propped cantilever with uniform distributed load

Model:

```text
A fixed, B vertical prop
L = 6 m
q = 9 kN/m downward
E = 210000000 kN/m²
I = 2e-05 m⁴
```

Classical force-method checks:

```text
RB = 3 q L / 8 = 20.25 kN
RA = 5 q L / 8 = 33.75 kN
MA = q L² / 8 = 40.5 kNm
theta_B = q L³ / (48 E I) = 0.00964285714285714 rad
v_B = 0 by compatibility
```

This benchmark is valuable because it is a one-degree indeterminate problem with a compatibility condition at the prop. It catches mistakes that simple determinate beams cannot catch.

---

## B10 - Fixed-fixed beam with uniform distributed load

Model:

```text
A fixed, B fixed
L = 6 m
q = 12 kN/m downward
```

Classical fixed-end checks:

```text
RA = RB = q L / 2 = 36 kN
|MA| = |MB| = q L² / 12 = 36 kNm
```

The benchmark also checks member end forces. With the solver's local member-end convention, the expected member end moments are:

```text
startMoment = +36 kNm
endMoment = -36 kNm
```

This protects rotational restraint handling and member end-force sign conventions.

---

## B11 - Simply supported beam with triangular distributed load

Model:

```text
L = 6 m
wA = 0 kN/m
wB = 18 kN/m downward
```

Statics checks:

```text
W = wB L / 2 = 54 kN
resultant location = 2L / 3 from A
RA = W / 3 = 18 kN
RB = 2W / 3 = 36 kN
```

This benchmark validates linearly varying distributed-load equivalent nodal loads. It is deliberately asymmetric so that swapped or averaged load handling is easy to detect.

---

## B12 - Moment-released simply supported beam with uniform load

Model:

```text
L = 5 m
q = 10 kN/m downward
member AB has releaseStartMoment = true and releaseEndMoment = true
```

Classical checks:

```text
RA = RB = q L / 2 = 25 kN
Mmax = q L² / 8 = 31.25 kNm
member start moment = 0 kNm
member end moment = 0 kNm
```

This benchmark verifies that moment releases are condensed correctly and that released member-end moments remain zero while the span still carries the correct bending diagram.

---

## F04 - Fixed-base portal frame with lateral nodal load

Model:

```text
single bay: 6 m wide, 4 m high
bases A and B fixed
horizontal nodal load H = +24 kN at top-right node D
```

This is a regression validator based on an independent direct-stiffness calculation. It checks support reactions, top-node displacements and global equilibrium:

```text
ΣRx = -24 kN
ΣRy = 0 kN
```

Representative expected values:

```text
A: Fx = -11.976149979682 kN, Fy = -6.149903913458 kN, Mz = 29.479993481209 kNm
B: Fx = -12.023850020318 kN, Fy =  6.149903913458 kN, Mz = 29.620583072256 kNm
C: Ux = 0.017157833011 m
D: Ux = 0.017255597490 m
```

This benchmark is not meant to replace a full frame-design verification suite. It is a compact regression case for sway behavior, base shear distribution, finite displacements and beam-column coupling.
