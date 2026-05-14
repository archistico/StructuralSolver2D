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

Milestone 39 adds a UI-independent viewer-ready result data model.

The reporting project now contains `StructuralSolver2D.Reporting.Visualization`, which prepares:

- undeformed nodes and member axes;
- scaled deformed nodal coordinates;
- nodal `Ux`, `Uy` and `Rz` values for labels/tooltips;
- deformed member polylines;
- normal-force, shear-force and bending-moment diagram polylines;
- drawing bounds;
- optional cyclic animation frames for deformed-shape playback.

This is not a GUI and must remain independent from Avalonia, WPF, HTML, SVG and OpenCad2D.

The post-M39 roadmap has been realigned. The next recommended milestone is:

```text
Milestone 40 — Parametric model generators
```

The viewer path is still planned, but it now follows the generator/report/export work instead of replacing it. See `docs/structural/development-plan.md`.

Important convention:

```text
examples/      user-facing files for learning and CLI usage
benchmarks/    validation and regression cases with expected results
docs/theory/   educational notes explaining solver assumptions and mechanics
```

Preferred example layout:

```text
examples/
  beams/
  trusses/
  mixed/
  combinations/
```

The CLI can analyze any JSON file path, so no solver changes are required for this organization.

Existing flat `examples/*.json` files may remain temporarily for compatibility, but new examples should use the categorized layout.

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
  README.md
  beams/
  trusses/
  mixed/
  combinations/

benchmarks/
  beams/
  frames/
  trusses/
  mixed/
  convergence/
  expected/

docs/theory/
  matrix-method.md
  frame2d-element.md
  truss2d-element.md
  equivalent-nodal-loads.md
  local-global-coordinates.md
  sign-conventions.md
  displacement-interpolation.md
  validation-strategy.md

docs/structural/
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
- `StructuralMaterialLibrary`
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
export-csv <input.json> <output-directory> [loadCaseId]
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
- `CsvStructuralResultExporter`

Milestone 35 report options:

- `IncludeEducationalExplanations`
- `IncludeModelStatistics`
- `IncludeDeflectionChecks`

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
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

Export CSV files:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
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

The next recommended milestone is:

```text
Milestone 40 — Parametric model generators
```

The unified post-M39 roadmap is:

```text
40 - Parametric model generators
41 - Validation example files in JSON
42 - Static graphical result export: SVG, HTML and PNG
43 - XLSX report export
44 - PDF technical report
45 - Section catalog persistence
46 - First simple interactive viewer prototype
47 - Viewer controls for result scale, diagram scale and animation playback
48 - Labels/tooltips for nodal displacements, rotations and diagram values
49 - OpenCad2D integration boundary study
```

Do not skip the generator, JSON validation, XLSX/PDF and section-catalog milestones just because the viewer data model now exists. The viewer path should grow after the solver has better example generation, validation files and professional exports.

See:

```text
docs/structural/development-plan.md
```
