# Theory notes

This folder contains the first educational theory notes for StructuralSolver2D.

The goal is not to replace a structural mechanics textbook. The goal is to explain the assumptions, conventions and numerical ideas used by the codebase, so that the solver can be studied, reviewed and extended with confidence.

Recommended reading order:

1. [`matrix-method.md`](matrix-method.md)
2. [`frame2d-element.md`](frame2d-element.md)
3. [`truss2d-element.md`](truss2d-element.md)
4. [`equivalent-nodal-loads.md`](equivalent-nodal-loads.md)
5. [`local-global-coordinates.md`](local-global-coordinates.md)
6. [`sign-conventions.md`](sign-conventions.md)
7. [`displacement-interpolation.md`](displacement-interpolation.md)
8. [`validation-strategy.md`](validation-strategy.md)

These notes intentionally describe the current solver scope:

- first-order linear elastic analysis;
- planar structures;
- one-dimensional members;
- `Frame2D`, `Truss2D` and mixed plane models;
- static loads;
- no nonlinear, modal, seismic or design-code checks.
