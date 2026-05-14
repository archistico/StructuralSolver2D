# StructuralSolver2D

StructuralSolver2D is an independent .NET 8 / C# engine for planar structural analysis of simple 2D schemes made of one-dimensional members.

The project is intentionally separated from OpenCad2D while the calculation engine is being consolidated. OpenCad2D may become a future graphical client, but it is not a dependency of the solver.

The project has two complementary goals:

1. provide a lightweight 2D structural analysis engine;
2. offer an educational codebase for understanding matrix-based structural analysis and the finite-element formulation of 2D frame members.

---

## Current scope

The current implementation focuses on first-order linear elastic analysis of plane structures using 2D frame members.

Supported at the current stage:

- independent structural model;
- nodes, members, materials, elastic material presets, explicit sections, parametric sections and supports;
- load cases and basic loads;
- 2D frame analysis with three degrees of freedom per node: `Ux`, `Uy`, `Rz`;
- nodal forces;
- nodal moments;
- uniform distributed loads on members;
- support reactions;
- nodal displacements;
- local member end forces;
- sampled internal-force diagrams `N(x)`, `V(x)`, `M(x)`;
- characteristic internal-force points for reporting and future graphical output;
- preliminary serviceability deflection checks;
- parametric section helpers for rectangular, timber rectangular, solid circular and hollow circular sections;
- initial elastic material library for common steel, timber, glulam and concrete presets;
- result extrema and analysis summaries;
- CLI examples;
- JSON input examples;
- Markdown report generation, including educational guidance, executive summaries, characteristic internal-force point tables and optional preliminary deflection-check tables;
- CSV export for spreadsheet validation and external post-processing.

Not supported yet:

- automatic CAD integration;
- OpenCad2D integration;
- automatic conversion from drawing entities to structural entities;
- 3D analysis;
- plates, shells, solids or 2D/3D mesh FEM;
- nonlinear material behavior;
- geometric nonlinearity / second-order effects;
- modal, dynamic or seismic analysis;
- automatic wind, snow or seismic load generation;
- self-weight load generation in the analyzer;
- structural design checks according to NTC, Eurocodes or other codes;
- steel/timber connection design;
- fire, fatigue or buckling checks.

---

## Internal units

StructuralSolver2D uses fixed coherent internal units.

| Quantity | Internal unit |
|---|---:|
| Length | m |
| Force | kN |
| Moment | kNm |
| Elastic modulus | kN/m² |
| Area | m² |
| Second moment of area | m⁴ |
| Uniform distributed load | kN/m |

The CLI and future UI layers may display other units, but all analysis data should be converted to these internal units before solving.

---

## Repository structure

```text
StructuralSolver2D/
  StructuralSolver2D.sln
  Directory.Build.props
  README.md
  CHANGELOG.md
  VERSION
  ai-handoff.md

  docs/
    structural/
      vision.md
      scope.md
      architecture.md
      model.md
      units.md
      analysis.md
      validation.md
      reporting.md
      csv-export.md
      public-api.md
      release-checklist.md
      release-notes-v0.1.0.md
      roadmap.md

  examples/
    README.md
    beams/
      simple-supported-beam.json
      cantilever-point-load.json
      cantilever-uniform-load.json
      member-point-load.json
      triangular-distributed-load.json
      released-beam.json
      axial-bar.json
    trusses/
      simple-truss.json
    mixed/
      mixed-frame-truss.json
    combinations/
      load-combination.json

  benchmarks/
    beams/
    frames/
    trusses/
    mixed/
    convergence/
    expected/

  reports/
    simple-supported-beam.md

  src/
    StructuralSolver2D.Core/
    StructuralSolver2D.Analysis/
    StructuralSolver2D.Cli/
    StructuralSolver2D.Reporting/

  tests/
    StructuralSolver2D.Core.Tests/
    StructuralSolver2D.Analysis.Tests/
```

---

## Projects

### `StructuralSolver2D.Core`

Contains the structural model and validation logic.

Main concepts:

- `StructuralModel`
- `StructuralNode`
- `StructuralMember`
- `StructuralMaterial`
- `StructuralSection`
- `StructuralSectionFactory`
- `StructuralSupport`
- `StructuralLoadCase`
- `StructuralLoad`
- `StructuralLoadCombination`
- `StructuralLoadCombinationTerm`
- `StructuralModelValidator`

This project must remain independent from solvers, CLI, reporting, CAD, UI and rendering.

### `StructuralSolver2D.Analysis`

Contains the current 2D frame solver and post-processing utilities.

Main concepts:

- `Frame2DAnalyzer`
- `Frame2DElementMatrices`
- `DenseLinearSystemSolver`
- `Frame2DInternalForceSampler`
- `Frame2DResultSummarizer`
- `StructuralAnalysisResult`
- `StructuralAnalysisSummary`

This project depends on `StructuralSolver2D.Core`.

### `StructuralSolver2D.Cli`

Provides a command-line interface for quick analysis, examples and report generation.

Current commands:

- `help`
- `example <name>`
- `analyze <input.json> [loadCaseId|combinationId]`
- `report <input.json> <output.md> [loadCaseId|combinationId]`
- `export-csv <input.json> <output-directory> [loadCaseId|combinationId]`

### `StructuralSolver2D.Reporting`

Contains report generators.

Current implementation:

- Markdown report generator;
- CSV result exporter.

---

## Build and test

From the repository root:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```

Or simply:

```powershell
dotnet build
dotnet test
```

The project targets `.NET 8`.

---

## CLI usage

Show help:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- help
```

Run a built-in example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- example simple-supported-beam
```

Analyze a JSON file:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
```

Analyze a JSON file and specify the load case explicitly:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json LC1
```

Generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

Generate a Markdown report for a specific load case:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md LC1
```

Export CSV files for spreadsheet validation:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

Export CSV files for a specific load case or combination:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\combinations\load-combination.json reports\csv\combination ULS1
```

---

## Available examples

Current built-in and JSON examples:

| Example | Description |
|---|---|
| `simple-supported-beam` | Simply supported beam with uniform distributed load. |
| `cantilever-point-load` | Cantilever with a point load at the free end. |
| `cantilever-uniform-load` | Cantilever with uniform distributed load. |
| `axial-bar` | Axially loaded bar. |

---


## Parametric sections

Milestone 33 adds `StructuralSectionFactory` in `StructuralSolver2D.Core.Model.Sections`.

It generates `StructuralSection` instances from simple dimensions using internal units:

```csharp
var section = StructuralSectionFactory.Rectangular(
    "RECT_100x200",
    width: 0.10,
    height: 0.20);
```

Available helpers:

- `Rectangular(id, width, height)`;
- `TimberRectangular(id, width, height)`;
- `CircularSolid(id, diameter)`;
- `CircularHollow(id, outerDiameter, innerDiameter)`.

These helpers compute area `A` in m² and in-plane bending inertia `I` in m⁴. They do not replace manual `StructuralSection` input; they only reduce mistakes for common shapes.

---


## Elastic material presets

Milestone 34 adds `StructuralMaterialLibrary` in `StructuralSolver2D.Core.Model.Materials`.

It generates ordinary `StructuralMaterial` records for common elastic analysis presets using internal units:

```csharp
var material = StructuralMaterialLibrary.SteelS235();
```

Available helpers:

- `SteelS235()`;
- `SteelS275()`;
- `SteelS355()`;
- `TimberC24()`;
- `GlulamGL24h()`;
- `GenericConcrete()`;
- `ConcreteC25_30()`.

The presets currently provide only Young's modulus `E` in kN/m² and optional unit weight in kN/m³. They are not normative design definitions and do not include strengths, partial factors, national annex rules, duration factors, fire checks, buckling checks or connection checks.

---

## Validation approach

Validation is a core part of the project.

The current test suite includes checks for:

- model construction;
- structural validation errors;
- duplicate IDs;
- missing node/material/section/load-case references;
- invalid materials and sections;
- invalid load definitions;
- simply supported beam with uniform distributed load;
- simply supported beam with point load at midspan;
- cantilever with point load at the free end;
- cantilever with uniform distributed load;
- axially loaded bar;
- zero-load model;
- load-case filtering;
- symmetric portal behavior;
- unstable/labile model detection;
- sampled internal-force diagrams;
- result extrema and summaries.

When adding new solver features, add analytical benchmark tests before relying on the feature from the CLI or reports.

---

## Current milestones

Completed so far:

```text
Milestone 1  - Independent .NET 8 solution
Milestone 2  - Pure structural data model
Milestone 3  - Loads and load cases
Milestone 4  - Minimal Frame2D solver
Milestone 5  - Extended Frame2D validation
Milestone 6  - Internal force sampling N/V/M
Milestone 7  - Result summary and extrema
Milestone 8  - CLI with built-in examples
Milestone 9  - JSON examples and analyze command
Milestone 10 - Markdown report generation
Milestone 11 - README and AI handoff documentation
Milestone 12 - Test coverage for Reporting and CLI input
Milestone 13 - Point loads on members in the Frame2D analyzer
Milestone 14 - Linear, triangular and trapezoidal distributed loads
Milestone 15 - Manual load combinations
Milestone 16 - Truss2D / axial-only members / simple trusses
Milestone 17 - Benchmark catalog
Milestone 18 - Automated benchmark runner
Milestone 19 - Frame2D displacement and deformed-shape sampling
Milestone 20 - Deformed-shape samples in Markdown reports
Milestone 21 - Analysis diagnostics
Milestone 22 - Frame2D member end moment releases
Milestone 23 - Mixed Frame2D + Truss2D plane-structure analyzer
Milestone 24 - Local/global load conventions and inclined member validation
Milestone 25 - Global equilibrium checker
Milestone 26 - Mesh refinement and convergence benchmarks
Milestone 28 - Improved benchmark runner
Milestone 29 - Examples and benchmarks reorganization
Milestone 30 - Initial theory documentation
Milestone 31 - Improved internal-force diagrams and characteristic points
Milestone 32 - Preliminary SLE deflection checks
Milestone 33 - Parametric sections
Milestone 34 - Initial material library
Milestone 35 - Advanced educational Markdown reports
Milestone 36 - CSV export
Milestone 37 - Public API stabilization
Milestone 38 - First technical release
```

Current technical release:

```text
v0.1.0 - First technical release
```

Recommended next milestone:

```text
Milestone 39 - Future OpenCad2D integration study
```

Medium-term roadmap:

```text
Milestone 39 - Future OpenCad2D integration study
Milestone 40 - Experimental viewer
```

See the full roadmap in:

```text
docs/structural/roadmap.md
```

---

## First technical release

The first technical release is documented in:

```text
CHANGELOG.md
VERSION
docs/structural/release-notes-v0.1.0.md
docs/structural/release-checklist.md
```

Suggested GitHub release title:

```text
StructuralSolver2D v0.1.0 - First technical release
```

This release is a technical preview and not a certified structural design product.

---

## Design principles

- Keep the solver independent from OpenCad2D.
- Keep `Core` independent from analysis, reporting and UI.
- Keep all units explicit.
- Prefer small testable steps.
- Add validation before relying on model data.
- Add analytical benchmarks before adding user-facing features.
- Avoid automatic structural assumptions from CAD geometry.
- Treat the current project as an analysis engine, not as a code-compliant design tool.

---

## Safety and professional-use note

StructuralSolver2D is currently an experimental and educational structural analysis engine.

It must not be treated as a certified structural design tool. Results should be checked independently and must not be used for real structural design, construction decisions or safety-critical work without review by a qualified professional.


## Milestone 14 update

The Frame2D solver now supports `LinearDistributedLoad` member loads. `value` is the start intensity and `endValue` is the end intensity, both in kN/m. This covers triangular and trapezoidal distributed loads in global or local X/Y directions.


## Milestone 15 update

The solver now supports manual load combinations through `StructuralLoadCombination` and `StructuralLoadCombinationTerm`. Combinations are user-defined only: no automatic NTC/Eurocode generation is performed.

Example JSON command:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\load-combination.json ULS1
dotnet run --project src\StructuralSolver2D.Cli -- report examples\load-combination.json reports\load-combination.md ULS1
```

A combination such as `ULS1 = 1.35 G1 + 1.50 Q1` is analyzed by summing factored contributions from the referenced load cases. Internal force sampling and Markdown reporting understand the same combination id.

---

## Benchmark catalog

The project now includes a first benchmark catalog under:

```text
benchmarks/
  README.md
  beams/
  frames/
  trusses/
  expected/
```

The catalog is used to document hand-checkable validation cases and to prepare the future automated benchmark runner.

Initial benchmark groups:

| Group | Purpose |
|---|---|
| `benchmarks/beams` | closed-form beam and cantilever checks |
| `benchmarks/trusses` | simple axial truss checks |
| `benchmarks/frames` | symmetry, equilibrium and stability checks for frame structures |
| `benchmarks/expected` | expected values for future automated validation |

Run a benchmark manually:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze benchmarks\beams\B01-simple-supported-udl.json
```

Generate a benchmark report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report benchmarks\beams\B01-simple-supported-udl.json reports\B01-simple-supported-udl.md
```

The expected values are currently documented in:

```text
benchmarks/expected/expected-results.md
benchmarks/expected/expected-results.json
```

The next planned validation milestone is an automated benchmark runner that reads `expected-results.json`, executes the benchmark models, and compares reactions, displacements and extrema against the expected values.

---

## Milestone 19 update - Frame2D displacement sampling

The analysis project now includes a dedicated finite-element displacement sampler for `Frame2D` members:

```text
Frame2DDisplacementSampler
MemberDisplacementSample
MemberDisplacementDiagram
```

It samples:

```text
u(x)  local axial displacement
v(x)  local transverse displacement
rz(x) local rotation
Ux(x) global horizontal displacement
Uy(x) global vertical displacement
```

Important modeling note: the sampler uses the standard finite-element interpolation of nodal displacements. This is suitable for drawing deformed shapes and for post-processing the FEM displacement field. For distributed loads, an internal sampled displacement is not always identical to the closed-form beam deflection unless that position is explicitly modeled as a structural node.

For benchmark checks, critical positions such as midspan should therefore usually be modeled as nodes when their closed-form deflection is used as an expected value.


## Milestone update: deformed shape sampling in reports

Markdown reports can include sampled Frame2D deformed-shape values (`u`, `v`, `rz`, `Ux`, `Uy`). These samples are finite-element interpolation of nodal displacements and should not be confused with closed-form exact internal deflections under all load types.

## Milestone update: analysis diagnostics

Analysis failure messages now include more actionable context for common modeling mistakes:

- analyzer/member type mismatches list the unsupported member id and type;
- `Truss2DAnalyzer` unsupported load errors list the offending load id and type;
- invalid model exceptions include a short summary of the first validation issues;
- singular reduced stiffness matrix errors report the failing pivot and mention possible mechanisms or missing restraints.

This keeps the project more suitable for educational use: when an analysis fails, the message should help the user understand whether the problem is an invalid model, an unsupported feature, or an unstable/labile structural scheme.

## Milestone update: Frame2D member end moment releases

Frame2D members can now declare local bending moment releases at the start and/or end of the element:

```json
{
  "id": "M1",
  "startNodeId": "A",
  "endNodeId": "B",
  "materialId": "MAT",
  "sectionId": "SEC",
  "type": "Frame2D",
  "releaseStartMoment": true,
  "releaseEndMoment": true
}
```

This is useful for modeling pin-ended beams, internal hinges and frame members whose end rotations are not fully moment-continuous.

The implementation uses element-level static condensation. Inactive rotational DOFs with no stiffness and no applied load are automatically suppressed by the analyzer, so a pin-ended single member can be analyzed without artificially adding rotational restraints to the supports.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\released-beam.json
dotnet run --project src\StructuralSolver2D.Cli -- report examples\released-beam.json reports\released-beam.md
```

## Milestone 23 - Mixed Frame2D + Truss2D analysis

The project now includes a first mixed plane-structure analyzer:

```text
StructuralSolver2D.Analysis.PlaneStructure2D.PlaneStructureAnalyzer
```

It supports models containing both:

- `Frame2D` members, with axial, shear and bending behavior;
- `Truss2D` members, with axial behavior only.

The mixed analyzer uses a common three-degree-of-freedom nodal layout:

```text
Ux, Uy, Rz
```

`Truss2D` members contribute stiffness only to translational degrees of freedom `Ux` and `Uy`. Rotational degrees of freedom remain available for connected `Frame2D` members.

Current limits:

- member distributed loads and member point loads are supported only on `Frame2D` members;
- `Truss2D` members support nodal-force loading through the global model only;
- this is still a first-order linear elastic analysis;
- no second-order effects, buckling checks or design checks are included.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed-frame-truss.json
dotnet run --project src\StructuralSolver2D.Cli -- report examples\mixed-frame-truss.json reports\mixed-frame-truss.md
```

## Mesh refinement benchmarks

The project now includes initial mesh-refinement benchmarks under:

```text
benchmarks/convergence/
```

These benchmarks document how FEM results change when the same structural problem is discretized with different numbers of elements.

They are especially useful for understanding the difference between:

- nodal FEM results;
- internally interpolated displacement samples;
- closed-form beam theory values.

The related automated tests are in:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/MeshRefinementConvergenceTests.cs
```

See also:

```text
docs/structural/mesh-refinement.md
```


## Milestone 28 update

The automated benchmark runner has been refactored into dedicated test-side components:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalog.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkRepository.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkAnalysisRunner.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkResultAssertions.cs
```

The benchmark catalog test now performs two distinct steps:

1. validate the benchmark catalog structure;
2. run all benchmark models and compare computed results with expected values.

This keeps `BenchmarkCatalogTests.cs` small and makes future expected-result extensions easier to add without duplicating test logic.

Current benchmark checks include:

- support reactions;
- nodal displacements and rotations;
- member axial forces;
- maximum absolute shear and bending moment;
- named stability/symmetry checks;
- global equilibrium residuals.

---

## Examples and benchmarks

StructuralSolver2D now separates user-facing examples from validation benchmarks.

```text
examples/     user-facing files for learning and CLI usage
benchmarks/   validation and regression cases with expected results
```

Preferred example paths are categorized:

```text
examples/
  beams/
  trusses/
  mixed/
  combinations/
```

Run an organized example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
```

Run a benchmark case manually:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze benchmarks\beams\B01-simple-supported-udl.json
```

Benchmarks are automatically checked by the test suite through `benchmarks/expected/expected-results.json`.

See also:

- `examples/README.md`
- `docs/structural/examples-and-benchmarks.md`
- `docs/structural/benchmark-strategy.md`
- `docs/structural/public-api.md`

---

## Theory documentation

Milestone 30 adds the first theory documentation layer under:

```text
docs/theory/
```

The goal is to make the project useful not only as a solver, but also as an educational codebase for studying matrix-based structural analysis.

Current theory notes:

- `docs/theory/matrix-method.md`
- `docs/theory/frame2d-element.md`
- `docs/theory/truss2d-element.md`
- `docs/theory/equivalent-nodal-loads.md`
- `docs/theory/local-global-coordinates.md`
- `docs/theory/sign-conventions.md`
- `docs/theory/displacement-interpolation.md`
- `docs/theory/validation-strategy.md`

These documents explain the assumptions, coordinate conventions, element behavior, equivalent nodal loads, displacement interpolation and validation strategy used by the current solver.


## Milestone 31 - Characteristic internal-force points

Milestone 31 is complete. The post-processing pipeline now detects characteristic points on sampled internal-force diagrams:

- member start and end points;
- sampled minimum, maximum and maximum absolute values of `N`, `V` and `M`;
- zero crossings of `N`, `V` and `M`;
- bending-moment extremum candidates detected from zero shear;
- candidate shear discontinuities between adjacent samples.

These points are derived from sampled diagrams and are intended for reporting, validation, future graphical output and educational inspection. The Markdown report includes them by default. Exact analytical locations may require dedicated closed-form post-processing for specific load configurations.

Milestone 32 adds preliminary serviceability deflection checks through `PreliminaryDeflectionChecker`.

The checker evaluates sampled member displacement diagrams against simple limits such as `L/200`, `L/250`, `L/300` or `L/400`. It reports the critical sample, the selected displacement direction, the allowable deflection, the maximum sampled deflection and the pass/fail status.

This feature is intentionally conservative in wording and scope: it is a preliminary engineering aid, not a complete code-compliant serviceability verification. Internal sampled displacement values are still finite-element interpolation values; for exact benchmark comparisons at critical positions, model those positions as explicit nodes.

Milestone 33 adds parametric section helpers through `StructuralSectionFactory`. The helpers generate ordinary `StructuralSection` records for rectangular, timber rectangular, solid circular and hollow circular sections.

Milestone 34 adds initial elastic material presets through `StructuralMaterialLibrary`. The presets generate ordinary `StructuralMaterial` records for common steel, timber, glulam and concrete analysis inputs. They are convenience values for linear elastic analysis, not complete normative design definitions.

Milestone 35 expands Markdown reports with educational guidance, an executive summary, model-size statistics, governing absolute values and optional preliminary serviceability deflection-check tables. These additions are report-layer features only: they do not modify solver calculations or imply normative design verification.

Milestone 36 adds CSV export through `CsvStructuralResultExporter` and the CLI `export-csv` command. The exported tables cover nodal displacements, support reactions, member end forces, internal-force samples, displacement samples and compact result summaries. CSV output is intended for spreadsheet validation and external post-processing, not for complete model exchange.

Milestone 37 adds a stable high-level public facade through `StructuralSolver2DService` in `StructuralSolver2D.Analysis.PublicApi`. Applications can now run a complete workflow from one entry point and receive a bundled result with analysis results, sampled internal-force diagrams, optional displacement diagrams, optional preliminary deflection checks and a compact summary.

Milestone 38 prepares the first technical release baseline `v0.1.0` with `VERSION`, `CHANGELOG.md`, release notes and a release checklist. It does not change solver behavior.

Next recommended work: Milestone 39, focused on studying a future OpenCad2D integration boundary without coupling the solver to OpenCad2D.
