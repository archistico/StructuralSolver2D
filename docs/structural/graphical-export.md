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
- optional nodal displacement labels;
- optional member station displacement labels at `L/4`, `L/2` and `3L/4`;
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

Support orientation is now used consistently by both the static SVG/HTML rendering layer and the analysis layer for translational restraints. A simple support with `restrainedUy: true` and a non-zero `orientationDegrees` restrains the corresponding local support direction. Future work can add named helpers for biella/pendolo and pattino/manicotto on top of this mechanism.


## Interactive viewer

A separate standalone HTML viewer can be generated with `export-viewer`. It embeds the SVG result scene and adds pan, zoom, reset view and layer toggles. The viewer remains read-only and uses the same `StructuralVisualizationModel` data as the static exports.


## Applied load visualization

The SVG/HTML export can now draw applied loads as a dedicated `loads` layer. Supported graphical representations include:

- nodal concentrated forces as arrows;
- concentrated member point loads as arrows at the normalized member position;
- nodal moments as circular arrows;
- uniform distributed loads as repeated arrows along the member;
- linearly varying distributed loads as tapered distributed-load glyphs.

Load labels show the load label when available and the numerical value with units. Concentrated forces are displayed in `kN`, moments in `kNm`, and distributed loads in `kN/m`.

For load combinations, the viewer currently shows available model loads as graphical reference. It does not yet build a separately factored graphical load set for each combination term.
