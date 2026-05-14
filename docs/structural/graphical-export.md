# Static graphical result export

Milestone 42 adds the first static graphical export layer based on the viewer-ready visualization model introduced earlier.

The goal is to produce lightweight graphical outputs that are:

- easy to open and share;
- independent from a desktop UI toolkit;
- suitable for documentation, previews and future reporting workflows.

## Current export formats

The current implementation supports:

- standalone SVG export;
- standalone HTML export with embedded inline SVG.

PNG export is intentionally deferred. At this stage, keeping the output vector-based is more useful because:

- the geometry remains sharp at any zoom level;
- SVG can later be embedded in HTML, Markdown, PDF or documentation workflows;
- the same geometry can be reused by a future interactive viewer.

## Main classes

Located under `StructuralSolver2D.Reporting/Visualization`:

- `SvgStructuralResultExporter`
- `SvgExportOptions`
- `HtmlStructuralResultPreviewExporter`
- `HtmlPreviewExportOptions`

These exporters consume an already prepared `StructuralVisualizationModel`.

## Rendering approach

The exporter renders:

- undeformed structural model;
- support symbols on the undeformed model;
- rotated support symbols through `StructuralSupport.OrientationDegrees`;
- member-length dimensions between connected nodes;
- scaled support reactions with value labels;
- deformed shape;
- maximum displacement callout;
- internal-force diagrams `N`, `V`, `M` when available;
- maximum value labels on the diagrams;
- node labels;
- title and small legend.

The HTML export is intentionally simple: it wraps the SVG in a lightweight page with a title and a few summary values.

## CLI usage

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-svg exampleseams\simple-supported-beam.json reports\graphics\simple-supported-beam.svg
```

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-html exampleseams\simple-supported-beam.json reports\graphics\simple-supported-beam.html
```

Optional final argument:

```text
[loadCaseId|combinationId]
```

## Intentional limitations

This milestone does **not** yet provide:

- interactivity;
- animation playback controls;
- mouse hover tooltips;
- dynamic scale sliders;
- CAD integration.

These are intentionally reserved for later milestones. The current output is static but now aims to be structurally informative, not only visually descriptive.


## Rotated support symbols

`StructuralSupport` now exposes `OrientationDegrees`, expressed in model coordinates as a counterclockwise angle in degrees.

The static SVG/HTML exporter uses this value to rotate only the support symbol. Labels remain horizontal for readability.

Example JSON fragment:

```json
{
  "id": "SB",
  "nodeId": "B",
  "restrainedUx": false,
  "restrainedUy": true,
  "restrainedRz": false,
  "type": "SimpleSupport",
  "orientationDegrees": 30.0
}
```

Current limitation: the orientation is graphical/model metadata. The solver still applies restraints to global `Ux`, `Uy` and `Rz`. Full mechanically oriented restraints, such as a biella/pendolo acting along an arbitrary axis, require a dedicated solver milestone.
