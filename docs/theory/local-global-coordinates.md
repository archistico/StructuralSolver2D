# Local and global coordinates

StructuralSolver2D uses a global two-dimensional coordinate system and member-specific local coordinate systems.

---

## Global coordinate system

The global system uses:

```text
X = horizontal axis
Y = vertical axis
Z = out-of-plane axis
```

Frame rotations are about the global/local Z axis:

```text
Rz
```

---

## Local member coordinate system

Every member has a local coordinate system.

The local X axis is defined from the start node to the end node:

```text
start node  ---- local X ---->  end node
```

The local Y axis is perpendicular to the local X axis in the structure plane.

---

## Why this matters

Local and global coordinates affect:

- stiffness transformation;
- load transformation;
- internal force signs;
- diagram interpretation;
- member orientation behavior;
- A→B / B→A consistency.

Inclined members are especially useful for detecting transformation errors.

---

## Global loads vs local loads

A `GlobalY` load always points in the global Y direction.

A `LocalY` load points in the member local Y direction.

For a horizontal member, these may coincide.

For an inclined member, they are different.

```text
horizontal member:
  GlobalY and LocalY are often visually similar

inclined member:
  GlobalY is vertical
  LocalY is perpendicular to the member
```

---

## Member orientation

The same physical member can be modeled as:

```text
A -> B
```

or:

```text
B -> A
```

The physical structural response should remain consistent, although local result signs may change according to the local axis definition.

This is why the benchmark suite includes orientation tests.

---

## Validation cases

Important local/global validation cases include:

- inclined `Frame2D` member with global nodal load;
- inclined `Frame2D` member with `LocalY` distributed load;
- inclined `Frame2D` member with `GlobalY` distributed load;
- inclined `Truss2D` member with global nodal load;
- same member modeled in opposite directions;
- mixed frame with inclined truss brace.
