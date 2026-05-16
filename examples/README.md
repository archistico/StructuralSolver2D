# StructuralSolver2D examples

This folder contains user-facing JSON examples for learning and trying StructuralSolver2D from the CLI.

Examples are separated from benchmarks:

- `examples/` contains readable usage examples;
- `benchmarks/` contains validation and regression cases with expected results.

The CLI can analyze any JSON file path.

---

## Folder layout

```text
examples/
  beams/          beam and Frame2D examples
  trusses/        axial-only Truss2D examples
  mixed/          mixed Frame2D + Truss2D models
  combinations/   manual load combination examples
  loads/          specific load-convention examples
  sections/       section catalog examples
  validation/     larger representative validation models
```

New examples should be added to the categorized folders, not to the root of `examples/`.

---

## Beam and Frame2D examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\cantilever-point-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\cantilever-uniform-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\member-point-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\triangular-distributed-load.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\released-beam.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\axial-bar.json
```

---

## Truss2D examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\trusses\simple-truss.json
```

---

## Mixed Frame2D + Truss2D examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\mixed-frame-truss.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\three-storey-mixed-loads.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\truss-bridge-20m-deck-loads.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\mixed\two-storey-house-balconies.json
```

---

## Manual load combination examples

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json ULS1
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json SLS1
```

---

## Load convention examples

Inclined single forces are represented by global components, for example one `GlobalX` force and one `GlobalY` force.

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\loads\inclined-nodal-force.json
```

For the full JSON input explanation, see:

```text
docs/structural/json-input.md
```

---

## Validation examples

Validation examples are representative structural schemes stored as real JSON files and covered by automated smoke tests.

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\validation\rigid-joint-portal-frame.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\validation\small-bridge-truss.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\validation\nielsen-parabolic-truss.json
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\validation\gerber-beam-asymmetric-loads.json
```

See `examples/validation/README.md` for the complete list.

---

## Reports

Any example can be used to generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

The same input can be exported to CSV, XLSX, PDF, SVG, HTML or interactive viewer formats through the corresponding CLI commands.
