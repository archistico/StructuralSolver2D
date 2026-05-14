# Validation JSON examples

Milestone 41 adds a first catalog of JSON validation examples under:

```text
examples/validation/
```

The goal is to make representative structural schemes available as real files that can be used by the CLI, by automated tests and by future graphical/reporting exporters.

## Why JSON examples matter

Earlier validation examples existed mainly inside test code. That is useful for regression testing, but less useful for users and for future tools.

JSON files make the examples:

- inspectable without reading C# test builders;
- runnable from the CLI;
- reusable for reports and exports;
- suitable as visual test inputs for SVG, HTML, PNG and viewer milestones;
- stable enough to be referenced by documentation.

## Included schemes

The initial set covers:

- rigid-joint portal frame;
- small bridge-like truss;
- isostatic triangular truss beam;
- Nielsen-type parabolic truss;
- inverted parabolic truss;
- hyperstatic double-diagonal truss;
- simplified Gerber beam with asymmetric loads.

## Scope

These examples are qualitative validation and demonstration inputs.

They are not:

- certified benchmark values;
- normative design examples;
- NTC/Eurocode verification cases;
- a substitute for hand checks;
- a professional calculation report.

The tests currently verify that each file is valid, analyzable and numerically finite. Future milestones may add expected reaction/displacement/force envelopes where appropriate.

## CLI usage

Analyze one validation file:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\validation\rigid-joint-portal-frame.json
```

Generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\validation\nielsen-parabolic-truss.json reports\nielsen-parabolic-truss.md
```

Export CSV tables:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\validation\double-diagonal-hyperstatic-truss.json reports\csv\double-diagonal
```

## Future use

These files should become the first reusable inputs for:

- static graphical export SVG/HTML/PNG;
- XLSX report export;
- PDF technical report export;
- viewer smoke tests;
- OpenCad2D integration experiments.
