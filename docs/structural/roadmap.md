# StructuralSolver2D - Roadmap

This roadmap describes the recommended development path before any integration with OpenCad2D.

The project starts as an independent solver. OpenCad2D integration is postponed until the calculation engine is stable and validated.

## Milestone 0 - Documentation and scope

Goal:

```text
Define the project vision, scope, architecture, units, model, analysis assumptions and validation plan.
```

Outputs:

```text
docs/structural/vision.md
docs/structural/scope.md
docs/structural/architecture.md
docs/structural/model.md
docs/structural/units.md
docs/structural/analysis.md
docs/structural/validation.md
docs/structural/roadmap.md
```

## Milestone 1 - Independent .NET solution

Goal:

```text
Create an independent .NET solution for the structural solver.
```

Recommended projects:

```text
src/StructuralSolver2D.Core
src/StructuralSolver2D.Analysis
tests/StructuralSolver2D.Core.Tests
tests/StructuralSolver2D.Analysis.Tests
```

The solution must not reference OpenCad2D.

## Milestone 2 - Structural domain model

Goal:

```text
Implement the pure structural model.
```

Initial entities:

```text
StructuralModel
StructuralNode
StructuralMember
StructuralSupport
StructuralLoad
StructuralMaterial
StructuralSection
StructuralLoadCase
```

Also implement model validation rules.

## Milestone 3 - Frame2D solver foundation

Goal:

```text
Implement the basic 2D frame analysis pipeline.
```

Required components:

```text
Frame2D local stiffness matrix
coordinate transformation
global stiffness matrix assembly
global load vector assembly
boundary condition application
linear system solution
nodal displacement recovery
support reaction computation
```

## Milestone 4 - Numerical validation tests

Goal:

```text
Validate the solver against known benchmark cases.
```

Minimum cases:

```text
simply supported beam with UDL
simply supported beam with point load
cantilever with end point load
cantilever with UDL
axial bar
continuous beam
simple portal frame
unstable model
zero-length member
invalid material/section
```

## Milestone 5 - CLI prototype

Goal:

```text
Analyze example models without any graphical interface.
```

Example future command:

```bash
structural2d analyze examples/beam-simple.json
```

The CLI should print:

- model summary;
- validation result;
- nodal displacements;
- support reactions;
- member forces;
- analysis warnings/errors.

## Milestone 6 - Example models

Goal:

```text
Create a library of small example models.
```

Examples:

```text
examples/beam-simple.json
examples/cantilever-point-load.json
examples/cantilever-udl.json
examples/portal-frame.json
examples/truss-simple.json
```

These examples should also be useful for documentation and regression testing.

## Milestone 7 - Reporting

Goal:

```text
Generate readable reports from analysis results.
```

Initial formats:

```text
Markdown
plain text
```

Later formats:

```text
HTML
PDF
```

Report contents:

```text
model data
units
materials
sections
nodes
members
supports
loads
displacements
reactions
member forces
warnings
limitations
```

## Milestone 8 - Load cases and manual combinations

Goal:

```text
Support multiple load cases and user-defined combinations.
```

Initial rule:

```text
No automatic normative combination generation.
```

Combinations are manually defined by the user.

## Milestone 9 - Stabilized public API

Goal:

```text
Prepare the engine for reuse by external clients.
```

Tasks:

- clean public API;
- XML documentation summaries;
- stable result DTOs;
- clear exception/result model;
- documented sign conventions;
- documented units;
- examples for API usage.

## Milestone 10 - Future OpenCad2D integration study

Goal:

```text
Evaluate integration with OpenCad2D after the solver is stable.
```

Possible integration strategy:

```text
OpenCad2D.Structural.Adapter
```

Rules:

- OpenCad2D may reference StructuralSolver2D;
- StructuralSolver2D must not reference OpenCad2D;
- structural entities remain explicit;
- CAD entities may be used as background/snap/reference only;
- no automatic CAD-to-structure conversion is required.

## Milestone 11 - Preliminary serviceability checks

Goal:

```text
Add preliminary displacement and deflection checks.
```

This is the first step toward design assistance, but not full normative verification.

## Milestone 12 - Preliminary steel/timber checks

Goal:

```text
Add clearly documented preliminary checks for simple steel and timber members.
```

Possible steel checks:

```text
tension
simple compression
bending
shear
simplified N + M interaction
```

Possible timber checks:

```text
bending
shear
tension parallel to grain
compression parallel to grain
instantaneous/final deflection in simplified form
```

Excluded initially:

```text
connections
advanced instability
fire
fatigue
seismic design
complete normative verification
```
