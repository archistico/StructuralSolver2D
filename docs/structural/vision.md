# StructuralSolver2D - Vision

StructuralSolver2D is an independent .NET/C# engine for planar structural analysis of simple static schemes.

The project is intentionally separated from OpenCad2D during the first development phases. The goal is to consolidate the calculation engine, validate the numerical results and keep the testing workflow fast and focused.

OpenCad2D may become a future graphical client of the engine, but it is not a dependency of the solver.

## Product idea

StructuralSolver2D is designed to analyze 2D structures made of one-dimensional structural elements.

The initial target is not a generic FEM platform and not a complete professional structural design suite. The initial target is a clear, testable and reliable structural solver for simple planar schemes.

## Target structures

The engine should support, in progressive steps:

- simply supported beam;
- continuous beam;
- cantilever beam;
- single-bay portal frame;
- simple truss;
- simple planar frame;
- column with cantilever;
- axial bar / tie / strut;
- simple braced schemes.

## First objective

The first technical objective is:

> Build a stable and validated linear elastic 2D analysis engine for one-dimensional structural elements.

The engine must calculate:

- nodal displacements;
- nodal rotations;
- support reactions;
- axial force N;
- shear force V;
- bending moment M;
- basic result envelopes in later phases.

## What the project is not, initially

StructuralSolver2D is not initially intended to perform:

- full building analysis;
- BIM structural modeling;
- complete NTC / Eurocode design checks;
- seismic analysis of buildings;
- dynamic analysis;
- modal analysis;
- nonlinear material analysis;
- nonlinear geometric analysis;
- advanced instability analysis;
- fire design;
- fatigue checks;
- steel or timber connection design;
- plate, shell or solid FEM analysis;
- 3D structural analysis.

## Relationship with OpenCad2D

OpenCad2D is a possible future graphical interface for StructuralSolver2D.

During the first phase:

- StructuralSolver2D remains independent;
- OpenCad2D is not referenced by the solver projects;
- CAD entities are not part of the structural model;
- graphical integration is postponed until the solver is stable.

Future integration may happen through an adapter layer, for example:

```text
OpenCad2D
    ↓
OpenCad2D.Structural.Adapter
    ↓
StructuralSolver2D.Core
StructuralSolver2D.Analysis
```

The dependency direction must remain one-way: OpenCad2D may depend on StructuralSolver2D, but StructuralSolver2D must not depend on OpenCad2D.

## Development principle

The project grows through small, validated steps.

Every milestone should:

- compile successfully;
- include automated tests;
- contain at least one known benchmark case;
- document assumptions and limitations;
- avoid adding UI complexity before the analysis engine is reliable.
