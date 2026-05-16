# StructuralSolver2D - Analysis

This document describes the initial analysis model of StructuralSolver2D.

## Initial analysis type

The first supported analysis is:

```text
static linear elastic first-order analysis of planar frame structures
```

Assumptions:

- small displacements;
- linear elastic materials;
- constant member sections;
- ideal supports;
- static loads;
- no second-order effects;
- no material nonlinearities;
- no dynamic effects.

## Fundamental equation

The solver resolves:

```text
K · u = F
```

Where:

```text
K = global stiffness matrix
u = unknown displacement vector
F = global load vector
```

After solving for `u`, the engine computes:

- support reactions;
- member end forces;
- internal force diagrams;
- maximum/minimum result values.

## Initial element: Frame2D

The initial finite element is the 2D frame element.

Each node has three degrees of freedom:

```text
Ux
Uy
Rz
```

Each Frame2D element has six degrees of freedom:

```text
Ux1
Uy1
Rz1
Ux2
Uy2
Rz2
```

This element can represent:

- beams;
- columns;
- portal frames;
- simple planar frames;
- cantilevers;
- continuous beams.

## Required section and material properties

For each Frame2D member, the solver needs:

```text
E = elastic modulus
A = cross-sectional area
I = second moment of area
L = member length
```

Internal units:

```text
E = kN/m²
A = m²
I = m⁴
L = m
```

## Local and global coordinates

The member stiffness matrix is first defined in local coordinates.

The solver must transform each member contribution into global coordinates before assembly.

Required steps:

```text
1. compute member length L
2. compute direction cosines
3. build local stiffness matrix
4. build transformation matrix
5. transform local stiffness to global stiffness
6. assemble into global K
```

## Boundary conditions

Supports restrain individual degrees of freedom.

The solver must apply boundary conditions to the global system before solving.

Supported DOF restraints:

```text
RestrainedUx
RestrainedUy
RestrainedRz
```

`RestrainedUx` and `RestrainedUy` are interpreted in the support local system.
For a non-rotated support, the local support axes coincide with global X and global Y.
When `OrientationDegrees` is different from zero, the translational constraint equations are rotated accordingly.
This allows mechanically active inclined rollers and inclined translational supports without changing the JSON support schema.

## Support reactions

Support reactions are computed from the global residual vector:

```text
r = K · u - F
```

The result fields `Fx` and `Fy` are always global force components.
The result field `Mz` is the global moment component.

This distinction is important for inclined supports. A roller restrained along a single local direction can produce two non-zero global components because the local reaction vector is decomposed into global X and global Y. Therefore:

- a vertical roller with only global/local `Uy` restrained should normally report `Fx = 0`;
- a horizontal roller with only global/local `Ux` restrained should normally report `Fy = 0`;
- an inclined roller may correctly report both `Fx` and `Fy` as non-zero.

The public result currently exposes global components only. If local support reaction components become necessary for reporting, they should be added explicitly to the reaction DTO rather than reinterpreting the meaning of `Fx` and `Fy`.

## Loads

Initial load handling:

- nodal forces are inserted directly into the global load vector;
- nodal moments are inserted directly into the global load vector;
- member distributed loads are converted into equivalent nodal loads;
- member point loads are converted into equivalent nodal loads.

The exact sign convention must be documented and tested.

## Sign conventions

The project must define explicit sign conventions before implementing diagrams.

Recommended global convention:

```text
X positive to the right
Y positive upward
Rz positive counterclockwise
```

For internal forces, the convention must be documented separately and kept consistent in tests, reports and diagrams.

## Current solver implementation limits

The current linear equation solver is a dense Gaussian-elimination solver with partial pivoting.
It is intentionally simple and suitable for small/medium educational, validation and prototype models.

The dense implementation stores the full matrix and has approximately cubic computational cost with respect to the number of unknowns. This means it is not intended for large production-scale finite element models. Future larger models should use a sparse matrix assembly and sparse linear solver.

This is an implementation limit, not a modeling convention: the public API is expected to remain stable when the internal solver is replaced.

## Failure cases

The solver must fail in a controlled way when:

- the structure is unstable;
- the stiffness matrix is singular;
- a member has zero length;
- a material has invalid E;
- a section has invalid A or I;
- a load references a missing entity;
- a support references a missing node.

No analysis should return apparently valid numbers for an invalid model.

## Not included in the first analysis model

The first solver does not include:

- nonlinear geometric analysis;
- nonlinear material analysis;
- plastic hinges;
- buckling analysis;
- second-order effects;
- modal analysis;
- response spectrum analysis;
- time-history analysis;
- plate, shell or solid elements;
- staged construction;
- contact;
- support settlements;
- thermal loads.
