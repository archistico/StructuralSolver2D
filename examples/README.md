# StructuralSolver2D examples

This folder contains user-facing examples for learning and trying StructuralSolver2D from the CLI.

Examples are intentionally separated from validation benchmarks:

- `examples/` contains readable usage examples;
- `benchmarks/` contains validation and regression cases with expected results.

The CLI can analyze any JSON file path, so organized examples can be run directly.

## Beam and Frame2D examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\cantilever-point-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\cantilever-uniform-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\member-point-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\triangular-distributed-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\released-beam.json
```

## Truss2D examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\trusses\simple-truss.json
```

## Mixed Frame2D + Truss2D examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\mixed-frame-truss.json
```

## Manual load combination examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json ULS1
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json SLS1
```

## Reports

Any example can be used to generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

## Legacy flat examples

Older flat example files in `examples/*.json` may remain in existing working copies for compatibility. New examples should be added to one of the categorized subfolders:

```text
examples/
  beams/
  trusses/
  mixed/
  combinations/
```
