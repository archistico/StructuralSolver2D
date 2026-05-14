# Validation examples

This document lists the representative structural examples used as qualitative regression tests.

The goal is not to certify the solver against a design code. These examples verify that common 2D educational schemes are:

- accepted by the model validator;
- solvable through the public API;
- numerically finite;
- globally balanced according to the equilibrium checker;
- compatible with the current `Frame2D` and `Truss2D` modeling assumptions.

The automated test suite is implemented in:

```text
tests/StructuralSolver2D.Analysis.Tests/ValidationExamples/StructuralValidationExampleTests.cs
```

---

## Current validation example suite

| Example | Main member type | Purpose |
|---|---:|---|
| Rigid-joint portal frame | `Frame2D` | Tests a frame with rigid beam-column joints, fixed bases, vertical member load and horizontal nodal action. |
| Small bridge truss | `Truss2D` | Tests a small bridge-like reticular scheme with bottom chord, top chord, posts and diagonals. |
| Isostatic triangular truss beam | `Truss2D` | Tests a simple statically determinate truss beam scheme. |
| Nielsen-style parabolic truss | `Truss2D` | Tests a parabolic upper chord with inclined hangers inspired by Nielsen schemes. |
| Inverted parabolic isostatic truss | `Truss2D` | Tests a didactic inverted parabolic truss arrangement. |
| Hyperstatic double-diagonal truss | `Truss2D` | Tests a redundant double-diagonal truss layout. |
| Gerber beam with asymmetric loads | `Frame2D` | Tests a beam with member-end moment releases and asymmetric point loads. |

---

## What these tests check

For every example, the tests check that:

1. `StructuralModelValidator` accepts the model;
2. `StructuralSolver2DService` completes the analysis;
3. the number of displacements, reactions and member end forces is coherent with the model;
4. internal-force diagrams are produced for all members;
5. displacement diagrams are produced only for `Frame2D` members;
6. all main numerical results are finite;
7. the global residual forces and moments are close to zero.

This is a deliberately robust test style. It avoids locking these examples to fragile exact values while still catching common structural-analysis regressions:

- unstable mechanisms;
- broken public API behavior;
- invalid support/load combinations;
- NaN or infinite numerical output;
- wrong global load/reaction balance;
- accidental post-processing failure on truss-only models.

---

## Important modeling note

`Frame2D` members have three degrees of freedom per node:

```text
Ux, Uy, Rz
```

`Truss2D` members contribute axial stiffness only. They do not provide bending stiffness and do not have a meaningful member displacement diagram based on the frame Hermite interpolation.

For this reason, the public analysis workflow now samples displacement diagrams only for `Frame2D` members. Truss-only models still return nodal displacements, reactions, axial member end forces and internal-force diagrams.

---

## Future improvements

These examples are a first broad regression layer. Future work may add:

- JSON versions under `examples/validation/` for CLI/manual testing;
- expected result snapshots for selected examples;
- graphical reference diagrams;
- independent hand-calculated checks for small isostatic cases;
- comparison reports exported to Markdown and CSV.
