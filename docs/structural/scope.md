# StructuralSolver2D - Scope

This document defines the initial functional scope of StructuralSolver2D.

The purpose is to keep the project realistic, testable and technically safe during the first development phases.

## Included in the initial scope

The initial solver should support:

- planar 2D structural analysis;
- one-dimensional structural members;
- linear elastic material behavior;
- first-order static analysis;
- small displacements;
- constant cross-sections along each member;
- idealized supports;
- static loads;
- deterministic numerical results;
- automated validation tests.

## Initial structural schemes

The engine should be able to represent and analyze:

- simply supported beams;
- continuous beams;
- cantilever beams;
- single-bay portal frames;
- simple planar frames;
- simple trusses;
- axial bars / ties / struts;
- columns with cantilevers.

## Excluded from the initial scope

The initial scope excludes:

- full 3D structural analysis;
- complete building modeling;
- automatic seismic design;
- dynamic analysis;
- modal analysis;
- response spectrum analysis;
- nonlinear material behavior;
- plastic hinges;
- large displacement analysis;
- second-order effects;
- buckling analysis as an automatic design check;
- plate elements;
- shell elements;
- solid elements;
- soil-structure interaction;
- advanced foundation modeling;
- masonry structural behavior;
- reinforced concrete design;
- steel connection design;
- timber connection design;
- fire checks;
- fatigue checks;
- complete normative verification.

## Supported degrees of freedom

For the first Frame2D analysis model, each structural node has three degrees of freedom:

```text
Ux = horizontal displacement
Uy = vertical displacement
Rz = in-plane rotation
```

This allows the first solver to analyze beams, columns and planar frames.

## Initial member types

The first member type should be:

```text
Frame2D
```

A `Frame2D` member carries:

- axial force;
- shear force;
- bending moment.

A `Truss2D` member can be added later for pure axial truss behavior.

## Initial support types

The engine should represent supports as restrained degrees of freedom.

Typical presets:

```text
Free node:
Ux free
Uy free
Rz free

Simple vertical support:
Ux free
Uy restrained
Rz free

Pinned support:
Ux restrained
Uy restrained
Rz free

Fixed support:
Ux restrained
Uy restrained
Rz restrained

Custom support:
Ux configurable
Uy configurable
Rz configurable
```

Internally, support presets should resolve to explicit DOF restraints.

## Initial load types

The first load types should be:

- nodal force Fx;
- nodal force Fy;
- nodal moment Mz;
- uniform distributed load on member;
- point load on member;
- optional self-weight in a later step.

Deferred load types:

- variable distributed load;
- trapezoidal load;
- thermal load;
- imposed support settlement;
- prestress;
- automatic snow load;
- automatic wind load;
- automatic seismic load.

Wind, snow or seismic actions may be entered manually as user-defined loads, but they are not automatically generated in the initial scope.

## Initial results

The solver should produce:

- nodal displacements;
- nodal rotations;
- support reactions;
- member end forces;
- axial force N;
- shear force V;
- bending moment M;
- sampled internal force diagrams;
- model validation warnings;
- controlled analysis failure messages.

## Initial non-goals

The first versions should not attempt to say whether a structure is legally or normatively verified.

The engine may later support preliminary checks, but the first target is analysis, not design certification.
