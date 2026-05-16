# Validation Manual

This manual explains how StructuralSolver2D is numerically validated.

The goal is not only to have many tests, but to have tests that are understandable, reproducible and useful for detecting real structural-analysis regressions.

StructuralSolver2D is currently intended for 2D educational, prototyping and validation-oriented structural analysis. It is not yet a certified professional design tool. The validation suite is therefore treated as a central engineering asset of the project.

---

## Validation goals

The validation suite is designed to answer four questions:

1. **Does the solver reproduce elementary closed-form results?**
2. **Does it behave correctly on classical indeterminate structures?**
3. **Does it preserve equilibrium and sign conventions on larger models?**
4. **Does it keep working when JSON input, load combinations, reporting and visualization evolve?**

A benchmark is only useful when it states:

- the model file;
- the analysis or combination to run;
- the expected quantities;
- the numerical tolerance;
- the formula, reference or validation principle;
- the solver behavior protected by the test.

---

## Internal unit system

All validation models use the internal StructuralSolver2D unit system.

| Quantity | Unit |
|---|---:|
| Length | m |
| Force | kN |
| Moment | kNm |
| Elastic modulus | kN/m² |
| Area | m² |
| Second moment of area | m⁴ |
| Distributed load | kN/m |
| Rotation | rad |

A wrong unit convention can make a benchmark appear wrong even when the solver is correct. Do not mix N/mm², mm, kN and m without explicit conversion.

---

## Validation layers

StructuralSolver2D uses five validation layers.

### Level 1 — closed-form benchmarks

These are small problems with well-known analytical solutions.

Examples:

- simply supported beam with uniform load;
- simply supported beam with point load;
- cantilever with tip load;
- cantilever with uniform load;
- simple triangular truss;
- axial bar in tension.

These benchmarks should use strict tolerances because the expected values come directly from formulas.

### Level 2 — classical indeterminate benchmarks

These are university-style structures that require compatibility and stiffness, not only equilibrium.

Examples:

- continuous two-span beam;
- propped cantilever;
- fixed-fixed beam;
- beam with moment releases;
- simple portal frame.

These cases protect the assembly process, rotational DOFs, member end forces and boundary conditions.

### Level 3 — regression validators

These are larger or more realistic models where not every value is checked from a closed-form formula.

They verify robust invariants such as:

- global equilibrium;
- finite displacements;
- non-zero internal forces;
- expected reaction resultants;
- expected qualitative behavior, such as frame sway or active bracing.

These cases are useful, but they are not a substitute for Levels 1 and 2.

### Level 4 — edge-case robustness validators

These tests intentionally exercise problematic input rather than normal structural examples.

They cover:

- singular or unstable structures;
- insufficient restraints and rigid-body modes;
- member orientations very close to global axes;
- very small and very large loads;
- moment releases that create mechanisms;
- load combinations with negative or zero factors.

Expected behavior is not always a numerical result. For invalid or unstable cases, the correct result is a clear validation or analysis exception. For numerically extreme but valid cases, the checks verify finite results, equilibrium and linear scaling.

These cases live in:

```text
benchmarks/edge-cases/
```

and are exercised by:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/EdgeCaseValidationTests.cs
```

### Level 5 — external reference benchmarks

These are future validation cases compared against independent sources:

- structural analysis textbooks;
- university examples;
- published benchmark problems;
- independent commercial/open-source structural solvers;
- NAFEMS-style cases where applicable.

External benchmarks should always include the source, assumptions, units and solver version used for comparison.

---

## Benchmark files

Benchmark models are stored in:

```text
benchmarks/
  beams/
  trusses/
  frames/
  mixed/
  convergence/
  edge-cases/
  expected/
```

Expected results are stored in two forms:

```text
benchmarks/expected/expected-results.json
benchmarks/expected/expected-results.md
```

The JSON file is the source used by automated tests. The Markdown file is the human-readable explanation of the expected values.

---

## Automated test runner

The benchmark test runner is located in:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalogTests.cs
```

Supporting classes:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalog.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkResultAssertions.cs
```

The runner reads:

```text
benchmarks/expected/expected-results.json
```

Then it loads the referenced JSON model using the same input path used by the CLI. This means the benchmark suite validates both:

- the structural analysis engine;
- the benchmark JSON input files.

Run the complete validation suite with:

```powershell
dotnet test
```

Run only analysis tests with:

```powershell
dotnet test tests\StructuralSolver2D.Analysis.Tests
```

Run only benchmark catalog tests with a filter:

```powershell
dotnet test tests\StructuralSolver2D.Analysis.Tests --filter BenchmarkCatalog
```

---

## What the benchmark runner checks

Depending on the benchmark, the automated catalog can check:

| Check type | Purpose |
|---|---|
| `supportReactions` | individual support `Fx`, `Fy`, `Mz` components |
| `reactionSums` | global resultant equilibrium checks |
| `nodalDisplacements` | selected nodal translations/rotations |
| `tipDisplacementY` | legacy/simple cantilever tip deflection check |
| `tipRotationZ` | legacy/simple cantilever tip rotation check |
| `memberDisplacements` | interpolated member displacement at selected stations |
| `memberAxialForces` | Truss2D axial force checks |
| `memberEndForces` | Frame2D local end-force checks |
| `maxAbsShear` | maximum absolute shear from diagrams |
| `maxAbsMoment` | maximum absolute bending moment from diagrams |
| `checks` | named structural invariants such as finite response or symmetry |

When adding a benchmark, prefer direct numerical checks over vague qualitative checks. Use qualitative checks only when there is no reliable closed-form value.

---

## Current automatic benchmark catalog

| ID | Type | Model | Validation level | Main purpose |
|---|---|---|---:|---|
| B01 | Beam | `benchmarks/beams/B01-simple-supported-udl.json` | 1 | UDL reactions, shear, bending moment and midspan deflection |
| B02 | Beam | `benchmarks/beams/B02-simple-supported-point-midspan.json` | 1 | centered point load, symmetry and deflection |
| B03 | Beam | `benchmarks/beams/B03-cantilever-tip-point-load.json` | 1 | cantilever tip load, fixed reaction/moment, tip displacement/rotation |
| B04 | Beam | `benchmarks/beams/B04-cantilever-udl.json` | 1 | cantilever UDL equivalent loads and tip response |
| T01 | Truss | `benchmarks/trusses/T01-symmetric-triangular-truss.json` | 1 | truss method-of-joints forces and tension/compression signs |
| F01 | Frame | `benchmarks/frames/F01-portal-symmetric-gravity.json` | 3 | portal symmetry, stable solution and vertical equilibrium |
| F02 | Frame | `benchmarks/frames/F02-inclined-cantilever-local-y.json` | 2 | local load projection on inclined Frame2D member |
| F03 | Frame | `benchmarks/frames/F03-inclined-cantilever-global-y.json` | 2 | global load direction on inclined Frame2D member |
| B05 | Beam | `benchmarks/beams/B05-simple-supported-eccentric-point-load.json` | 1 | unsymmetric point-load reactions and eccentric maximum moment |
| B06 | Beam | `benchmarks/beams/B06-simple-supported-load-combination.json` | 1 | manual load-combination arithmetic in solver output |
| T02 | Truss | `benchmarks/trusses/T02-horizontal-bar-axial-tension.json` | 1 | axial bar stiffness and `MomentOfInertia = 0` truss section |
| B07 | Beam | `benchmarks/beams/B07-continuous-two-span-udl.json` | 2 | continuous-beam compatibility and intermediate support reaction |
| B08 | Beam/Frame | `benchmarks/beams/B08-cantilever-axial-tip-force.json` | 1 | Frame2D axial stiffness isolated from bending |
| T03 | Truss | `benchmarks/trusses/T03-cantilever-truss-horizontal-load.json` | 1 | 3-4-5 truss, uplift reaction, zero-force member |
| M02 | Mixed | `benchmarks/mixed/M02-two-storey-braced-frame-validator.json` | 3 | larger mixed Frame2D + Truss2D regression validator |
| B09 | Beam | `benchmarks/beams/B09-propped-cantilever-udl.json` | 2 | one-degree indeterminate beam and prop compatibility |
| B10 | Beam | `benchmarks/beams/B10-fixed-fixed-beam-udl.json` | 2 | fixed-end moments and member end-force sign convention |
| B11 | Beam | `benchmarks/beams/B11-simple-supported-triangular-load.json` | 1 | linearly varying distributed load resultant and reactions |
| B12 | Beam | `benchmarks/beams/B12-released-simply-supported-beam-udl.json` | 2 | moment releases and zero local end moments |
| F04 | Frame | `benchmarks/frames/F04-portal-lateral-load-validator.json` | 3 | portal sway, lateral load equilibrium and frame coupling |

### Current edge-case robustness validators

| ID | Model | Validation level | Expected behavior |
|---|---|---:|---|
| E01 | `benchmarks/edge-cases/E01-unstable-free-frame.json` | 4 | singular/unstable analysis exception |
| E02 | `benchmarks/edge-cases/E02-insufficient-horizontal-restraint.json` | 4 | singular/unstable analysis exception due to horizontal rigid-body mode |
| E03 | `benchmarks/edge-cases/E03-nearly-vertical-frame-member.json` | 4 | finite response and global equilibrium |
| E04 | `benchmarks/edge-cases/E04-nearly-horizontal-frame-member.json` | 4 | finite response and global equilibrium |
| E05 | `benchmarks/edge-cases/E05-release-mechanism-cantilever.json` | 4 | singular/unstable analysis exception caused by an end release mechanism |
| E06 | `benchmarks/edge-cases/E06-negative-load-combination.json` | 4 | negative factor accepted and applied with the correct sign |
| E07 | `benchmarks/edge-cases/E07-zero-load-combination-factor-invalid.json` | 4 | validator rejects the zero factor with `LOAD_COMBINATION_INVALID_FACTOR` |

---

## Edge-case validation policy

Edge cases are deliberately separated from the normal benchmark catalog because some of them are supposed to fail.

A singular model test passes only when the solver fails clearly. The expected diagnostic should mention that the reduced stiffness matrix is singular and that the model is probably unstable, insufficiently constrained or contains a mechanism. A model that silently returns huge or non-finite displacements is not acceptable.

Near-axis member tests do not assert exact displacements. Instead, they verify that the local/global transformation remains numerically stable by checking finite displacements, finite reactions, finite member end forces and global equilibrium.

Very small and very large load tests verify two properties:

1. all returned quantities remain finite;
2. reactions and displacements scale linearly with the load magnitude.

Load combination edge cases use this policy:

- negative factors are valid because they represent load reversal or subtraction;
- zero factors are currently invalid because a zero term is considered a modeling mistake and should be removed from the combination.

## Formula reference summary

This section summarizes the main formulas currently used in the benchmark catalog.

The detailed numerical values are documented in:

```text
benchmarks/expected/expected-results.md
```

### Simply supported beam — uniform distributed load

```text
RA = RB = qL / 2
Mmax = qL² / 8
v_mid = 5qL⁴ / (384EI)
```

Used by: `B01`.

### Simply supported beam — point load at midspan

```text
RA = RB = P / 2
Mmax = PL / 4
v_mid = PL³ / (48EI)
```

Used by: `B02`.

### Cantilever — tip point load

```text
RA = P
MA = PL
v_tip = PL³ / (3EI)
theta_tip = PL² / (2EI)
```

Used by: `B03`.

### Cantilever — uniform distributed load

```text
RA = qL
MA = qL² / 2
v_tip = qL⁴ / (8EI)
theta_tip = qL³ / (6EI)
```

Used by: `B04`.

### Simply supported beam — eccentric point load

For a point load `P` at distance `a` from the left support and `b` from the right support:

```text
L = a + b
RA = P b / L
RB = P a / L
M_under_load = P a b / L
v_under_load = P a² b² / (3 E I L)
```

Used by: `B05`.

### Load combination beam

For a manual combination:

```text
qd = gamma_G * G + gamma_Q * Q
RA = RB = qd L / 2
Mmax = qd L² / 8
v_mid = 5 qd L⁴ / (384EI)
```

Used by: `B06`.

### Continuous two-span beam — equal spans and equal UDL

For two equal spans of length `L` with uniform load `q`:

```text
RA = RC = 3qL / 8
RB = 5qL / 4
|M_B| = qL² / 8
```

Used by: `B07`.

### Axial member stiffness

For a member loaded axially:

```text
u = P L / (E A)
N = P
```

Used by: `B08` and `T02`.

### Propped cantilever — uniform distributed load

For a fixed end at `A`, roller prop at `B`, span `L`, UDL `q`:

```text
RB = 3qL / 8
RA = 5qL / 8
|MA| = qL² / 8
theta_B = qL³ / (48EI)
v_B = 0
```

Used by: `B09`.

### Fixed-fixed beam — uniform distributed load

```text
RA = RB = qL / 2
|MA| = |MB| = qL² / 12
```

Used by: `B10`.

The benchmark also verifies the solver's local member end-force sign convention.

### Simply supported beam — triangular distributed load

For triangular load increasing from `0` to `wB` over span `L`:

```text
W = wB L / 2
load resultant acts at 2L / 3 from the zero-intensity end
RA = W / 3
RB = 2W / 3
```

Used by: `B11`.

### Moment-released simply supported beam — uniform distributed load

For a beam with both end moments released:

```text
RA = RB = qL / 2
Mmax = qL² / 8
M_start = 0
M_end = 0
```

Used by: `B12`.

### Simple truss method of joints

Truss benchmark values are derived from joint equilibrium:

```text
sum Fx = 0
sum Fy = 0
positive axial force = tension
negative axial force = compression
```

Used by: `T01`, `T02`, `T03`.

---

## Sign conventions validated by benchmarks

The validation suite intentionally protects sign conventions because sign errors are among the easiest mistakes to miss in structural solvers.

Current conventions checked by tests:

| Quantity | Convention |
|---|---|
| Global `Fx` | positive along global +X |
| Global `Fy` | positive along global +Y |
| Global `Mz` | positive around global +Z |
| Vertical downward load | negative `GlobalY` when modeled as force direction/value pair, except where the input convention explicitly defines positive gravity loads |
| Truss axial force `N` | positive tension, negative compression |
| Support reactions | reported as global components |
| Inclined support reactions | local restraint is rotated, reported `Fx/Fy` remain global |
| Member end forces | local element sign convention, tested explicitly in fixed-end/released-end cases |

When a test fails, first check whether the failure is a real numerical error or a sign-convention mismatch in the expected result.

---

## Tolerances

Most benchmark cases currently use strict tolerances such as:

```text
1e-6
```

Recommended defaults:

| Check type | Suggested tolerance |
|---|---:|
| Simple reactions from closed-form formulas | `1e-6` |
| Closed-form displacements at modeled nodes | `1e-6` to `1e-5` |
| Member end forces | `1e-6` |
| Truss axial forces | `1e-6` |
| Diagram extrema from exact stations | `1e-6` to `1e-5` |
| Sampled/interpolated diagram extrema | `1e-4` or looser if sampling controls accuracy |
| Large regression validators | quantity-specific, documented in JSON |

Do not loosen tolerances to hide unexplained failures. If a tolerance must change, document why.

---

## How to add a new validation case

Use this checklist when adding a benchmark.

### 1. Choose the validation level

Decide whether the case is:

```text
Level 1: closed-form benchmark
Level 2: classical indeterminate benchmark
Level 3: regression validator
Level 4: external reference benchmark
```

### 2. Add the JSON model

Place the file in the correct category:

```text
benchmarks/beams/
benchmarks/trusses/
benchmarks/frames/
benchmarks/mixed/
```

Use a stable ID prefix:

```text
B = beam
T = truss
F = frame
M = mixed
C = convergence
```

### 3. Add expected values

Update:

```text
benchmarks/expected/expected-results.json
```

Every automatic benchmark entry should include:

```json
{
  "id": "Bxx",
  "name": "...",
  "modelPath": "benchmarks/...",
  "analysisId": "LC1",
  "tolerance": 1e-6,
  "expected": {},
  "reference": "...",
  "purpose": "..."
}
```

The `reference` field must explain where the expected result comes from.
The `purpose` field must explain what behavior the benchmark protects.

### 4. Document the formulas

Update:

```text
benchmarks/expected/expected-results.md
```

For closed-form and classical cases, include formulas. For regression validators, include the checked invariants and why they are meaningful.

### 5. Extend the runner if necessary

If the expected quantity cannot be expressed with the current runner, extend:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkResultAssertions.cs
```

Do this before considering the case part of the automatic validation suite.

### 6. Run the tests

```powershell
dotnet test
```

A benchmark is accepted only when:

- the model validates;
- the analysis runs;
- the expected numerical checks pass;
- the formulas/references are documented.

---

## Manual inspection workflow

Automated tests are the main validation mechanism, but visual/manual inspection is still useful.

Generate a report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report benchmarks\beams\B01-simple-supported-udl.json reports\B01-simple-supported-udl.md LC1
```

Generate an interactive viewer:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- viewer benchmarks\beams\B01-simple-supported-udl.json reports\viewer\B01-simple-supported-udl.html LC1
```

For load combinations:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- viewer benchmarks\beams\B06-simple-supported-load-combination.json reports\viewer\B06-ULS1.html ULS1
```

Manual inspection should check:

- support layout;
- load direction;
- reaction direction;
- deformed shape plausibility;
- diagram shape plausibility;
- labels and load-combination factors in the viewer.

Manual inspection does not replace numerical checks.

---

## Known validation boundaries

The current benchmark suite is strong for:

- 2D Frame2D beam behavior;
- 2D Truss2D axial behavior;
- mixed Frame2D + Truss2D assembly;
- common support configurations;
- manual load combinations;
- local/global load direction checks;
- selected indeterminate beam cases.

The suite still needs more coverage for:

- instability and singular-matrix diagnostics;
- near-mechanism cases;
- large sparse-model behavior;
- more published portal-frame references;
- Gerber/internal-hinge reference cases;
- temperature loads, if implemented later;
- self-weight, if implemented later;
- second-order effects, if implemented later.

Unsupported features should fail validation explicitly rather than fail unexpectedly during analysis.

---

## Release validation checklist

Before a release, run:

```powershell
dotnet build
dotnet test
```

Then verify that the benchmark catalog is still coherent:

- every benchmark JSON referenced by `expected-results.json` exists;
- every benchmark has `reference` and `purpose`;
- formulas are documented in `expected-results.md` or this manual;
- the README still points users to the benchmark and validation documentation;
- generated report/viewer examples are regenerated if their output contract changed.

Recommended release note wording:

```text
Validation: benchmark catalog passed against closed-form beam/truss cases, classical indeterminate beam cases and mixed Frame2D/Truss2D regression validators.
```

---

## Related documents

| Document | Purpose |
|---|---|
| `benchmarks/README.md` | Benchmark catalog overview |
| `benchmarks/expected/expected-results.md` | Human-readable formulas and expected values |
| `benchmarks/expected/expected-results.json` | Machine-readable expected values for tests |
| `docs/structural/benchmark-strategy.md` | Validation strategy and benchmark quality levels |
| `docs/structural/analysis.md` | Solver assumptions and analysis behavior |
| `docs/theory/matrix-method.md` | Matrix method background |
| `docs/theory/frame2d-element.md` | Frame2D theory |
| `docs/theory/truss2d-element.md` | Truss2D theory |
| `docs/theory/sign-conventions.md` | Sign conventions |
