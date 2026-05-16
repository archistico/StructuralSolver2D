# Benchmark strategy

StructuralSolver2D should be validated by a growing catalog of small, transparent and reproducible benchmark models.

The validation strategy has four levels:

1. **closed-form benchmarks**: beams, cantilevers and simple trusses with known manual solutions;
2. **classical indeterminate benchmarks**: university-style continuous beams and frames with documented reference values;
3. **equilibrium and regression validators**: larger models where global equilibrium, finite response and qualitative force behavior are checked;
4. **external reference benchmarks**: future models compared against trusted literature, university examples, NAFEMS-style benchmarks or independent structural software.

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
benchmarks/mixed
benchmarks/convergence
benchmarks/expected
```

---

## Current purpose

The benchmark catalog is an active validation asset of the project.

The automated benchmark runner reads:

```text
benchmarks/expected/expected-results.json
```

and compares solver output against documented expected values.

The operational validation workflow is documented in:

```text
docs/structural/validation-manual.md
```

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

### Level 2 - classical indeterminate

Examples:

- continuous two-span beam under uniform load;
- fixed-end and propped cantilever cases;
- small portal frames with published stiffness-method solutions.

These benchmarks should still have explicit numerical reference values, but they validate more than basic equilibrium.

### Level 3 - equilibrium and regression validators

Examples:

- symmetric portal frame with vertical beam load;
- mixed Frame2D + Truss2D braced frame;
- larger models with reaction-resultant checks, finite displacements and non-zero member forces.

These cases are not substitutes for closed-form checks. They protect the assembly path and catch regressions in larger realistic models.

### Level 4 - external reference

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
- support-reaction resultants;
- global maximum absolute shear and bending moment for Frame2D benchmarks;
- selected nodal and member-interpolated displacements;
- Truss2D member axial forces;
- named stability/symmetry/equilibrium checks;
- finite displacement response for larger validators;
- non-zero member forces and support reactions when requested.

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

---

## Strengthened benchmark catalog

The benchmark layer is now treated as a first-class verification asset, not as a collection of demo files.

Each automatic benchmark entry must provide:

- `id` and `name`;
- `modelPath`;
- `analysisId`, which may be either a load case or a load combination;
- `tolerance`;
- `reference`, describing the closed-form formula, symmetry principle or external source used;
- `purpose`, explaining which solver behavior the benchmark protects;
- `expected`, containing the numerical quantities asserted by the test runner.

The catalog validation test fails when `reference` or `purpose` is missing. This makes it harder to add opaque regression cases whose expected numbers cannot be audited later.

New closed-form validation cases include:

```text
B05 - Simply supported beam with eccentric nodal point load
B06 - Simply supported beam with factored load combination
T02 - Horizontal truss bar axial tension
```

These cases extend the coverage in three important directions:

1. **unsymmetric beam behavior**: B05 catches bugs that symmetric beam tests may miss;
2. **load combination arithmetic**: B06 validates factored loads in the analyzer, not only in visualization;
3. **pure truss axial behavior**: T02 validates axial stiffness, positive tension sign convention and sections with zero bending inertia when used only by `Truss2D` members.

When adding future benchmarks, prefer closed-form cases first. More complex frame and mixed models should enter the automatic catalog only when they have either a documented hand solution, a clear stiffness-method derivation or an independent reference result.


---

## Classical and validator benchmark expansion

The benchmark suite now includes two additional closed-form beam cases, one unsymmetric truss case and one larger mixed validator:

```text
B07-continuous-two-span-udl.json
B08-cantilever-axial-tip-force.json
T03-cantilever-truss-horizontal-load.json
M02-two-storey-braced-frame-validator.json
```

The purpose is to cover four distinct risks:

- indeterminate beam stiffness and internal support continuity;
- Frame2D axial stiffness independent from bending;
- truss uplift/zero-force-member behavior;
- mixed Frame2D + Truss2D assembly on a larger model.

Closed-form benchmarks should remain strict. Larger validator models may use reaction resultants and qualitative checks until a trusted external reference solution is available.

---

## Extended classic benchmark layer

The benchmark suite should grow in layers, not by adding arbitrary large models.

The current extended layer adds the following protections:

| Case | Structural behavior | Why it matters |
|---|---|---|
| B09 propped cantilever | one-degree indeterminacy and compatibility at a prop | catches stiffness/constraint mistakes that determinate beams hide |
| B10 fixed-fixed beam | double rotational restraint and fixed-end actions | validates end moments and member end-force sign conventions |
| B11 triangular distributed load | linearly varying member load | protects asymmetric equivalent nodal load assembly |
| B12 released simply supported beam | moment-release condensation | verifies zero released end moments and preservation of span response |
| F04 lateral portal frame | sway behavior and beam-column coupling | regression validator for a realistic frame response |

A benchmark may have either:

1. **closed-form expected values**, preferred for beams and simple trusses;
2. **independent direct-stiffness expected values**, acceptable for compact frame validators;
3. **global invariant checks**, useful for larger realistic models where a full hand solution would be misleading.

The important rule is that the reference type must be explicit in `expected-results.json` and in the human-readable documentation. A regression validator is useful, but it should not be presented as a textbook closed-form benchmark.

## Member end-force checks

Some defects do not appear in reactions or nodal displacements. For that reason the catalog now supports `memberEndForces` entries.

Use them when validating:

- fixed-end moment signs;
- released end moments;
- shear sign conventions;
- local member-force reporting.

Example:

```json
"memberEndForces": [
  {
    "memberId": "AB",
    "startMoment": 0.0,
    "endMoment": 0.0
  }
]
```

For released members, checking zero end moments is often more robust than checking only global reactions.

---

## Validation manual

For the complete practical workflow, benchmark catalog, formula summary, sign conventions, tolerance policy and release checklist, see:

```text
docs/structural/validation-manual.md
```
