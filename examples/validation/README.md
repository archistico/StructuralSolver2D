# Validation JSON examples

This folder contains user-facing structural schemes used as validation smoke tests.

They are intentionally different from formal benchmarks:

- they are readable JSON models for CLI usage and documentation;
- they cover representative structural topologies;
- automated tests verify that every file is loadable, valid and analyzable;
- they do not contain normative design checks or certified reference values.

Run one example from the CLI:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\validation\rigid-joint-portal-frame.json
```

Generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\validation\nielsen-parabolic-truss.json reports\nielsen-parabolic-truss.md
```

Export CSV result tables:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\validation\gerber-beam-asymmetric-loads.json reports\csv\gerber-beam
```

## Included examples

| File | Scheme |
|---|---|
| `rigid-joint-portal-frame.json` | Rigid-joint portal frame with fixed bases |
| `small-bridge-truss.json` | Small bridge-like truss |
| `isostatic-triangular-truss-beam.json` | Isostatic triangular truss beam |
| `nielsen-parabolic-truss.json` | Nielsen-type parabolic truss |
| `inverted-parabolic-truss.json` | Inverted parabolic truss |
| `double-diagonal-hyperstatic-truss.json` | Hyperstatic truss with double diagonals |
| `gerber-beam-asymmetric-loads.json` | Simplified Gerber beam with asymmetric loads |

## Validation intent

The automated test suite checks that each JSON file:

- can be parsed by `StructuralModelJsonReader`;
- passes `StructuralModelValidator`;
- can be analyzed through `StructuralSolver2DService`;
- produces finite displacements, reactions and member end forces;
- produces internal-force diagrams for every member.

These files are good candidates for future SVG/HTML/PDF/XLSX export tests because they cover visually different structural schemes.
