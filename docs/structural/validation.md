# StructuralSolver2D - Validation

Validation is a core requirement of StructuralSolver2D.

The engine should not be considered usable until the basic numerical cases are covered by automated tests and compared against known theoretical results.

## Validation principle

Every important part of the solver must be tested:

- model validation;
- member length computation;
- local stiffness matrix;
- coordinate transformation;
- global matrix assembly;
- load vector generation;
- boundary condition application;
- linear system solution;
- support reaction computation;
- internal force recovery;
- failure handling.

## Minimum benchmark cases

### 1. Simply supported beam with uniform distributed load

Schema:

```text
A────────B
    q
```

Expected results:

```text
RA = qL / 2
RB = qL / 2
Mmax = qL² / 8
Vmax = qL / 2
```

This validates:

- support reactions;
- equivalent nodal loads;
- bending moment diagram;
- shear force diagram.

### 2. Simply supported beam with midspan point load

Expected results:

```text
RA = P / 2
RB = P / 2
Mmax = P L / 4
```

This validates point load handling.

### 3. Cantilever with point load at free end

Schema:

```text
A────────B
          ↓ P
```

Expected results:

```text
RAy = P
MA = P · L
free-end deflection = P · L³ / (3 E I)
free-end rotation = P · L² / (2 E I)
```

This validates:

- fixed support;
- rotations;
- deflection;
- bending behavior.

### 4. Cantilever with uniform distributed load

Expected results:

```text
RAy = qL
MA = qL² / 2
free-end deflection = qL⁴ / (8 E I)
```

This validates distributed loads on a cantilever.

### 5. Axial bar in tension

Expected result:

```text
elongation = F L / (E A)
```

This validates axial stiffness.

### 6. Continuous beam

A continuous beam validates:

- multiple members;
- shared nodes;
- internal continuity;
- support reactions in indeterminate structures.

Expected results should be taken from a trusted benchmark or hand calculation.

### 7. Simple portal frame

A portal frame validates:

- inclined or vertical frame elements;
- frame action;
- coupling between axial and bending behavior;
- global matrix assembly.

### 8. Unstable structure

The solver must detect instability.

Example:

```text
single free member with no support
```

Expected behavior:

```text
Analysis failed: the model is unstable or insufficiently constrained.
```

The solver must not produce fake numerical results.

### 9. Zero-length member

Expected behavior:

```text
Validation failed: member length is zero or below tolerance.
```

### 10. Invalid material or section

Expected behavior:

```text
Validation failed: invalid elastic modulus, area or moment of inertia.
```

## Tolerances

Tests should compare floating point values with explicit tolerances.

The tolerance depends on the result type.

Examples:

```text
reaction tolerance:       1e-6 kN for small examples
moment tolerance:         1e-6 kNm for small examples
displacement tolerance:   relative tolerance recommended
```

The project should define both absolute and relative comparison helpers.

## Validation before UI

The graphical interface must not be developed before the calculation engine passes basic validation tests.

A UI can make wrong results look convincing. The engine must be validated first.
