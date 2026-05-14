# StructuralSolver2D - Structural Model

This document describes the initial domain model of StructuralSolver2D.

The model represents a planar structural scheme composed of nodes, members, supports, loads, materials and sections.

## StructuralModel

`StructuralModel` is the root aggregate of the structural scheme.

Conceptual content:

```text
StructuralModel
    Nodes
    Members
    Supports
    Loads
    Materials
    Sections
    LoadCases
    LoadCombinations
```

The model should be independent from any graphical representation.

## StructuralNode

A `StructuralNode` represents a structural point in the 2D plane.

Conceptual properties:

```text
Id
Label
X
Y
```

Initial degrees of freedom for Frame2D analysis:

```text
Ux = horizontal displacement
Uy = vertical displacement
Rz = in-plane rotation
```

Coordinates use the internal length unit: meters.

## StructuralMember

A `StructuralMember` represents a one-dimensional structural element between two nodes.

Conceptual properties:

```text
Id
Label
StartNodeId
EndNodeId
MemberType
MaterialId
SectionId
```

Initial member type:

```text
Frame2D
```

Future member type:

```text
Truss2D
```

A member must have:

- non-null start node;
- non-null end node;
- length greater than model tolerance;
- valid material;
- valid section.

## StructuralSupport

A `StructuralSupport` represents restrained degrees of freedom at a node.

Conceptual properties:

```text
Id
Label
NodeId
RestrainedUx
RestrainedUy
RestrainedRz
```

Examples:

```text
Pinned support:
RestrainedUx = true
RestrainedUy = true
RestrainedRz = false

Fixed support:
RestrainedUx = true
RestrainedUy = true
RestrainedRz = true

Vertical roller:
RestrainedUx = false
RestrainedUy = true
RestrainedRz = false
```

Support presets should be UI or factory helpers. The solver should operate on explicit restraints.

## StructuralLoadCase

A `StructuralLoadCase` groups loads that are applied together.

Conceptual properties:

```text
Id
Name
Category
Description
```

Possible categories:

```text
Permanent
Variable
Snow
Wind
SeismicManual
Accidental
Custom
```

In the first phases, categories are descriptive. Automatic normative combinations are out of scope.

## StructuralLoad

A `StructuralLoad` represents an action applied to a node or member.

Initial load types:

```text
NodalForce
NodalMoment
UniformDistributedLoad
PointLoadOnMember
```

Conceptual properties:

```text
Id
Label
LoadCaseId
LoadType
TargetType
TargetId
Direction
Value
Position
```

Examples:

```text
Nodal force:
TargetType = Node
Direction = GlobalY
Value = -20.0 kN

Uniform distributed load:
TargetType = Member
Direction = GlobalY
Value = -10.0 kN/m
```

## StructuralMaterial

A `StructuralMaterial` stores elastic material data.

Conceptual properties:

```text
Id
Name
ElasticModulus
Density
```

Internal units:

```text
ElasticModulus = kN/m²
Density = kN/m³ or mass density in a later documented convention
```

Density requires a careful convention before self-weight is implemented.

## StructuralSection

A `StructuralSection` stores geometric section properties.

Conceptual properties:

```text
Id
Name
Area
MomentOfInertia
Height
Width
```

Internal units:

```text
Area = m²
MomentOfInertia = m⁴
```

For Frame2D analysis, at minimum the solver needs:

```text
A = area
I = second moment of area around the relevant bending axis
```

Sections can still be entered manually when exact or catalog-derived properties are required.

Milestone 33 adds `StructuralSectionFactory` in `StructuralSolver2D.Core.Model.Sections` for common parametric shapes:

```text
Rectangular(id, width, height)
TimberRectangular(id, width, height)
CircularSolid(id, diameter)
CircularHollow(id, outerDiameter, innerDiameter)
```

The helper methods use meters as input and return ordinary `StructuralSection` records. The generated properties are:

```text
rectangular area: A = b h
rectangular inertia: I = b h³ / 12
solid circular area: A = π d² / 4
solid circular inertia: I = π d⁴ / 64
hollow circular area: A = π (D² - d²) / 4
hollow circular inertia: I = π (D⁴ - d⁴) / 64
```

This is a convenience API, not a material or code-design library. The caller is still responsible for choosing dimensions, material, orientation and appropriate structural assumptions.

## StructuralLoadCombination

A `StructuralLoadCombination` combines load cases with coefficients.

Conceptual properties:

```text
Id
Name
Terms
```

Each term:

```text
LoadCaseId
Factor
```

The first implementation may postpone combinations and solve individual load cases only.

## Model validation rules

The model should detect at least:

- duplicate IDs;
- members with missing nodes;
- members with zero or near-zero length;
- missing materials;
- missing sections;
- invalid elastic modulus;
- invalid area;
- invalid moment of inertia;
- loads targeting missing nodes or members;
- supports targeting missing nodes;
- missing restraints that may cause instability.

Numerical instability must produce a controlled analysis failure, not random results.
