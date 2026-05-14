# Matrix method overview

StructuralSolver2D uses the displacement method, also known as matrix stiffness analysis.

The basic equation is:

```text
K · u = F
```

where:

- `K` is the global stiffness matrix;
- `u` is the vector of unknown nodal displacements;
- `F` is the global load vector.

For a static linear elastic model, once the unknown displacements are found, the solver can compute:

- support reactions;
- member end forces;
- internal force diagrams;
- displacement/deformed-shape samples.

---

## Main analysis steps

The current analysis pipeline is conceptually:

```text
StructuralModel
    ↓
model validation
    ↓
element stiffness matrices
    ↓
local-to-global transformation
    ↓
global stiffness assembly
    ↓
equivalent nodal load vector
    ↓
boundary conditions
    ↓
linear system solution
    ↓
reactions and member forces
    ↓
post-processing
```

---

## Degrees of freedom

For `Frame2D` models, each node has three global degrees of freedom:

```text
Ux = horizontal displacement
Uy = vertical displacement
Rz = rotation about the out-of-plane Z axis
```

For pure `Truss2D` models, only translational degrees of freedom are structurally active:

```text
Ux
Uy
```

For mixed `Frame2D + Truss2D` models, the global plane-structure analyzer keeps the common `Ux`, `Uy`, `Rz` layout. `Truss2D` members only contribute stiffness to translational degrees of freedom.

---

## Boundary conditions

Supports restrain selected nodal degrees of freedom.

Typical examples:

```text
Pinned support:
  Ux restrained
  Uy restrained
  Rz free

Roller support:
  Ux free
  Uy restrained
  Rz free

Fixed support:
  Ux restrained
  Uy restrained
  Rz restrained
```

After supports are applied, the solver partitions or reduces the global system to solve only for free active degrees of freedom.

The solver also handles the valid special case where all active degrees of freedom are restrained. In that situation displacements are zero and reactions/member end forces can still be computed.

---

## Linear elastic assumptions

The current solver assumes:

- small displacements;
- linear elastic material behavior;
- constant member properties along each element;
- static loads;
- no second-order effects;
- no material plasticity;
- no contact;
- no dynamic or modal analysis.

These assumptions must remain explicit in documentation and reports.
