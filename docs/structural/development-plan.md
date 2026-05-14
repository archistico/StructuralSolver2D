# StructuralSolver2D Development Plan After Milestone 39

This document keeps the post-Milestone-39 roadmap aligned.

Milestone 39 introduced a renderer-independent visualization data model. During planning, two future-roadmap threads temporarily overlapped:

1. engineering/product features already planned before the viewer discussion;
2. graphical viewer features derived from the new visualization model.

The unified roadmap below keeps both threads and renumbers them consistently.

---

## Completed baseline

The current completed baseline is:

```text
31 - Improved internal-force diagrams and characteristic points
32 - Preliminary SLE deflection checks
33 - Parametric sections
34 - Initial material library
35 - Advanced educational Markdown reports
36 - CSV export
37 - Public API stabilization
38 - First technical release
39 - Viewer-ready result data model and animation frames
40 - Parametric model generators
41 - Validation example files in JSON
```

Milestone 39 is deliberately not a GUI. It creates the stable data boundary that future SVG, PNG, HTML, Avalonia, WPF or OpenCad2D viewers can consume.

---

## Completed Milestones 40-41 and next milestones

### Milestone 40 — Parametric model generators

Status: completed. The Core project now contains parametric generators for beams, frames and trusses.

The generator layer returns normal `StructuralModel` instances and remains independent from analysis, reporting, CLI and UI projects.

---

### Milestone 41 — Validation example files in JSON

Status: completed. The first validation JSON files live under `examples/validation/` and are covered by automated analysis smoke tests.

The initial files cover rigid-joint frames, small bridge trusses, isostatic trusses, Nielsen/parabolic schemes, inverted parabolic schemes, double-diagonal hyperstatic trusses and Gerber beams with asymmetric loads.

---

### Milestone 42 — Static graphical result export: SVG and HTML

Status: completed. The exporter produces vector-first static previews through SVG and HTML. PNG export is intentionally deferred.

Scope:

- undeformed model;
- scaled deformed shape;
- N/V/M diagrams;
- configurable deformation scale;
- configurable diagram scales;
- simple HTML wrapper around SVG.

This milestone is still not an interactive viewer. It is usable from CLI and tests.

---

### Milestone 43 — XLSX report export

Status: implemented.

The first workbook export creates spreadsheet reports richer than CSV without changing solver behavior.

Scope:

- dependency-free XLSX package writer;
- summary sheet;
- nodal displacement sheet;
- support reaction sheet;
- member end-force sheet;
- internal-force sample sheet;
- displacement sample sheet;
- CLI command `export-xlsx`.

### Milestone 44 — PDF technical report

Goal: generate a professional technical report suitable for review and attachment to a calculation note.

Expected contents:

- cover/header metadata;
- assumptions and units;
- model input tables;
- analysis summary;
- reactions/displacements/member forces;
- characteristic points;
- preliminary serviceability checks;
- static figures from Milestone 42 where available.

The wording must remain clear that the current solver performs first-order linear elastic analysis and does not provide complete code-compliant design verification.

---

### Milestone 45 — Section catalog persistence

Status: completed.

Implemented:

- reusable `StructuralSectionCatalog`;
- JSON catalog format with schema version;
- load/save helpers;
- validation of section values and ids;
- duplicate id detection;
- apply-to-model workflow;
- tests and documentation.

Future extensions remain possible for steel/timber profile libraries and GUI editing.

---

### Milestone 46 — Advanced static structural annotations and support visualization

Status: completed.

Implemented:

- support symbols for simple support, hinge and fixed support in SVG/HTML exports;
- scaled support reaction arrows and reaction moment glyphs with value labels;
- member-length dimensions on the undeformed model;
- maximum displacement callout on the deformed shape;
- maximum value labels on `N`, `V` and `M` diagrams;
- visualization-layer data objects for supports, reactions and annotations.

This milestone intentionally stays inside the reporting/visualization layer so that a future viewer can reuse the same prepared data.

---

### Milestone 47 — First simple interactive viewer prototype

Goal: build the first minimal interactive viewer using the visualization data already produced by the reporting layer.

Candidate technologies:

- Avalonia;
- WPF;
- simple HTML/SVG viewer.

Preferred first step: keep the viewer separate from solver projects and consume only public API/reporting visualization data.

Minimum features:

- pan/zoom;
- undeformed model;
- deformed shape;
- one visible diagram type at a time.

---

### Milestone 48 — Viewer controls for scales and animation playback

Goal: add controls to the viewer.

Controls:

- deformation scale;
- normal-force diagram scale;
- shear-force diagram scale;
- bending-moment diagram scale;
- show/hide undeformed model;
- show/hide deformed shape;
- play/pause animation;
- animation speed.

---

### Milestone 49 — Labels and tooltips for nodal and diagram values

Goal: make the viewer useful for inspection, not only visualization.

Expected labels/tooltips:

- node id;
- member id;
- nodal `Ux`, `Uy`, `Rz`;
- support reactions;
- diagram value at sampled points;
- max/min diagram values;
- characteristic points where available.

---

### Milestone 50 — OpenCad2D integration boundary study

Goal: define the integration boundary without coupling the solver to OpenCad2D.

The correct dependency direction remains:

```text
OpenCad2D or another UI
    -> adapter/client layer
        -> StructuralSolver2D.Core
        -> StructuralSolver2D.Analysis
        -> StructuralSolver2D.Reporting
```

The solver projects must not reference OpenCad2D, Avalonia UI controls, WPF UI classes or CAD persistence types.

---

## Strategic note

The viewer path is important, but the project should not skip generators, JSON validation examples and professional exports. Those features make the solver easier to validate and easier to compare with existing structural tools before investing heavily in interaction design.


### Milestone 44 — PDF technical report

Status: completed. The Reporting project now includes a lightweight dependency-free PDF exporter for technical computation reports.

Scope implemented:

- model overview;
- assumptions and limitations;
- executive summary;
- nodal displacements;
- support reactions;
- member end forces;
- sampled internal-force rows;
- sampled displacement rows;
- CLI command `export-pdf`.

The next milestone is Milestone 47 — First simple interactive viewer prototype.


### Support orientation refinement

Added mechanically oriented translational support constraints for `StructuralSupport.OrientationDegrees`.

The analysis layer now enforces restrained local support directions through homogeneous constraint equations. This makes rotated simple supports / rollers structurally active, not only graphical. Axis-aligned support behavior remains unchanged.
