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
- CLI commands;
- JSON input examples;
- Markdown reports;
- benchmark catalog;
- automated benchmark runner;
- diagnostic analysis errors;
- README and AI handoff documentation.

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

### Planned features

Detect characteristic points such as:

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

---

## Milestone 32 — Preliminary SLE deflection checks

### Goal

Add preliminary serviceability checks for deflection.

### Example check

```text
maximum deflection <= L / limit
```

Common limits:

```text
L/200
L/250
L/300
L/400
```

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

---

## Milestone 33 — Parametric sections

### Goal

Generate section properties from simple geometric inputs.

### Initial section types

- rectangular section;
- circular solid section;
- circular hollow section;
- simple timber rectangular section.

### Example

```json
{
  "id": "RECT_100x200",
  "type": "Rectangular",
  "width": 0.10,
  "height": 0.20
}
```

The generated section should compute:

- area;
- second moment of area.

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

### Goal

Make the Markdown report more useful for study, debugging and validation.

### Planned additions

- sign convention summary;
- model diagnostics;
- global equilibrium residuals;
- benchmark expected/computed comparison;
- notes on FEM discretization;
- notes on local/global coordinates;
- warnings and limitations;
- better formatting for diagrams and sampled values.

---

## Milestone 36 — CSV export

### Goal

Export results to CSV for spreadsheet analysis.

Possible exports:

- nodal displacements;
- support reactions;
- member end forces;
- internal-force samples;
- deformed-shape samples;
- result extrema.

Possible command:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beam.json output\
```

---

## Milestone 37 — Public API stabilization

### Goal

Review and stabilize public APIs before a first technical release.

### Review checklist

- namespaces;
- public class names;
- public property names;
- record immutability;
- exception messages;
- XML documentation comments;
- separation between Core, Analysis, Reporting and CLI;
- extensibility for future analyzers;
- consistency of units and sign conventions.

---

## Milestone 38 — First technical release

### Goal

Prepare the first GitHub release.

Possible version:

```text
v0.1.0
```

Suggested title:

```text
StructuralSolver2D v0.1.0 - Linear 2D structural analysis core
```

Release contents:

- README;
- ai-handoff.md;
- documentation;
- CLI;
- JSON examples;
- Markdown report generation;
- benchmark catalog;
- automated tests;
- release notes;
- license.

---

## Milestone 39 — Future OpenCad2D integration study

### Goal

Study how StructuralSolver2D could be integrated into OpenCad2D.

### Principle

StructuralSolver2D must remain independent.

OpenCad2D should become a possible graphical client, not a dependency of the solver.

### Possible integration layer

```text
OpenCad2D.Structural.Adapter
```

The adapter would be responsible for:

- creating `StructuralModel` instances from graphical input;
- drawing structural nodes;
- drawing members;
- drawing supports;
- drawing loads;
- displaying reactions;
- displaying internal-force diagrams;
- displaying deformed shapes;
- generating reports.

---

## Milestone 40 — Experimental viewer

### Goal

Evaluate a lightweight viewer before full OpenCad2D integration.

Possible options:

- SVG export;
- static HTML report with diagrams;
- small Avalonia viewer;
- Blazor viewer;
- command line generated graphics.

This is not urgent.

The solver and validation suite remain the priority.

---

# Short-term priority

The immediate recommended order is:

```text
29 - Examples and benchmarks reorganization
30 - Initial theory documentation
```

After that, the project can move toward:

```text
31 - Improved diagrams and characteristic points
32 - Preliminary SLE deflection checks
33 - Parametric sections
34 - Material library
35 - Advanced educational reports
36 - CSV export
37 - Public API stabilization
38 - First technical release
```

---

# Long-term direction

StructuralSolver2D should remain:

```text
an independent structural analysis engine
```

Possible future clients:

- CLI;
- report generator;
- educational examples;
- OpenCad2D adapter;
- lightweight viewer;
- external applications.

The long-term architecture should remain:

```text
Core
  structural model

Analysis
  solvers and post-processing

Reporting
  textual and document output

CLI
  command line usage

Future UI / CAD clients
  optional external consumers
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
