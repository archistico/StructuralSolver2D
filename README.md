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
- nodes, members, materials, sections and supports;
- load cases and basic loads;
- 2D frame analysis with three degrees of freedom per node: `Ux`, `Uy`, `Rz`;
- nodal forces;
- nodal moments;
- uniform distributed loads on members;
- support reactions;
- nodal displacements;
- local member end forces;
- sampled internal-force diagrams `N(x)`, `V(x)`, `M(x)`;
- result extrema and analysis summaries;
- CLI examples;
- JSON input examples;
- Markdown report generation.

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
  README.md
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
      roadmap.md

  examples/
    simple-supported-beam.json
    cantilever-point-load.json
    cantilever-uniform-load.json
    axial-bar.json
    member-point-load.json

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

### `StructuralSolver2D.Reporting`

Contains report generators.

Current implementation:

- Markdown report generator.

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
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json
```

Analyze a JSON file and specify the load case explicitly:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json LC1
```

Generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\simple-supported-beam.json reports\simple-supported-beam.md
```

Generate a Markdown report for a specific load case:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\simple-supported-beam.json reports\simple-supported-beam.md LC1
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
Milestone 5  - Extended solver validation
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
```

Recommended next steps:

```text
Milestone 16 - Add self-weight generation
Milestone 17 - Add richer example library and validation cases
Milestone 18 - Start considering a minimal graphical viewer or future OpenCad2D adapter
```

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
