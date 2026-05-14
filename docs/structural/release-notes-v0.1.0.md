# StructuralSolver2D v0.1.0 - First technical release

StructuralSolver2D v0.1.0 is the first technical release of the project.

It consolidates the current state after Milestones 1-37 and creates a clean baseline for future development, validation and possible OpenCad2D integration studies.

---

## What this release is

This release is a technical preview of an independent .NET 8 structural analysis engine.

It is intended for:

- educational study of matrix-based structural analysis;
- experimentation with 2D frame and truss models;
- benchmark-backed development;
- future integration experiments with CAD or graphical clients.

---

## What this release is not

This release is not a certified structural design product.

It does not provide:

- Eurocode, NTC or other code-compliant design checks;
- automatic load generation for wind, snow or seismic actions;
- buckling, fire, fatigue or connection design;
- nonlinear, second-order, dynamic, modal or seismic analysis;
- 3D analysis;
- plate, shell, solid or general mesh FEM analysis.

Results must be independently verified before any real engineering use.

---

## Main capabilities

### Structural model

- Nodes.
- Frame2D members.
- Truss2D members.
- Materials.
- Elastic material presets.
- Explicit and parametric sections.
- Supports.
- Load cases.
- Manual load combinations.

### Loads

- Nodal forces.
- Nodal moments.
- Uniform distributed member loads.
- Point loads on members.
- Linear, triangular and trapezoidal distributed member loads.
- Local and global load directions.

### Analysis

- First-order linear elastic static analysis.
- Frame2D analysis.
- Truss2D analysis.
- Mixed Frame2D + Truss2D plane-structure analysis.
- Frame2D member end moment releases.
- Support reactions.
- Nodal displacements.
- Local member end forces.
- Global equilibrium checks.

### Post-processing

- Internal-force diagrams for `N(x)`, `V(x)` and `M(x)`.
- Deformed-shape/displacement samples.
- Result summaries and governing extrema.
- Characteristic internal-force points.
- Preliminary sampled deflection checks.

### Reporting and export

- Markdown reports.
- Educational report explanations.
- Executive summaries.
- Model statistics.
- Optional preliminary deflection-check tables.
- CSV export for spreadsheet validation and external post-processing.

### Public API

Applications should prefer the high-level facade:

```csharp
using StructuralSolver2D.Analysis.PublicApi;

var service = new StructuralSolver2DService();
var output = service.AnalyzeLoadCase(model, "LC1");
```

For manual load combinations:

```csharp
var output = service.AnalyzeLoadCombination(model, "SLS1");
```

---

## Validation baseline

The project includes unit tests, integration-style analysis tests and benchmark catalog tests.

Run:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```

---

## CLI examples

Analyze a model:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
```

Generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

Export CSV files:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

---

## Recommended next milestone

After v0.1.0, the next planned step is:

```text
Milestone 39 - Future OpenCad2D integration study
```

The goal is to study the adapter boundary without coupling the solver to OpenCad2D.
