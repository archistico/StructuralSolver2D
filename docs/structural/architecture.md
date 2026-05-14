# StructuralSolver2D - Architecture

StructuralSolver2D is designed as an independent .NET solution.

The engine must not depend on OpenCad2D, Avalonia, WPF, a canvas, a property panel or any graphical UI.

## Initial solution structure

Recommended structure:

```text
StructuralSolver2D/
  StructuralSolver2D.sln

  src/
    StructuralSolver2D.Core/
    StructuralSolver2D.Analysis/
    StructuralSolver2D.Reporting/
    StructuralSolver2D.Cli/

  tests/
    StructuralSolver2D.Core.Tests/
    StructuralSolver2D.Analysis.Tests/
```

## Project responsibilities

### StructuralSolver2D.Core

Contains the pure structural domain model.

Examples:

- `StructuralModel`;
- `StructuralNode`;
- `StructuralMember`;
- `StructuralSupport`;
- `StructuralLoad`;
- `StructuralMaterial`;
- `StructuralSection`;
- `StructuralLoadCase`;
- `StructuralLoadCombination`.

This project must remain independent from UI and analysis implementation details.

### StructuralSolver2D.Analysis

Contains the calculation engine.

Examples:

- `Frame2DAnalyzer`;
- `Frame2DLocalStiffnessMatrix`;
- `CoordinateTransformation`;
- `GlobalStiffnessMatrixAssembler`;
- `LoadVectorBuilder`;
- `BoundaryConditionApplier`;
- `LinearSystemSolver`;
- `StructuralAnalysisResult`;
- `MemberForceResult`;
- `SupportReactionResult`.

This project depends on `StructuralSolver2D.Core`.

### StructuralSolver2D.Reporting

Produces human-readable reports.

Initial reporting can be Markdown or plain text.

Later reporting may include HTML or PDF generation.

### StructuralSolver2D.Cli

Provides a fast way to test the engine without any graphical interface.

Example future command:

```bash
structural2d analyze examples/beam-simple.json
```

The CLI should be considered a development and validation tool before any graphical integration.

## Dependency direction

Correct dependency direction:

```text
StructuralSolver2D.Cli
    ↓
StructuralSolver2D.Reporting
    ↓
StructuralSolver2D.Analysis
    ↓
StructuralSolver2D.Core
```

The core must not depend on analysis, reporting, CLI or UI.

## Future OpenCad2D integration

OpenCad2D may later consume the solver through an adapter layer.

Possible future structure:

```text
OpenCad2D
  OpenCad2D.Structural.Adapter
      ↓
  StructuralSolver2D.Core
  StructuralSolver2D.Analysis
```

Rules:

- StructuralSolver2D must not reference OpenCad2D;
- OpenCad2D may reference StructuralSolver2D;
- CAD entities must not be required by the solver;
- UI concepts must remain outside the solver;
- the solver must remain usable from tests, CLI, API or other clients.

## Why the project starts independent

The first priority is numerical reliability.

Keeping the project independent provides:

- faster tests;
- simpler debugging;
- fewer dependencies;
- clearer API design;
- no interference with OpenCad2D UI or persistence;
- easier validation with benchmark cases;
- better future reuse.

## Architectural principle

The calculation engine must be deterministic, testable and UI-agnostic.

The UI should be a client of the engine, not part of the engine.
