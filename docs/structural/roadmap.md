# StructuralSolver2D Roadmap

StructuralSolver2D is developed as an independent .NET 8 structural analysis engine for two-dimensional linear structural models.

The project is intentionally developed in small, testable and documented milestones.

The current goal is not to build a full professional structural design package, but to create a clean, validated and educational 2D structural analysis core.

---

## Development principle

Every important feature must be supported by:

- a clear model representation;
- deterministic tests;
- validation benchmarks;
- documentation;
- CLI usage where appropriate;
- reporting support where appropriate.

The guiding rule is:

> no significant new feature should be added without dedicated validation benchmarks.

This is especially important because the project deals with structural analysis results, where silent numerical or sign-convention errors can be misleading.

---

## Current status

The project currently includes:

- independent .NET 8 solution;
- structural model core;
- nodes, members, materials, sections and supports;
- load cases;
- manual load combinations;
- nodal forces and moments;
- uniform distributed loads;
- linear distributed loads;
- point loads on members;
- Frame2D analysis;
- Truss2D analysis;
- mixed Frame2D + Truss2D analysis;
- moment releases at Frame2D member ends;
- internal-force sampling;
- deformed-shape sampling;
- result summaries and extrema;
- characteristic internal-force points;
- CLI commands;
- JSON input examples;
- Markdown reports with educational guidance;
- CSV export for spreadsheet validation and external post-processing;
- benchmark catalog;
- automated benchmark runner;
- diagnostic analysis errors;
- README and AI handoff documentation;
- preliminary SLE deflection checks;
- parametric section helpers;
- initial elastic material library;
- public API facade;
- first technical release baseline;
- UI-independent viewer-ready result data;
- optional cyclic deformed-shape animation frames;
- support glyphs, scaled support reactions and static result annotations in SVG/HTML exports.

---

## Completed milestones

### Milestone 1 — Independent .NET 8 solution

Created the initial independent .NET 8 solution.

The solver is not coupled to OpenCad2D, Avalonia, WPF or any graphical user interface.

---

### Milestone 2 — Structural core model

Added the first pure structural data model:

- `StructuralModel`;
- `StructuralNode`;
- `StructuralMember`;
- `StructuralMaterial`;
- `StructuralSection`;
- `StructuralSupport`.

The model is independent from the solver and from any UI.

---

### Milestone 3 — Loads and load cases

Added:

- `StructuralLoadCase`;
- `StructuralLoad`;
- nodal forces;
- nodal moments;
- member loads;
- load validation.

---

### Milestone 4 — Minimal Frame2D solver

Added the first linear elastic Frame2D solver.

Supported:

- 2D frame elements;
- global stiffness assembly;
- boundary conditions;
- nodal displacements;
- support reactions;
- local member end forces.

---

### Milestone 5 — Extended Frame2D validation

Added additional analytical tests for:

- simply supported beams;
- cantilevers;
- axial behavior;
- unloaded models;
- symmetric portal frames;
- unstable models.

---

### Milestone 6 — Internal-force sampling

Added sampling of:

- axial force `N(x)`;
- shear force `V(x)`;
- bending moment `M(x)`.

This enables future diagrams, reporting and graphical visualization.

---

### Milestone 7 — Result summaries and extrema

Added result summaries and maximum/minimum extraction for internal-force diagrams.

This makes the results easier to consume from CLI, reports and future UI layers.

---

### Milestone 8 — Minimal CLI examples

Added a minimal command line interface with predefined examples.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- example simple-supported-beam
```

---

### Milestone 9 — JSON input examples

Added JSON model loading and the `analyze` command.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\simple-supported-beam.json
```

---

### Milestone 10 — Markdown reports

Added a Markdown report generator.

Example:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- report examples\simple-supported-beam.json reports\simple-supported-beam.md
```

---

### Milestone 11 — README and AI handoff

Added project-level documentation and `ai-handoff.md`.

The AI handoff file helps future LLM sessions understand:

- project architecture;
- current status;
- conventions;
- solver limits;
- next recommended steps.

---

### Milestone 12 — Reporting and CLI test coverage

Added tests for:

- Markdown report generation;
- JSON input reading;
- CLI-related input behavior;
- report options.

---

### Milestone 13 — Point loads on members

Implemented point loads applied along Frame2D members.

Supported:

- normalized position along the member;
- global directions;
- local directions;
- equivalent nodal loads;
- internal-force sampling with shear jumps.

---

### Milestone 14 — Linear distributed loads

Implemented linearly varying distributed loads.

This supports:

- triangular loads;
- trapezoidal loads;
- generic linear member loads.

---

### Milestone 15 — Manual load combinations

Added manual load combinations.

Example:

```json
{
  "id": "ULS1",
  "name": "ULS 1",
  "terms": [
    { "loadCaseId": "G1", "factor": 1.35 },
    { "loadCaseId": "Q1", "factor": 1.50 }
  ]
}
```

The solver does not generate normative combinations automatically.

Only user-defined combinations are supported.

---

### Milestone 16 — Truss2D solver

Added a Truss2D analyzer for axial-only members.

Supported:

- 2D truss members;
- nodal forces;
- support reactions;
- nodal displacements;
- axial member force.

---

### Milestone 17 — Benchmark catalog

Added a structured benchmark catalog.

Initial benchmark groups:

- beams;
- trusses;
- frames;
- expected results.

The benchmark catalog is intended to become the main validation base of the project.

---

### Milestone 18 — Automated benchmark runner

Added an automated benchmark runner.

The runner reads benchmark models and expected results, runs the solver and compares computed values against the expected values.

This turns benchmark files into regression tests.

---

### Milestone 19 — Frame2D displacement sampler

Added displacement and deformed-shape sampling along Frame2D members.

Sampled values include:

- local axial displacement;
- local transverse displacement;
- local rotation;
- global X displacement;
- global Y displacement.

Important note:

> the displacement sampler currently performs FEM interpolation of nodal displacements. It is useful for drawing the deformed shape, but it is not always equal to the exact closed-form internal displacement field for distributed loads.

---

### Milestone 20 — Deformed-shape samples in Markdown reports

Added deformed-shape samples to the Markdown report.

This prepares the project for:

- better educational reports;
- graphical output;
- future UI integration.

---

### Milestone 21 — Analysis diagnostics

Improved diagnostic errors for unsupported or invalid analysis cases.

Examples:

- wrong analyzer for member type;
- unsupported loads in Truss2D;
- unstable model;
- singular stiffness matrix;
- invalid model issues.

---

### Milestone 22 — Frame2D moment releases

Added moment releases at Frame2D member ends.

Supported:

- start moment release;
- end moment release;
- both-end moment release.

This allows modelling hinged member ends and internal hinges.

---

### Milestone 23 — Mixed Frame2D + Truss2D analyzer

Added `PlaneStructureAnalyzer`.

It supports mixed 2D structural models containing both:

- `Frame2D` members;
- `Truss2D` members.

The global model uses three degrees of freedom per node:

- `Ux`;
- `Uy`;
- `Rz`.

Truss members contribute only to translational degrees of freedom.

---



### Milestone 24 — Local/global load conventions and inclined member validation

Added tests and benchmark files for inclined members and local/global load conventions.

Covered topics:

- inclined Frame2D cantilever with global nodal load;
- inclined Frame2D cantilever with `LocalY` uniform load;
- inclined Frame2D cantilever with `GlobalY` uniform load;
- reversed member orientation `A -> B` and `B -> A`;
- axial load along an inclined member;
- mixed Frame2D + Truss2D model with inclined brace.

This milestone reinforces the convention that global loads stay global, while local loads rotate with the member axes.

### Milestone 25 — Global equilibrium checker

Added an automatic global equilibrium checker.

The checker verifies, for a completed analysis result:

```text
ΣFx ≈ 0
ΣFy ≈ 0
ΣMz ≈ 0
```

Implemented class:

```text
GlobalEquilibriumChecker
```

The checker computes:

- total applied horizontal force;
- total applied vertical force;
- total applied moment about a reference point;
- total support horizontal reaction;
- total support vertical reaction;
- total support moment reaction;
- force and moment residuals.

Supported load sources:

- nodal forces;
- nodal moments;
- point loads on members;
- uniform distributed loads;
- linearly varying distributed loads;
- manual load combinations.

The benchmark runner now executes this global equilibrium check for every catalog benchmark.

This milestone is important because it can detect:

- missing loads;
- wrong load signs;
- wrong reaction signs;
- local/global transformation errors;
- load combination errors;
- member load conversion errors.

---

# Upcoming milestones

## Milestone 26 — Mesh refinement and convergence benchmarks

### Goal

Introduce convergence benchmarks.

The same problem should be modeled with increasing discretization density:

```text
1 element
2 elements
4 elements
8 elements
```

### Candidate cases

- simply supported beam with uniform distributed load;
- cantilever with uniform distributed load;
- beam with triangular load;
- beam with point load not coincident with a node.

### Checks

The benchmark should evaluate convergence of:

- deflection;
- maximum bending moment;
- shear force;
- deformed shape;
- internal sampled values.

### Educational purpose

This milestone should document a key FEM concept:

> a finite element model is a discretized approximation of a continuous structure.

---

## Milestone 27 — Professional and institutional benchmark catalog

### Goal

Create a benchmark section inspired by recognized verification sources.

Possible sources:

- NAFEMS;
- SOFiSTiK verification examples;
- Autodesk Robot / Nastran verification manuals;
- Dlubal verification examples;
- OpenSees examples;
- university lecture examples.

### Proposed structure

```text
benchmarks/
  professional/
    README.md
    nafems-inspired/
    opensees-inspired/
    software-verification/
```

### Rules

For every professional/institutional benchmark, document:

- source;
- description;
- adaptation to StructuralSolver2D;
- assumptions;
- limitations;
- expected results;
- tolerances;
- what the benchmark validates.

Do not copy long copyrighted text from external manuals.

Use only minimal data, independent explanations and proper references.

---

## Milestone 28 — Improved benchmark runner

### Goal

Make the benchmark runner more expressive and maintainable.

### Planned improvements

Support expected checks for:

- reactions;
- nodal displacements;
- nodal rotations;
- support moments;
- internal-force values at specific positions;
- global maximum/minimum values;
- axial forces in truss members;
- equilibrium residuals;
- symmetry conditions;
- mesh convergence criteria.

### Why this matters

The benchmark runner should become the backbone of the validation strategy.

As the solver grows, benchmarks should be easier to add without duplicating test code.

---

## Milestone 29 — Examples and benchmarks reorganization

### Goal

Separate user examples from validation benchmarks.

### Proposed structure

```text
examples/
  beams/
  frames/
  trusses/
  mixed/
  combinations/

benchmarks/
  beams/
  frames/
  trusses/
  mixed/
  convergence/
  professional/
  expected/
```

### Difference

Examples are for users.

Benchmarks are for validation.

Examples should be easy to understand.

Benchmarks should be precise, documented and regression-tested.

---

## Milestone 30 — Initial theory documentation

### Goal

Add educational theory documentation.

### Proposed files

```text
docs/theory/
  matrix-method.md
  frame2d-element.md
  truss2d-element.md
  equivalent-nodal-loads.md
  local-global-coordinates.md
  sign-conventions.md
  displacement-interpolation.md
  validation-strategy.md
```

### Why this matters

StructuralSolver2D can be both:

- a lightweight 2D structural analysis engine;
- an educational codebase for learning matrix-based structural analysis.

The theory documentation should explain how and why the solver works.

---

## Milestone 31 — Improved internal-force diagrams and characteristic points

### Goal

Improve the post-processing of internal-force diagrams.

### Completed features

The post-processing pipeline detects characteristic points such as:

- maximum bending moment;
- minimum bending moment;
- maximum shear;
- zero shear points;
- zero moment points;
- concentrated load positions;
- shear discontinuities;
- diagram segment limits.

### Why this matters

This is necessary for:

- better reports;
- future graphical output;
- preliminary checks;
- future integration with OpenCad2D.

### Status

Completed. The implementation is based on sampled diagrams. It is suitable for reporting and educational inspection; exact analytical locations may still require dedicated closed-form post-processing for specific load configurations.

---

## Milestone 32 — Preliminary SLE deflection checks

### Goal

Add preliminary serviceability checks for deflection.

### Implemented scope

Milestone 32 adds a deliberately limited serviceability helper in the analysis project:

```text
StructuralSolver2D.Analysis.Serviceability.PreliminaryDeflectionChecker
```

It checks sampled member displacement diagrams against a simple limit:

```text
maximum sampled deflection <= L / limit
```

Common limits remain:

```text
L/200
L/250
L/300
L/400
```

The implementation reports:

- member id;
- checked direction;
- reference length;
- allowable deflection;
- maximum sampled absolute deflection;
- signed critical deflection;
- critical sample position;
- pass/fail status;
- utilization ratio.

### Important limitation

This must not be presented as a complete normative check.

The correct wording is:

```text
preliminary serviceability check
```

or:

```text
preliminary deflection check
```

not:

```text
full code-compliant design verification
```

The check is based on the existing FEM displacement/deformed-shape samples. For exact benchmark comparison at a specific critical point, that point should still be modeled as an explicit node.

### Status

Completed.

---

## Milestone 33 — Parametric sections

### Goal

Generate section properties from simple geometric inputs while preserving the existing explicit `StructuralSection` API.

### Implemented scope

Milestone 33 adds:

```text
StructuralSolver2D.Core.Model.Sections.StructuralSectionFactory
```

Implemented helpers:

- rectangular section;
- simple timber rectangular section;
- circular solid section;
- circular hollow section.

Example:

```csharp
var section = StructuralSectionFactory.Rectangular(
    "RECT_100x200",
    width: 0.10,
    height: 0.20);
```

The generated section computes:

- area in m²;
- second moment of area in m⁴;
- optional height and width metadata where meaningful.

### Status

Completed.

---

## Milestone 34 — Initial material library

### Goal

Add predefined elastic materials.

Possible materials:

- steel S235;
- steel S275;
- steel S355;
- timber C24;
- glulam GL24h;
- generic concrete.

### Limitation

The material library should initially support only elastic analysis data.

It should not imply full normative design verification.

---

## Milestone 35 — Advanced educational Markdown reports

Completed.

Milestone 35 makes Markdown reports more useful for study, debugging and validation without changing solver behavior.

Added:

- an optional `How to read this report` educational section;
- an executive summary with model size and governing absolute values;
- optional preliminary deflection-check reporting when `DeflectionCheckResult` values are supplied;
- report options to hide educational explanations, model statistics or deflection checks;
- tests for the new report sections and options.

The report remains a textual Markdown output. It is still intentionally separate from HTML/PDF rendering and from any graphical UI.

---

## Milestone 36 — CSV export

Completed.

Milestone 36 adds spreadsheet-friendly CSV export without changing solver behavior.

Added:

- `CsvStructuralResultExporter`;
- CSV export for nodal displacements;
- CSV export for support reactions;
- CSV export for local member end forces;
- CSV export for sampled internal-force diagrams;
- CSV export for sampled displacement/deformed-shape diagrams;
- CSV export for compact governing result summaries;
- CLI command `export-csv`;
- documentation in `docs/structural/csv-export.md`;
- tests for CSV headers, values, invariant numeric formatting and escaping.

Example command:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

CSV export is intended for spreadsheet validation and external post-processing. It is not a complete model exchange format.

---

## Milestone 37 — Public API stabilization

### Goal

Introduce a stable high-level public API before a first technical release.

### Completed

Milestone 37 adds `StructuralSolver2DService` in `StructuralSolver2D.Analysis.PublicApi`.

The facade keeps low-level components available, but gives external applications one preferred entry point for the standard workflow:

- analyze one load case;
- analyze one manual load combination;
- sample internal-force diagrams;
- optionally sample displacement diagrams;
- optionally perform preliminary deflection checks;
- compute compact governing result summaries.

New public API files:

```text
src/StructuralSolver2D.Analysis/PublicApi/StructuralSolver2DService.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisRequest.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisOptions.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisOutput.cs
src/StructuralSolver2D.Analysis/PublicApi/StructuralAnalysisTargetKind.cs
```

The API is intentionally conservative: it wraps the current first-order linear elastic plane-structure analyzer and post-processing pipeline. It does not freeze future internals, does not introduce normative design checks and does not replace specialized low-level solver classes for advanced usage.

---

## Milestone 38 — First technical release

### Goal

Prepare the first GitHub technical release.

Version:

```text
v0.1.0
```

Suggested title:

```text
StructuralSolver2D v0.1.0 - First technical release
```

Release contents:

- README;
- `VERSION`;
- `CHANGELOG.md`;
- `ai-handoff.md`;
- documentation;
- CLI;
- JSON examples;
- Markdown report generation;
- CSV export;
- public API facade;
- benchmark catalog;
- automated tests;
- release notes;
- license.

### Status

Completed.

---

## Milestone 39 — Viewer-ready result data model

### Goal

Create a renderer-independent data model for future graphical result viewers.

### Status

Completed.

Implemented under:

```text
src/StructuralSolver2D.Reporting/Visualization
```

The visualization layer prepares:

- undeformed nodes and members;
- scaled deformed nodal coordinates;
- nodal `Ux`, `Uy` and `Rz`;
- deformed member polylines;
- normal-force, shear-force and bending-moment diagram polylines;
- drawing bounds;
- optional cyclic animation frames.

This milestone intentionally does not add a GUI dependency. It creates the stable data boundary for SVG, PNG, HTML, Avalonia, WPF or OpenCad2D clients.

---

## Upcoming milestones after Milestone 39

The post-M39 roadmap keeps both work streams:

- structural/model/reporting features planned before the viewer discussion;
- graphical viewer features enabled by the Milestone 39 visualization data model.

The unified sequence is:

```text
40 - Parametric model generators
41 - Validation example files in JSON
42 - Static graphical result export: SVG and HTML
43 - XLSX report export
44 - PDF technical report
45 - Section catalog persistence
46 - First simple interactive viewer prototype
47 - Viewer controls for result scale, diagram scale and animation playback
48 - Labels/tooltips for nodal displacements, rotations and diagram values
49 - OpenCad2D integration boundary study
```


### Milestone 43 — XLSX report export

Added a lightweight `.xlsx` workbook exporter based on direct OpenXML package generation.

The workbook contains sheets for summary values, nodal displacements, support reactions, member end forces, internal-force samples and displacement samples.

PNG export remains intentionally deferred; the graphical export path is vector-first through SVG/HTML.

### Milestone 44 — PDF technical report

Added a lightweight PDF technical report exporter.

The report contains model overview, assumptions, executive summary, nodal displacements, support reactions, member end forces and sampled result tables.

The first implementation writes a minimal PDF directly, without external PDF-generation dependencies.

### Milestone 45 — Section catalog persistence

Added reusable section catalogs with JSON persistence.

The catalog stores normal `StructuralSection` records, validates ids and section properties, and can apply reusable sections to a `StructuralModel`.

The next milestone is:

```text
46 - First simple interactive viewer prototype
```

Detailed planning is kept in:

```text
docs/structural/development-plan.md
```

---

# Short-term priority

Completed short-term milestones:

```text
31 - Improved diagrams and characteristic points
32 - Preliminary SLE deflection checks
33 - Parametric sections
34 - Initial material library
35 - Advanced educational reports
36 - CSV export
37 - Public API stabilization
38 - First technical release
39 - Viewer-ready result data model and animation frames
```

The roadmap has been realigned after Milestone 39 to preserve both planned work streams:

1. engineering/product features already planned before the viewer discussion;
2. graphical viewer features made possible by the new viewer-ready data model.

The next recommended step is:

```text
40 - Parametric model generators
```

See also:

```text
docs/structural/development-plan.md
```

---

## Milestone 26 completion update

Milestone 26 adds the first mesh-refinement and convergence benchmark layer.

Added files:

```text
benchmarks/convergence/
  C01-simple-supported-udl-1-elements.json
  C01-simple-supported-udl-2-elements.json
  C01-simple-supported-udl-4-elements.json
  C01-simple-supported-udl-8-elements.json
  C02-point-load-single-element.json
  C02-point-load-explicit-node.json

docs/structural/mesh-refinement.md

tests/StructuralSolver2D.Analysis.Tests/Benchmarks/MeshRefinementConvergenceTests.cs
```

The milestone validates and documents the distinction between:

- nodal finite element results;
- internal displacement interpolation;
- closed-form beam theory values;
- convergence behavior under mesh refinement.

This milestone reinforces the project rule that validation must evolve together with solver capabilities.

---

## Milestone 28 completion update

Milestone 28 improves the automated benchmark runner without changing the solver.

Added/refactored test-side files:

```text
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalog.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkRepository.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkAnalysisRunner.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkResultAssertions.cs
tests/StructuralSolver2D.Analysis.Tests/Benchmarks/BenchmarkCatalogTests.cs
```

The benchmark runner now has a clearer structure:

- catalog loading and schema validation;
- repository path resolution;
- analyzer selection;
- result assertions;
- catalog orchestration.

The catalog validation checks:

- duplicate benchmark ids;
- missing names;
- missing model paths;
- missing model files;
- missing analysis ids;
- invalid tolerances.

The supported automatic result checks currently include:

- support reactions;
- nodal displacements and rotations;
- maximum absolute shear;
- maximum absolute bending moment;
- Truss2D member axial forces;
- named stability and symmetry checks;
- global equilibrium residuals.

This milestone makes the benchmark suite easier to extend before adding more validation cases and future expected-result types.

---

## Milestone 29 completion update

Milestone 29 reorganizes user-facing examples and documents the difference between examples and benchmarks.

Added preferred example folders:

```text
examples/
  README.md
  beams/
  trusses/
  mixed/
  combinations/
```

The new rule is:

```text
examples/   are for learning and CLI usage
benchmarks/ are for validation and regression testing
```

The CLI does not require code changes for this organization because it can analyze any JSON input path.

Existing flat example files may remain temporarily for compatibility, but new examples should use the categorized layout.

Documentation added:

```text
docs/structural/examples-and-benchmarks.md
examples/README.md
```

This milestone prepares the project for a larger benchmark and example catalog without mixing educational examples with validation cases.

---

## Milestone 30 completion update

Milestone 30 adds the initial theory documentation layer.

Added files:

```text
docs/theory/README.md
docs/theory/matrix-method.md
docs/theory/frame2d-element.md
docs/theory/truss2d-element.md
docs/theory/equivalent-nodal-loads.md
docs/theory/local-global-coordinates.md
docs/theory/sign-conventions.md
docs/theory/displacement-interpolation.md
docs/theory/validation-strategy.md
```

This milestone reinforces the educational purpose of StructuralSolver2D.

The new theory documentation explains:

- the displacement/matrix stiffness method;
- the current Frame2D element assumptions;
- the current Truss2D element assumptions;
- equivalent nodal loads;
- local/global coordinate transformations;
- sign conventions;
- displacement interpolation and deformed-shape sampling;
- benchmark and validation strategy.

Milestone 37 introduced a stable public facade that bundles analysis, post-processing and optional preliminary deflection checks for external applications.

---

## Milestone 38 completion update

Milestone 38 prepares the first technical release baseline.

Added release-oriented files:

```text
Directory.Build.props
VERSION
CHANGELOG.md
docs/structural/release-checklist.md
docs/structural/release-notes-v0.1.0.md
```

The release version is:

```text
v0.1.0
```

This milestone does not add new solver behavior. It consolidates the public-facing project state after Milestones 1-37 and documents how to validate, tag and publish the first technical release.

---

## Validation hardening and viewer preparation

Completed after the first technical release preparation:

- validation examples for rigid-joint frames, trusses, Nielsen-type schemes and Gerber beams;
- UI-independent viewer data model for deformed shapes, nodal displacement/rotation display and N/V/M diagram polylines;
- optional cyclic animation-frame data for the deformed shape.

### Milestone 39 — Viewer-ready result data model

Milestone 39 introduces a renderer-independent visualization layer under:

```text
src/StructuralSolver2D.Reporting/Visualization
```

The layer prepares:

- undeformed nodes and members;
- scaled deformed nodal positions;
- deformed member polylines;
- nodal `Ux`, `Uy` and `Rz` values for labels or tooltips;
- normal-force, shear-force and bending-moment diagram polylines;
- drawing bounds;
- optional cyclic deformed-shape animation frames;
- support glyphs, scaled support reactions and static result annotations in SVG/HTML exports.

This is intentionally not a GUI. It is the stable data boundary for future SVG/PNG export, Avalonia/WPF viewers, web canvases or OpenCad2D integration.

---

## Milestone 40 completion update

Milestone 40 adds parametric model generators under:

```text
src/StructuralSolver2D.Core/Model/Generators
```

The first generator set covers:

- simply supported beams;
- Gerber beams with asymmetric loads;
- rigid-joint portal frames;
- Pratt-like bridge trusses;
- isostatic triangular trusses;
- Nielsen/parabolic trusses;
- inverted parabolic trusses;
- hyperstatic double-diagonal trusses.

The generators return normal `StructuralModel` instances and do not depend on analysis, reporting, CLI or UI layers.

---

## Milestone 41 completion update

Milestone 41 adds user-facing validation JSON files under:

```text
examples/validation/
```

The initial catalog contains:

- `rigid-joint-portal-frame.json`;
- `small-bridge-truss.json`;
- `isostatic-triangular-truss-beam.json`;
- `nielsen-parabolic-truss.json`;
- `inverted-parabolic-truss.json`;
- `double-diagonal-hyperstatic-truss.json`;
- `gerber-beam-asymmetric-loads.json`.

Automated tests load every JSON file, validate the structural model and analyze it through the public API.

---

## Unified upcoming milestones after Milestone 41

```text
42 - Static graphical result export: SVG and HTML
43 - XLSX report export
44 - PDF technical report
45 - Section catalog persistence
46 - First simple interactive viewer prototype
47 - Viewer controls for result scale, diagram scale and animation playback
48 - Labels/tooltips for nodal displacements, rotations and diagram values
49 - OpenCad2D integration boundary study
```

The previous viewer milestones are therefore not discarded. They are moved after the generator/export/reporting work so the project remains validated and useful before the interactive UI grows.


### Milestone 46 — Advanced static structural annotations

Added:

- support glyphs in exported SVG/HTML previews;
- scaled reaction arrows and reaction moment glyphs;
- member-length dimensions on the undeformed model;
- maximum displacement callout on the deformed shape;
- maximum-value labels on `N`, `V` and `M` diagrams;
- visualization data structures for supports, reactions and annotations.

These additions remain in the reporting/visualization layer and do not change solver equations.
