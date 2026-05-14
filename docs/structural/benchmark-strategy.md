# Benchmark strategy

StructuralSolver2D should be validated by a growing catalog of small, transparent and reproducible benchmark models.

The validation strategy has three levels:

1. **closed-form benchmarks**: beams, cantilevers and simple trusses with known manual solutions;
2. **equilibrium and symmetry benchmarks**: simple frames where equilibrium and structural symmetry can be checked even before detailed reference values are available;
3. **external reference benchmarks**: future models compared against trusted literature, university examples, NAFEMS-style benchmarks or independent structural software.

---

## Principles

Each benchmark should include:

- a clear ID;
- a short description;
- the model file;
- the analysis ID to run;
- expected values;
- tolerances;
- formulas or reference source;
- an explanation of what the benchmark validates.

---

## Current benchmark groups

```text
benchmarks/beams
benchmarks/trusses
benchmarks/frames
benchmarks/expected
```

---

## Immediate purpose

Milestone 17 introduces the benchmark catalog as data and documentation.

Milestone 18 should add an automated benchmark runner that reads:

```text
benchmarks/expected/expected-results.json
```

and compares the solver output against expected values.

---

## Benchmark quality levels

### Level 1 - hand-checkable

Examples:

- simply supported beam with uniform distributed load;
- simply supported beam with point load at midspan;
- cantilever with tip point load;
- cantilever with uniform distributed load;
- simple triangular truss.

These benchmarks should use strict numerical tolerances.

### Level 2 - equilibrium and symmetry

Examples:

- symmetric portal frame with vertical beam load;
- symmetric frame with symmetric horizontal loading;
- model with opposite reactions by symmetry.

These benchmarks are useful before exact closed-form reference values are documented.

### Level 3 - external reference

Examples:

- published FEM benchmark problems;
- NAFEMS-style benchmark problems;
- examples from structural analysis textbooks;
- independent software validation cases.

These benchmarks should cite the source and record the solver version used for comparison.

---

## Tolerances

Recommended starting tolerances:

| Check type | Suggested tolerance |
|---|---:|
| Reactions from simple beam formulas | `1e-6` |
| Displacements from closed-form beam formulas | `1e-6` to `1e-5` |
| Internal force extrema from sampled diagrams | `1e-4` |
| Symmetry/equilibrium checks | `1e-6` |

Sampling-based checks should use slightly looser tolerances when the exact maximum does not fall exactly on a sampled point.

---

## Automated benchmark runner

Milestone 18 introduced an automated benchmark test runner in:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalogTests.cs
```

The runner reads:

```text
benchmarks/expected/expected-results.json
```

and executes each referenced JSON model using the same JSON reader used by the CLI. This means the benchmark suite validates both the solver and the example input files.

The current runner checks:

- support reactions;
- global maximum absolute shear and bending moment for Frame2D benchmarks;
- selected nodal and member-interpolated displacements;
- Truss2D member axial forces;
- named stability/symmetry/equilibrium checks for the first portal-frame benchmark.

When new benchmark JSON files are added, their expected results should be added to `benchmarks/expected/expected-results.json`. If the new expected quantities are not yet supported by the runner, the runner should be extended before the benchmark is considered part of the automatic regression suite.


---

## Milestone 24 - local/global conventions and inclined members

Milestone 24 extends the benchmark and test suite with inclined-member checks.

The goal is to protect one of the most error-prone parts of a structural solver: the relationship between local member axes and global model axes.

New validation topics include:

- 3-4-5 inclined Frame2D member geometry;
- global nodal load on an inclined cantilever;
- `LocalY` uniform member load projected into global coordinates;
- `GlobalY` uniform member load independent from member inclination;
- reversed member orientation `A -> B` and `B -> A`;
- axial load along an inclined member;
- mixed Frame2D + Truss2D behavior with an inclined brace.

Two benchmark files were added to the automatic catalog:

```text
benchmarks/frames/F02-inclined-cantilever-local-y.json
benchmarks/frames/F03-inclined-cantilever-global-y.json
```

An additional mixed benchmark model was added for manual and future automated checks:

```text
benchmarks/mixed/M01-braced-portal-inclined-truss.json
```

Important convention:

```text
Global loads remain expressed in global axes.
Local loads are expressed in member axes and therefore rotate when the member orientation changes.
```

---

## Milestone 26 — Mesh refinement and convergence benchmarks

Milestone 26 adds a dedicated convergence layer to the benchmark strategy.

The goal is not only to check a single numerical value, but also to document how the result changes when the same structural problem is represented with a finer mesh.

The initial convergence files are stored in:

```text
benchmarks/convergence/
```

Current convergence cases:

```text
C01-simple-supported-udl-1-elements.json
C01-simple-supported-udl-2-elements.json
C01-simple-supported-udl-4-elements.json
C01-simple-supported-udl-8-elements.json
C02-point-load-single-element.json
C02-point-load-explicit-node.json
```

These cases highlight an important FEM principle:

> internal sampled values depend on the element interpolation field, while explicitly modeled nodes provide nodal degrees of freedom that can be compared directly with closed-form values.

For future benchmarks, each convergence case should document:

- the reference closed-form solution, when available;
- the sequence of meshes used;
- the quantity being monitored;
- whether the value is nodal or internally interpolated;
- the expected convergence behavior.


---

## Milestone 28 — Improved benchmark runner

Milestone 28 refactors the benchmark runner into smaller test-side components.

The benchmark catalog now has a dedicated representation and validation step. Before executing the numerical comparisons, the tests check that:

- the catalog contains benchmark entries;
- benchmark ids are unique;
- benchmark names are not empty;
- model paths are not empty;
- referenced model files exist;
- analysis ids are not empty;
- tolerances are finite and positive.

The runner is split into:

```text
BenchmarkCatalog
BenchmarkRepository
BenchmarkAnalysisRunner
BenchmarkResultAssertions
```

This makes the benchmark layer easier to evolve. Future expected-result types, such as internal-force values at specific positions, symmetry objects or convergence criteria, should be added to the benchmark assertion layer rather than directly inside the main test method.

The benchmark runner currently supports:

- support reactions;
- nodal displacements and rotations;
- Frame2D global extrema;
- Truss2D member axial forces;
- named stability and symmetry checks;
- global equilibrium residuals.
