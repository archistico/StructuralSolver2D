# AI Handoff - StructuralSolver2D

This file is intended to help an LLM quickly understand the StructuralSolver2D project and contribute consistently.

Read this file before proposing code changes.

---

## One-sentence summary

StructuralSolver2D is an independent .NET 8 / C# planar structural analysis engine for simple 2D structures made of one-dimensional members, currently focused on first-order linear elastic 2D frame analysis.

---

## Current strategic decision

The project is intentionally independent from OpenCad2D.

OpenCad2D may become a future graphical front-end or integration target, but the solver must not depend on OpenCad2D, Avalonia, CAD entities, canvas rendering, UI commands or CAD persistence.

Do not introduce OpenCad2D references into the solver projects.

Correct dependency direction for future integration:

```text
OpenCad2D or another UI
    -> adapter/client layer
        -> StructuralSolver2D.Core
        -> StructuralSolver2D.Analysis
        -> StructuralSolver2D.Reporting
```

Never invert this dependency.

---

## Project identity

The project has two purposes:

1. lightweight 2D structural analysis engine;
2. educational codebase for studying matrix structural analysis / FEM-style 2D frame analysis.

The code should therefore remain clear, explicit and easy to test. Avoid clever abstractions that obscure the structural mechanics.

---


## Latest milestone

Milestone 24 added validation tests and benchmark files for inclined members and local/global load conventions.

The key rule is:

```text
Global loads stay in global coordinates.
Local loads rotate with the member local axes and therefore depend on member orientation.
```

The new tests cover:

- inclined Frame2D cantilever with global nodal load;
- inclined Frame2D cantilever with `LocalY` uniform load;
- inclined Frame2D cantilever with `GlobalY` uniform load;
- member orientation `A -> B` versus `B -> A`;
- axial tip load along an inclined member;
- mixed Frame2D + Truss2D model with an inclined brace.

## Current repository layout

```text
src/
  StructuralSolver2D.Core/
  StructuralSolver2D.Analysis/
  StructuralSolver2D.Cli/
  StructuralSolver2D.Reporting/

tests/
  StructuralSolver2D.Core.Tests/
  StructuralSolver2D.Analysis.Tests/

examples/
  simple-supported-beam.json
  cantilever-point-load.json
  cantilever-uniform-load.json
  axial-bar.json

docs/structural/
  vision.md
  scope.md
  architecture.md
  model.md
  units.md
  analysis.md
  validation.md
  roadmap.md
```

The solution file is:

```text
StructuralSolver2D.sln
```

The project targets:

```text
net8.0
```

---

## Project responsibilities

### `StructuralSolver2D.Core`

Contains only structural data model and validation.

Main objects:

- `StructuralModel`
- `StructuralNode`
- `StructuralMember`
- `StructuralMaterial`
- `StructuralSection`
- `StructuralSupport`
- `StructuralLoadCase`
- `StructuralLoad`
- `StructuralModelValidator`
- validation result/issue/severity classes
- enums for member, support, load type, target type and direction

Rules:

- no solver logic;
- no matrix logic;
- no CLI logic;
- no report generation;
- no UI/CAD concepts;
- no OpenCad2D references.

### `StructuralSolver2D.Analysis`

Contains the Frame2D solver and post-processing.

Main objects:

- `Frame2DAnalyzer`
- `Frame2DElementMatrices`
- `DenseLinearSystemSolver`
- `StructuralAnalysisException`
- `StructuralAnalysisResult`
- `NodalDisplacementResult`
- `SupportReactionResult`
- `MemberEndForceResult`
- `Frame2DInternalForceSampler`
- `MemberInternalForceSample`
- `MemberInternalForceDiagram`
- `Frame2DResultSummarizer`
- `StructuralAnalysisSummary`
- extrema result classes

Rules:

- depends on `Core`;
- validates the model before analysis;
- supports only `MemberType.Frame2D` at the current stage;
- should throw controlled `StructuralAnalysisException` for unsupported or invalid analysis cases;
- should not know anything about CLI, JSON, reports or UI.

### `StructuralSolver2D.Cli`

Contains command-line usage and JSON input for examples.

Current commands:

```text
help
example <name>
analyze <input.json> [loadCaseId]
report <input.json> <output.md> [loadCaseId]
```

Rules:

- CLI can reference Core, Analysis and Reporting;
- CLI should remain thin;
- model-building examples can live here for now;
- JSON schema intentionally mirrors the current Core model to remain educational.

### `StructuralSolver2D.Reporting`

Contains reporting logic.

Current implementation:

- `MarkdownStructuralReportGenerator`
- `MarkdownReportOptions`

Rules:

- reporting can reference Core and Analysis results;
- reporting should not run the solver itself;
- reporting should receive a model, analysis result, diagrams and summary.

---

## Internal units

The solver uses fixed coherent internal units.

| Quantity | Unit |
|---|---:|
| Length | m |
| Force | kN |
| Moment | kNm |
| Elastic modulus | kN/m² |
| Area | m² |
| Second moment of area | m⁴ |
| Uniform distributed load | kN/m |

Important examples:

```text
Steel E = 210000 N/mm² = 210000000 kN/m²
Timber E = 11000 N/mm² = 11000000 kN/m²
```

Do not introduce mixed internal units.

UI/CLI/report layers may format values differently in the future, but the solver should receive internal units.

---

## Structural model assumptions

Current analysis type:

```text
First-order linear elastic static analysis of plane frame structures.
```

Current element:

```text
Frame2D
```

Degrees of freedom per node:

```text
Ux - horizontal displacement
Uy - vertical displacement
Rz - in-plane rotation
```

Degrees of freedom per member:

```text
Ux1, Uy1, Rz1, Ux2, Uy2, Rz2
```

Supported structural schemes at this stage include:

- simply supported beam;
- continuous beam, where modelled with frame members and supports;
- cantilever;
- simple portal/frame;
- axially loaded bar represented as a frame member;
- simple educational frame examples.

---

## Current load support

The Core model can represent:

- `NodalForce`
- `NodalMoment`
- `UniformDistributedLoad`
- `PointLoadOnMember, LinearDistributedLoad`
- `SelfWeight` enum support may exist depending on current code state

The current Frame2D analyzer supports:

- nodal force;
- nodal moment;
- uniform distributed load on members.

The current Frame2D analyzer does not yet support:

- self-weight generation.

Point loads on members are supported through equivalent nodal loads. Keep benchmark tests for centered and eccentric point loads when changing this area. A useful reference case is a simply supported beam with a centered point load:

```text
RA = P / 2
RB = P / 2
Mmax = P L / 4
```

---

## Current result pipeline

The intended result pipeline is:

```text
StructuralModel
    -> StructuralModelValidator
    -> Frame2DAnalyzer
    -> StructuralAnalysisResult
    -> Frame2DInternalForceSampler
    -> MemberInternalForceDiagram
    -> Frame2DResultSummarizer
    -> StructuralAnalysisSummary
    -> CLI output / Markdown report
```

Do not bypass validation in analysis code.

---

## Current CLI commands

Build/test:

```powershell
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```

Help:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- help
```

Built-in example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- example simple-supported-beam
```

Analyze JSON:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json
```

Analyze JSON with explicit load case:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json LC1
```

Generate Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\simple-supported-beam.json reports\simple-supported-beam.md
```

---

## Validation philosophy

Validation is not optional in this project.

Before extending user-facing functionality, add tests with known analytical results.

Existing benchmark categories include:

- simply supported beam with uniform distributed load;
- simply supported beam with centered point load;
- cantilever with point load at free end;
- cantilever with uniform distributed load;
- axially loaded bar;
- no-load model;
- load-case filtering;
- symmetric portal/frame behavior;
- unstable/labile model;
- internal force sampling;
- extrema summarization.

When adding a feature, include:

1. a model-level validation test if the feature affects model validity;
2. an analysis test if the feature affects solver behavior;
3. a benchmark test with hand-computable expected results whenever possible.

---

## Current limitations

Do not claim that the project currently supports:

- real professional structural design;
- NTC or Eurocode design checks;
- steel or timber member verification;
- buckling checks;
- seismic analysis;
- dynamic/modal analysis;
- geometric nonlinearity;
- material nonlinearity;
- connection design;
- fire or fatigue design;
- 3D analysis;
- CAD integration;
- automatic conversion from CAD geometry.

The current project is an experimental and educational structural analysis engine.

---

## Coding style preferences

Follow the style already used in the project:

- C# / .NET 8;
- nullable reference types enabled;
- clear namespaces matching folders;
- small focused classes;
- explicit names over abbreviated names;
- XML documentation for public API where appropriate;
- deterministic tests;
- no hidden global state;
- no UI dependencies in Core or Analysis;
- prefer clear exceptions over silent failures.

When adding a new milestone, prefer incremental changes:

```text
small feature
+ tests
+ documentation/readme update if user-facing
```

---

## Important sign conventions

The current documentation and tests use the convention that, for a horizontal left-to-right beam:

```text
positive bending moment = sagging moment
```

A simply supported beam with a downward uniform load should produce positive bending moment in span.

A downward-loaded cantilever should produce negative bending moment at the fixed end under the current convention.

Before changing sign conventions, update tests, reports and documentation together. Do not change signs casually.

---

## Known next-step candidates

Recommended next milestones:

### Milestone 12 - Cleanup and polish

- remove unused default `Class1.cs` and `UnitTest1.cs` files if still present;
- ensure XML summaries are coherent;
- add missing tests for reporting;
- improve README examples if needed.

### Milestone 13 - Reporting tests

- add tests for `MarkdownStructuralReportGenerator`;
- verify that sections are generated;
- verify that units and key result values appear;
- verify report generation with and without internal-force samples.

### Milestone 14 - Self-weight

- use material unit weight and section area;
- generate equivalent uniform distributed loads;
- document direction and sign conventions.

### Milestone 15 - More examples

Add educational JSON examples:

- fixed-fixed beam;
- propped cantilever;
- two-span continuous beam;
- simple rectangular portal;
- triangular truss-like frame example, if still using Frame2D;
- validation examples with expected results in comments or companion docs.

### Future - OpenCad2D integration

Only after the solver is stable:

- keep StructuralSolver2D independent;
- add a separate adapter layer;
- structural entities should remain explicit;
- CAD geometry may be used as background/reference/snap aid, not automatically converted into structural entities.

---

## Safety note for future assistants

Do not present this project as a certified structural design tool.

Use language such as:

```text
analysis engine
educational solver
preliminary structural analysis
simple plane structural schemes
```

Avoid language such as:

```text
code-compliant design software
certified structural calculator
ready for professional construction design
NTC/Eurocode complete verifier
```

---

## Last known stable state from the conversation

At the time this handoff was written:

- build passed after Milestone 10 fix;
- tests passed through Milestone 12;
- point loads on members are implemented in the Frame2D analyzer and internal-force sampler;
- Markdown report generation command worked after fixing `MarkdownStructuralReportGenerator` collection parameter types;
- project is on .NET 8;
- `.sln` classic solution is used instead of `.slnx`;
- latest direction: continue with self-weight generation, richer examples and further numerical validation.


## Milestone 14 update

The Frame2D solver now supports `LinearDistributedLoad` member loads. `value` is the start intensity and `endValue` is the end intensity, both in kN/m. This covers triangular and trapezoidal distributed loads in global or local X/Y directions.


## Current load-combination support

Manual load combinations are supported through `StructuralLoadCombination` and `StructuralLoadCombinationTerm` in `StructuralSolver2D.Core`. The feature is deliberately manual only: do not add automatic normative generation unless explicitly requested and carefully scoped.

`Frame2DAnalyzer.AnalyzeCombination(model, combinationId)` solves a factored combination by summing loads from each referenced load case with its factor. The returned `StructuralAnalysisResult.LoadCaseId` contains the combination id, so downstream CLI/reporting code treats the selected id as a generic analysis id.

The CLI accepts either a load case id or a combination id in the same position:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\load-combination.json ULS1
dotnet run --project src\StructuralSolver2D.Cli -- report examples\load-combination.json reports\load-combination.md ULS1
```

The sampler resolves `analysisResult.LoadCaseId` as a combination id when it matches `model.LoadCombinations`; otherwise it treats it as a normal load case id.


## Milestone 16 - Truss2D axial-only analysis

This milestone adds a dedicated `Truss2DAnalyzer` for pure plane truss models.

Supported behavior:

- members with `type: Truss2D`;
- axial-only stiffness with active translational DOFs `Ux` and `Uy`;
- nodal force loads in `GlobalX` and `GlobalY`;
- manual load combinations;
- support reactions in `Fx` and `Fy`;
- nodal displacements with `Rz = 0`;
- axial member end forces, with positive internal `N` interpreted as tension.

Current limits:

- mixed `Frame2D` + `Truss2D` models are not supported yet;
- member distributed loads, member point loads and nodal moments are not supported by the Truss2D analyzer;
- truss members do not provide bending or shear behavior.

CLI example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-truss.json
dotnet run --project src\StructuralSolver2D.Cli -- report examples\simple-truss.json reports\simple-truss.md
```

---

## Milestone 17 - benchmark catalog

A benchmark catalog has been introduced under:

```text
benchmarks/
  README.md
  beams/
  frames/
  trusses/
  expected/
```

Current benchmark files:

```text
benchmarks/beams/B01-simple-supported-udl.json
benchmarks/beams/B02-simple-supported-point-midspan.json
benchmarks/beams/B03-cantilever-tip-point-load.json
benchmarks/beams/B04-cantilever-udl.json
benchmarks/trusses/T01-symmetric-triangular-truss.json
benchmarks/frames/F01-portal-symmetric-gravity.json
benchmarks/expected/expected-results.json
benchmarks/expected/expected-results.md
```

Important guidance for future LLM work:

- do not replace the benchmark catalog with ad-hoc tests only;
- keep every benchmark small and explainable;
- prefer closed-form solutions when available;
- when exact values are not documented, use equilibrium and symmetry checks first;
- do not add benchmark expected values copied from unknown or untrusted sources;
- if external benchmark values are added, cite the source in the Markdown documentation;
- keep internal units unchanged: m, kN, kNm, kN/m², m², m⁴.

Recommended next milestone:

```text
Milestone 18 - automated benchmark runner
```

Suggested implementation:

```text
StructuralSolver2D.Analysis.Tests/Benchmarks/
  BenchmarkExpectedResultsReader.cs
  BenchmarkTestRunner.cs
  BenchmarkCatalogTests.cs
```

The runner should read:

```text
benchmarks/expected/expected-results.json
```

and compare solver output against expected values with declared tolerances.

## Milestone 18 - Automated benchmark runner

The benchmark catalog is now executable through an xUnit test:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalogTests.cs
```

The test reads `benchmarks/expected/expected-results.json`, loads the referenced JSON models through `StructuralModelJsonReader`, selects `Frame2DAnalyzer` or `Truss2DAnalyzer` based on member types, and verifies the expected reactions, extrema, selected displacements, axial forces, and named equilibrium/symmetry checks.

Important convention: the benchmark runner is intentionally in `StructuralSolver2D.Analysis.Tests` and references `StructuralSolver2D.Cli` only to reuse the CLI JSON reader. This keeps the benchmark files aligned with the command-line input format.

## Milestone 19 - Frame2D displacement sampling

A `Frame2DDisplacementSampler` has been added under:

```text
src/StructuralSolver2D.Analysis/Frame2D/Frame2DDisplacementSampler.cs
```

Result types:

```text
src/StructuralSolver2D.Analysis/Results/MemberDisplacementSample.cs
src/StructuralSolver2D.Analysis/Results/MemberDisplacementDiagram.cs
```

The sampler uses finite-element interpolation only:

- axial displacement is linearly interpolated;
- transverse displacement uses cubic Hermite beam interpolation;
- local rotation is the derivative of the interpolated transverse displacement;
- global Ux/Uy are obtained by transforming local u/v back to global axes.

Do not claim that sampled internal deflections are closed-form exact for distributed loads. When validating exact midspan deflections for UDL cases, add a node at midspan and compare the nodal displacement instead. This was an important design decision after the B01 benchmark exposed the difference between FEM interpolation and closed-form beam deflection.


## Current milestone note - deformed shape samples in reports

The Reporting project can include `MemberDisplacementDiagram` data in Markdown reports. The CLI generates displacement diagrams for Frame2D models and passes them to the report generator. Truss2D reports currently omit member displacement diagrams. Displacement samples are FEM interpolation of nodal results; benchmark deflection checks should still model critical points as explicit nodes when comparing against closed-form formulas.

## Current milestone note - analysis diagnostics

The analyzers now provide more actionable exception messages. Keep this style for future work:

- include entity ids in user-facing analysis failures whenever possible;
- distinguish invalid model validation errors from unsupported analyzer features;
- for unsupported mixed models, explicitly say that mixed `Frame2D` + `Truss2D` analysis is not supported yet;
- for solver singularities, keep the message educational and mention instability, missing restraints or mechanisms;
- do not hide `StructuralModelValidationIssue` details when validation fails.

Regression coverage for these messages is in:

```text
tests/StructuralSolver2D.Analysis.Tests/Diagnostics/StructuralAnalysisDiagnosticsTests.cs
```

## Current milestone note - Frame2D member moment releases

`StructuralMember` now has:

```csharp
bool ReleaseStartMoment
bool ReleaseEndMoment
```

These apply only to `Frame2D` members and represent local end bending-moment releases. The `Frame2DAnalyzer` applies them through element-level static condensation in `Frame2DElementMatrices.ApplyMomentReleases(...)`.

Important implementation detail: after releases, some nodal rotational DOFs may become inactive, especially for pin-ended isolated members. The analyzer automatically suppresses zero-stiffness DOFs with zero load before solving. Do not remove this behavior unless a more explicit DOF-management layer is introduced.

JSON supports:

```json
"releaseStartMoment": true,
"releaseEndMoment": true
```

Known limitation: this is not yet a full joint-release model for all possible semi-rigid or partial-release cases. It currently covers ideal local moment releases for Frame2D elements.

## Milestone 23 note - Mixed plane-structure analyzer

A new analyzer has been added:

```text
StructuralSolver2D.Analysis.PlaneStructure2D.PlaneStructureAnalyzer
```

Purpose:

- analyze models that contain both `Frame2D` and `Truss2D` members;
- use one global system with three DOFs per node: `Ux`, `Uy`, `Rz`;
- assemble `Frame2D` members into all six member DOFs;
- assemble `Truss2D` members into translational DOFs only.

Important limits:

- member loads are allowed only on `Frame2D` members;
- `Truss2D` members remain axial-only and should receive loads through nodal forces;
- do not add design checks or second-order effects in this milestone;
- keep `Frame2DAnalyzer` and `Truss2DAnalyzer` intact as pure-model analyzers.

CLI selection logic:

- pure truss models still use `Truss2DAnalyzer`;
- pure frame models still use `Frame2DAnalyzer`;
- mixed models use `PlaneStructureAnalyzer`.
