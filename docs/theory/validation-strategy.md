# Validation strategy

StructuralSolver2D should grow through validated, testable steps.

The project rule is:

> no significant new feature should be added without dedicated validation benchmarks.

---

## Validation layers

The project uses several validation layers.

### 1. Unit tests

Unit tests verify isolated pieces of logic:

- model validation;
- load validation;
- element matrices;
- result summarization;
- report generation;
- JSON input reading.

### 2. Analytical tests

Analytical tests compare solver results with closed-form structural mechanics formulas.

Examples:

- simply supported beam with uniform load;
- simply supported beam with point load;
- cantilever with tip load;
- cantilever with uniform load;
- axial bar in tension;
- simple triangular truss.

### 3. Benchmark catalog

The benchmark catalog stores JSON models and expected results.

Files are organized under:

```text
benchmarks/
  beams/
  frames/
  trusses/
  mixed/
  convergence/
  expected/
```

The automated benchmark runner reads the catalog and compares computed results with expected values.

### 4. Global equilibrium checks

The global equilibrium checker verifies:

```text
ΣFx ≈ 0
ΣFy ≈ 0
ΣMz ≈ 0
```

This is a cross-cutting validation method that helps detect sign errors, missing loads and incorrect transformations.

### 5. Mesh refinement checks

Mesh refinement benchmarks compare multiple discretizations of the same problem.

They document how FEM results converge as the model is refined.

---

## What should be benchmarked

Every important solver capability should have benchmark coverage:

- nodal forces;
- nodal moments;
- uniform distributed loads;
- linear distributed loads;
- point loads on members;
- load combinations;
- moment releases;
- inclined members;
- local/global load directions;
- truss behavior;
- mixed frame/truss models;
- deformation sampling;
- global equilibrium.

---

## Expected-result quality

Expected results should be based on one of these sources:

1. closed-form formulas;
2. independent hand calculations;
3. trusted benchmark references;
4. carefully documented regression values for cases without simple closed-form results.

When a benchmark uses regression values rather than closed-form values, the documentation must say so clearly.

---

## Professional benchmark direction

Future benchmark work may include cases inspired by:

- NAFEMS-style verification problems;
- OpenSees examples;
- software verification examples from structural analysis tools;
- university lecture examples.

When using external sources, do not copy long copyrighted text. Use independent explanations, minimal required numerical data, assumptions and proper references.

---

## Failure policy

A failing benchmark is valuable.

When a benchmark fails, first determine whether the problem is:

- a solver bug;
- an expected-result error;
- an invalid model;
- a sign-convention mismatch;
- a discretization/interpolation issue;
- a tolerance that is too strict or too loose.

Do not hide failures by relaxing tolerances without explaining why.
