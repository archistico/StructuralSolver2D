# StructuralSolver2D

StructuralSolver2D is a free and open-source .NET 8 / C# engine for first-order linear elastic analysis of planar 2D structures made of one-dimensional members.

It is designed as a calculation engine and educational codebase. It can read structural models from JSON, solve load cases or manual load combinations, and export numerical reports and graphical result previews.

The project is independent from OpenCad2D. OpenCad2D may become a graphical client in the future, but the solver can already be used from the command line or from a .NET application.

---

## What it can do

StructuralSolver2D currently supports:

- 2D structural models with nodes, members, materials, sections, supports, load cases and loads;
- `Frame2D` members with axial, shear and bending behavior;
- `Truss2D` members with axial-only behavior;
- mixed `Frame2D` + `Truss2D` plane structures;
- nodal degrees of freedom `Ux`, `Uy`, `Rz`;
- hinged, fixed, roller/simple and custom support restraints;
- mechanically active inclined supports through `OrientationDegrees`;
- nodal forces and nodal moments;
- member point loads on `Frame2D` members;
- uniform, triangular and trapezoidal distributed loads on `Frame2D` members;
- manual load combinations, for example `1.35 G1 + 1.50 Q1`;
- nodal displacements;
- support reactions;
- local member end forces;
- sampled internal-force diagrams `N(x)`, `V(x)`, `M(x)`;
- sampled `Frame2D` displacement diagrams;
- result extrema and compact analysis summaries;
- preliminary serviceability deflection checks;
- Markdown, CSV, XLSX and PDF exports;
- static SVG/HTML graphical previews;
- interactive HTML viewer export;
- validation examples and benchmark cases for regression testing.

---

## What it does not do yet

StructuralSolver2D is not a certified structural design product.

It does not currently support:

- 3D frame or truss analysis;
- plates, shells, solids or general 2D/3D mesh FEM;
- nonlinear material behavior;
- geometric nonlinearity or second-order effects;
- modal, dynamic or seismic analysis;
- automatic wind, snow or seismic load generation;
- automatic self-weight generation in the analyzer;
- code-compliant design checks according to NTC, Eurocodes or other standards;
- steel, timber or reinforced concrete member design;
- connection design;
- buckling, fire, fatigue or robustness checks;
- automatic conversion from CAD drawing entities to structural models.

Results should always be checked independently before any professional or safety-critical use.

---

## Internal units

All input values are expected in coherent internal units.

| Quantity | Unit |
|---|---:|
| Length | m |
| Force | kN |
| Moment | kNm |
| Elastic modulus | kN/m² |
| Area | m² |
| Second moment of area | m⁴ |
| Distributed load | kN/m |

Important sign convention for vertical gravity loads: with the usual model orientation where global `Y` is upward, a downward load is usually entered as a **negative** `GlobalY` value.

Example:

```json
{
  "direction": "GlobalY",
  "value": -10.0
}
```

---

## Requirements

- .NET SDK 8.0 or later
- Windows, Linux or macOS supported by .NET 8
- PowerShell, Terminal, Bash or any equivalent command line shell

Check your installed SDK:

```powershell
dotnet --version
```

---

## Build and test

From the repository root:

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```

The shorter form also works from the repository root:

```powershell
dotnet build
dotnet test
```

---

## Command line usage

Show the CLI help:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- help
```

Run a built-in example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- example simple-supported-beam
```

Analyze a JSON model:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
```

Analyze a specific load case or load combination:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json ULS1
```

Generate a Markdown report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\beams\simple-supported-beam.json reports\simple-supported-beam.md
```

Export CSV tables:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

Export an XLSX workbook:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-xlsx examples\beams\simple-supported-beam.json reports\xlsx\simple-supported-beam.xlsx
```

Export a PDF report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-pdf examples\beams\simple-supported-beam.json reports\pdf\simple-supported-beam.pdf
```

Export a static SVG preview:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-svg examples\beams\simple-supported-beam.json reports\graphics\simple-supported-beam.svg
```

Export a static HTML preview:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-html examples\beams\simple-supported-beam.json reports\graphics\simple-supported-beam.html
```

Export an interactive HTML viewer:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-viewer examples\beams\simple-supported-beam.json reports\viewer\simple-supported-beam.html
```

---

## Getting started with a JSON model

A JSON input file describes the structural model and the loads to analyze. The simplest workflow is:

1. define the nodes;
2. define materials and sections;
3. define members between nodes;
4. define supports;
5. define one or more load cases;
6. define the loads;
7. run the CLI against the JSON file.

Minimal simply supported beam example:

```json
{
  "title": "Simply supported beam with uniform load",
  "description": "L = 5 m, q = 10 kN/m downward.",
  "loadCaseId": "LC1",
  "nodes": [
    { "id": "A", "x": 0.0, "y": 0.0 },
    { "id": "B", "x": 5.0, "y": 0.0 }
  ],
  "materials": [
    {
      "id": "MAT",
      "name": "Generic elastic material",
      "elasticModulus": 210000000.0
    }
  ],
  "sections": [
    {
      "id": "SEC",
      "name": "Generic section",
      "area": 0.003,
      "momentOfInertia": 0.00002
    }
  ],
  "members": [
    {
      "id": "M1",
      "startNodeId": "A",
      "endNodeId": "B",
      "materialId": "MAT",
      "sectionId": "SEC",
      "type": "Frame2D"
    }
  ],
  "supports": [
    {
      "id": "SA",
      "nodeId": "A",
      "restrainedUx": true,
      "restrainedUy": true,
      "restrainedRz": false,
      "type": "Hinge"
    },
    {
      "id": "SB",
      "nodeId": "B",
      "restrainedUx": false,
      "restrainedUy": true,
      "restrainedRz": false,
      "type": "SimpleSupport"
    }
  ],
  "loadCases": [
    { "id": "LC1", "name": "Default load case" }
  ],
  "loads": [
    {
      "id": "Q1",
      "loadCaseId": "LC1",
      "type": "UniformDistributedLoad",
      "targetType": "Member",
      "targetId": "M1",
      "direction": "GlobalY",
      "value": -10.0
    }
  ]
}
```

Save it as, for example:

```text
my-beam.json
```

Then run:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze my-beam.json
```

To generate a report:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report my-beam.json reports\my-beam.md
```

For the complete JSON guide, see:

```text
docs/structural/json-input.md
```

---

## Load combinations

Load combinations are manual. The solver does not generate Eurocode, NTC or other code combinations automatically.

Example idea:

```text
ULS1 = 1.35 G1 + 1.50 Q1
```

Run an existing example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json ULS1
```

The same identifier can be used for reports and exports:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\combinations\load-combination.json reports\load-combination.md ULS1
```

Graphical exports and the interactive viewer also respect manual combinations. When you export a combination, the load layer shows only the load cases included in that combination, with arrows and labels based on the factored values.

---

## Examples

User-facing examples are organized by category:

```text
examples/
  beams/
  trusses/
  mixed/
  combinations/
  loads/
  sections/
  validation/
```

Useful starting points:

| File | Purpose |
|---|---|
| `examples/beams/simple-supported-beam.json` | Basic beam with uniform load |
| `examples/beams/cantilever-point-load.json` | Cantilever with point load |
| `examples/beams/triangular-distributed-load.json` | Variable distributed load |
| `examples/beams/released-beam.json` | Beam with moment releases |
| `examples/trusses/simple-truss.json` | Basic axial truss |
| `examples/mixed/mixed-frame-truss.json` | Mixed frame and truss model |
| `examples/mixed/truss-bridge-20m-deck-loads.json` | 20 m truss bridge with deck loads |
| `examples/mixed/two-storey-house-balconies.json` | Two-storey frame with balconies |
| `examples/combinations/load-combination.json` | Manual load combinations |
| `examples/loads/inclined-nodal-force.json` | Inclined force represented by global components |
| `examples/validation/small-bridge-truss.json` | More realistic validation model |

See also:

```text
examples/README.md
```

---

## Reports and graphical output

The reporting project can export:

- Markdown reports for readable technical summaries;
- CSV tables for spreadsheet checks;
- XLSX workbooks;
- PDF reports;
- SVG previews;
- HTML previews;
- interactive HTML viewers.

The graphical exports are intended for review, documentation and future viewer integration. They do not replace independent engineering validation.

---

## Reactions and inclined supports

Support reactions are reported as global components:

```text
Fx  global horizontal reaction [kN]
Fy  global vertical reaction [kN]
Mz  global moment reaction [kNm]
```

For inclined supports, the restraint is applied in the rotated local support system, but the reported reaction is still expressed in global coordinates. Therefore, an inclined roller can correctly produce both `Fx` and `Fy`.

---

## Solver limitations

The current numerical backend uses a dense linear system solver. This is adequate for small and medium educational, prototyping and validation models, but it is not a production-scale sparse finite-element solver.

For large structural models, a sparse solver will be needed in the future.

---

## Repository structure

```text
StructuralSolver2D/
  src/
    StructuralSolver2D.Core/       model, units, validation, catalogs
    StructuralSolver2D.Analysis/   solvers, post-processing, public API
    StructuralSolver2D.Cli/        command-line interface
    StructuralSolver2D.Reporting/  reports and graphical exports

  tests/                           automated tests
  examples/                        user-facing JSON examples
  benchmarks/                      regression and validation cases
  docs/                            technical and theoretical documentation
  reports/                         generated report/output samples
```

---

## Documentation

Main documents:

| Document | Purpose |
|---|---|
| `docs/structural/json-input.md` | Full JSON input guide |
| `docs/structural/model.md` | Structural model concepts |
| `docs/structural/analysis.md` | Analysis behavior and assumptions |
| `docs/structural/public-api.md` | High-level .NET API usage |
| `docs/structural/validation-manual.md` | Numerical validation manual and benchmark workflow |
| `docs/structural/benchmark-strategy.md` | Benchmark quality levels and validation strategy |
| `docs/structural/reporting.md` | Report generation |
| `docs/structural/interactive-viewer.md` | Interactive viewer export |
| `docs/theory/matrix-method.md` | Matrix method background |
| `docs/theory/frame2d-element.md` | Frame2D element theory |
| `docs/theory/truss2d-element.md` | Truss2D element theory |

---

## Using StructuralSolver2D from .NET

The public entry point is `StructuralSolver2DService` in `StructuralSolver2D.Analysis.PublicApi`.

Typical application-level workflow:

1. create or load a `StructuralModel`;
2. validate the model;
3. call the service with an analysis request/options object;
4. consume the bundled result: analysis output, diagrams, summaries and optional checks.

See:

```text
docs/structural/public-api.md
```

---

## Development notes

Before adding a new user-facing solver feature:

- add validation rules first;
- add at least one analytical or benchmark test;
- document the expected input convention;
- update one JSON example when useful;
- keep `Core` independent from solvers, UI and reporting.

The project favors small, testable increments and explicit engineering assumptions.

---

## Safety note

StructuralSolver2D is experimental software for structural analysis research, education and prototyping.

Do not use it as the only basis for real structural design, construction decisions or safety-critical work. Results must be independently checked by a qualified professional.

---

## License

StructuralSolver2D is released under the GNU General Public License v3.0.

See:

```text
LICENSE
```

---

## Credits

Created with love by Emilie Rollandin.
