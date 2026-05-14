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
