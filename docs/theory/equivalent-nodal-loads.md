# Equivalent nodal loads

Member loads must be converted into equivalent nodal loads before the global system can be solved.

The global equation is:

```text
K · u = F
```

The vector `F` contains nodal forces and moments. Therefore, loads applied along a member must be transformed into nodal force components that produce an equivalent structural effect for the finite element.

---

## Supported load families

The current solver supports:

- nodal forces;
- nodal moments;
- uniform distributed loads on Frame2D members;
- linear distributed loads on Frame2D members;
- triangular/trapezoidal loads on Frame2D members;
- point loads on Frame2D members;
- manual load combinations.

---

## Global and local directions

Member loads may be defined in global or local directions, depending on the load type.

Examples:

```text
GlobalY = vertical load in the global coordinate system
LocalY  = load perpendicular to the member local X axis
LocalX  = load along the member axis
```

For inclined members this distinction is essential.

A vertical global load on an inclined member is not the same as a local transverse load.

---

## Load combinations

Manual load combinations are handled by factoring load cases before assembling the load vector.

Example:

```text
ULS1 = 1.35 G1 + 1.50 Q1
```

The solver does not generate code-compliant normative combinations. It only analyzes combinations explicitly defined by the user.

---

## Internal-force recovery

When member loads exist, internal forces are not obtained only from element end displacements.

The recovery step must account for:

- member stiffness contribution;
- equivalent nodal load contribution;
- load discontinuities along the member;
- point load positions;
- distributed load variation.

This is why point loads and distributed loads are tested not only by reactions, but also by shear/moment diagrams and global equilibrium.

---

## Validation strategy

Equivalent nodal loads should be validated with:

- closed-form beam reactions;
- global equilibrium checks;
- internal force diagram checks;
- local/global inclined-member tests;
- A→B / B→A orientation tests.
