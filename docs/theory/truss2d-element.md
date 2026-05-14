# Truss2D element

The `Truss2D` element represents an axial-only member in a plane truss.

It is suitable for:

- bielle;
- simple trusses;
- bracing members;
- axial-only members in mixed plane structures.

---

## Structural behavior

A `Truss2D` member carries only axial force.

It does not carry:

- bending moment;
- shear force;
- torsion;
- rotational stiffness.

Its only internal force result is the axial force `N`.

---

## Degrees of freedom

In a pure truss model, each node has two active degrees of freedom:

```text
Ux
Uy
```

In a mixed `Frame2D + Truss2D` model, the global analyzer still uses:

```text
Ux, Uy, Rz
```

but the `Truss2D` member contributes only to `Ux` and `Uy`.

---

## Required properties

The current `Truss2D` formulation uses:

```text
E = elastic modulus [kN/m²]
A = cross-section area [m²]
L = member length [m]
```

The axial stiffness is:

```text
EA / L
```

---

## Local/global transformation

The member local X axis follows the start-to-end orientation of the member.

An inclined truss member must correctly transform axial stiffness into the global X/Y system.

This is why the benchmark suite includes inclined-member tests and A→B / B→A orientation checks.

---

## Current load support

The pure `Truss2DAnalyzer` supports nodal loads in global directions.

Distributed loads and point loads on `Truss2D` members are not supported because a truss member has no bending behavior in the current formulation.

For braced frames, use the mixed `PlaneStructureAnalyzer` with:

- `Frame2D` members for beams/columns;
- `Truss2D` members for axial braces;
- nodal loads applied to nodes.

---

## Current limitations

Not supported yet:

- member buckling checks;
- tension-only/compression-only behavior;
- nonlinear axial behavior;
- cable behavior;
- connection eccentricities;
- distributed loads applied directly on truss members.
