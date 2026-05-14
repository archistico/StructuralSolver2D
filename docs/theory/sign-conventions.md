# Sign conventions

Sign conventions must be explicit because structural analysis errors often come from inconsistent signs rather than incorrect formulas.

This document records the conventions currently used by StructuralSolver2D.

---

## Global directions

The global coordinate system uses:

```text
+X = positive horizontal direction
+Y = positive vertical direction
+Mz = positive out-of-plane moment according to the right-hand rule
```

A downward vertical load is usually entered as a negative `GlobalY` value.

Example:

```json
{
  "direction": "GlobalY",
  "value": -10.0
}
```

---

## Local member directions

The local X axis follows the member start-to-end orientation.

The local Y axis is perpendicular to the local X axis in the element plane.

Changing the member orientation may change local result signs, but the global physical response should remain equivalent.

---

## Axial force N

`N` is the axial force along the member local X axis.

The exact sign convention must be kept consistent in:

- member end forces;
- internal force samples;
- truss axial force results;
- reports;
- benchmark expected values.

The benchmark suite should prefer explicit expected values for simple axial tests and orientation-pair tests.

---

## Shear force V and bending moment M

For horizontal left-to-right beam examples, the reporting convention is intended to make simply supported downward-loaded beams produce positive sagging moment in the span.

Important practical convention:

- simply supported beam with downward load: positive span bending moment;
- cantilever with downward load at the free end: negative fixed-end bending moment in the current report convention.

These conventions must remain documented because future UI diagrams will depend on them.

---

## Reactions

Support reactions are reported in global directions.

For a downward load, upward vertical support reactions are positive `Fy`.

Global equilibrium should satisfy:

```text
ΣFx ≈ 0
ΣFy ≈ 0
ΣMz ≈ 0
```

where applied loads and support reactions are summed together.

---

## Recommended test strategy

Whenever a sign convention is changed or clarified, add tests for:

- simply supported beam under downward load;
- cantilever under downward tip load;
- axial bar in tension;
- inclined member with global force;
- same member modeled A→B and B→A;
- global equilibrium residuals.
