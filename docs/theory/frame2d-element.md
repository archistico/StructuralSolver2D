# Frame2D element

The `Frame2D` element represents a two-dimensional beam-column member with axial, shear/bending and rotational behavior.

It is suitable for:

- beams;
- columns;
- portal frames;
- simple plane frames;
- members with moment releases at one or both ends.

---

## Degrees of freedom

A `Frame2D` member connects two nodes.

Each node has three degrees of freedom:

```text
Ux
Uy
Rz
```

The element therefore has six local/global degrees of freedom:

```text
u1, v1, rz1, u2, v2, rz2
```

where in local coordinates:

- `u` is axial displacement along the member local X axis;
- `v` is transverse displacement along the member local Y axis;
- `rz` is rotation about the out-of-plane Z axis.

---

## Required section and material properties

The current `Frame2D` formulation uses:

```text
E = elastic modulus [kN/m²]
A = cross-section area [m²]
I = second moment of area [m⁴]
L = member length [m]
```

The element combines:

- axial stiffness `EA/L`;
- bending stiffness terms based on `EI` and `L`.

---

## Local and global coordinates

The element stiffness is naturally defined in local coordinates and then transformed to the global system.

The local X axis follows the member orientation from start node to end node.

```text
start node  ---- local X ---->  end node
```

The local Y axis is perpendicular to the member in the element plane.

The transformation step is one of the most important parts of the solver. It is heavily tested through inclined-member and local/global load benchmarks.

---

## Moment releases

`Frame2D` members can currently release bending moment at the start and/or end node:

```text
ReleaseStartMoment
ReleaseEndMoment
```

Moment releases are used to model hinged member ends and internal hinges.

The current implementation uses local static condensation for the released rotational degrees of freedom.

Important consequence:

- a released end should not transfer bending moment;
- the model may become unstable if too many releases remove structural restraint;
- isolated inactive rotational degrees of freedom must not make the global system singular.

---

## Current load support

For `Frame2D` members, the solver currently supports:

- nodal forces;
- nodal moments;
- uniform distributed loads;
- linear distributed loads;
- triangular/trapezoidal loads;
- point loads on members;
- load cases;
- manual load combinations.

Member loads may be expressed in global or local directions where supported.

---

## Current limitations

The `Frame2D` element is not currently a nonlinear beam-column element.

Not supported yet:

- second-order effects;
- plastic hinges;
- shear deformation/Timoshenko formulation;
- member buckling checks;
- design-code checks;
- connection design;
- distributed stiffness variation along the member.
