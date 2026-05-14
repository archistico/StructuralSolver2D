# Viewer data model

This document describes the first UI-independent foundation for graphical result viewing.

The goal is not to introduce a desktop or web viewer yet. The goal is to create stable, testable data structures that any future viewer can render.

Possible renderers include:

- Avalonia canvas;
- WPF canvas;
- SVG export;
- PNG export;
- web canvas;
- OpenCad2D integration.

---

## Current scope

The current viewer data model can prepare:

- undeformed nodes;
- undeformed member axes;
- scaled deformed node positions;
- scaled deformed member shapes;
- node displacements and rotations;
- normal-force diagram polylines;
- shear-force diagram polylines;
- bending-moment diagram polylines;
- drawing bounds;
- optional cyclic animation frames for the scaled deformed shape.

The implementation lives in:

```text
src/StructuralSolver2D.Reporting/Visualization
```

The main entry point is:

```csharp
StructuralVisualizationModelBuilder
```

---

## Why this layer exists

A graphical viewer should not directly depend on solver internals.

The solver works with structural concepts:

- nodes;
- members;
- displacements;
- rotations;
- internal-force samples;
- local/global coordinate systems.

A renderer instead needs drawing geometry:

- points;
- polylines;
- bounds;
- labels;
- scale factors;
- diagram groups.

`StructuralVisualizationModelBuilder` converts solver results into renderer-friendly geometry.

This makes the graphical layer easier to test and keeps future UI code thin.

---

## Deformed shape

The deformed shape is generated with a configurable scale factor:

```csharp
new VisualizationOptions
{
    DeformationScale = 100.0,
};
```

For members with displacement samples, the builder uses the sampled global displacements along the member.

For members without displacement samples, for example simple truss members, the builder falls back to a straight deformed segment between the deformed end nodes.

This is enough for the first viewer stage and avoids coupling the viewer to a specific element formulation.

---

## Nodal displacement and rotation display

Each `VisualizationNode` contains:

- undeformed position;
- deformed position;
- `Ux`;
- `Uy`;
- `Rz`.

A future viewer can use these values to show:

- nodal displacement labels;
- rotation labels;
- hover tooltips;
- tables synchronized with graphics.

Rotations are stored in radians and are not directly converted into a graphical rotation glyph yet.

---

## Internal-force diagrams

Internal-force diagrams are converted to polylines offset from the undeformed member axis along the local normal direction.

Supported diagram kinds are:

- `NormalForce`;
- `ShearForce`;
- `BendingMoment`.

Each quantity has an independent scale:

```csharp
new VisualizationOptions
{
    NormalForceDiagramScale = 0.01,
    ShearForceDiagramScale = 0.01,
    BendingMomentDiagramScale = 0.02,
};
```

This is important because N, V and M use different units and usually need different visual scales.

---

## Animation readiness

The current model can optionally prepare cyclic animation frames:

```csharp
new VisualizationOptions
{
    DeformationScale = 100.0,
    AnimationFrameCount = 24,
};
```

`AnimationFrameCount = 0` disables frame generation.

Each `VisualizationAnimationFrame` contains:

- frame index;
- deformation factor;
- frame-specific nodes;
- frame-specific deformed member shapes.

The factor follows a sinusoidal cycle, so the prepared frames can be used directly by a simple viewer animation loop.

The actual timer, playback controls and canvas invalidation still belong to the UI layer, not to the reporting/data layer.

---

## Planned next steps

Recommended next milestones:

1. SVG result preview export.
2. First simple viewer window or web page.
3. Interactive diagram scale controls.
4. Playback controls for prepared animation frames.
5. Labels and tooltips for nodal values and diagram values.
6. OpenCad2D integration.
