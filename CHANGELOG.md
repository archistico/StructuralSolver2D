# Changelog

All notable project-level release changes are documented in this file.

The format follows a simple milestone-based structure. StructuralSolver2D is still experimental and educational; version numbers indicate technical snapshots, not certified engineering releases.

---

## Unreleased

### Fixed

- Deformation animation preserves nonuniform sample origins, fits both cycle peaks and the moving maximum callout, changes speed without phase jumps, and stops when the deformed layer is hidden. Added analytical cantilever and per-sample browser comparisons; coverage now includes 273 browser checks and three additional Reporting cases.
- Second Reporting review: independent SVG context layers, one viewer animation loop, preserved caller options and exact fractional initial controls.
- Viewer starts with model, supports and load arrows; result modes show one quantity at a time, with optional full layers, collapsed secondary controls, selection details, keyboard navigation, current amplification and Fit view. Narrow layouts no longer overflow horizontally.
- CLI `report`/`markdown` produce a concise report; `report-full` retains model tables and sampled appendices. Critical values, locations, units, analysis context and signed result tables remain in the summary; the Markdown API keeps its full default.
- PDF wrapping uses font widths, member forces use separate start/end rows, and WinAnsi text preserves Italian accents. Unsupported scripts fail explicitly instead of replacing identifiers. Markdown escapes special table content, report flags control only their sections, and Markdown/PDF/XLSX absolute summaries use magnitudes.
- Added 34 Reporting and one CLI regression cases; browser coverage now runs 180 checks across three models and three viewport sizes.
- CLI report/export commands reject output paths that would overwrite their input model, including every generated CSV filename.
- JSON input distinguishes missing/null required numbers from explicit zero and identifies null collection elements by path and index.
- All CLI analysis/report/export commands share the public analysis workflow; unused load cases no longer trigger different analyzer behavior between commands.
- A default `loadCombinationId` must identify a combination, explicit target overrides remain supported, and commands reject extra arguments before reading or writing files.
- Removed disconnected duplicate roof nodes from the distributed house-with-balconies example, and console absolute summaries now show magnitudes while detailed results retain their signs.
- Added CLI reader and process regression coverage for source preservation, aliases, input errors, case/combination selection, reactions, extrema and distributed examples.
- Reporting station labels interpolate sampled displacements at the requested quarter-span positions instead of relabeling the nearest sample.
- Interactive viewer reset restores the initial view; zoom and pan account for SVG aspect-ratio margins, and the maximum displacement callout follows deformation scaling and animation.
- PDF pagination checks every wrapped text line, Markdown reports include local support restraints and orientation, and XLSX numeric cells preserve small values with round-trip serialization.
- Visualization and viewer options reject non-finite values and enforce the supported numeric ranges.
- Added 80 Reporting .NET regression cases and 15 browser cases for exported viewer behavior.
- Analysis equilibrates linear systems before relative pivot checks and verifies residuals and finite solutions, rejecting rotated and unloaded mechanisms consistently.
- Released frame end rotations are recovered per member before displacement sampling, including factored point and distributed loads; deflection checks no longer use an artificial zero deformation.
- Independent support directions at a shared node receive their own reactions. Redundant supports use independent equations and a documented minimum-norm reaction distribution.
- Added regression coverage across all analyzers, load combinations, rotated members/supports, redundant constraints and stiffness scales.
- Parabolic truss generators now share chord end nodes and avoid duplicate end-panel diagonals.
- The Gerber generator uses stable cantilever end restraints around its suspended span.
- Validation rejects conflicting load-case/combination identifiers and undefined member types, and reports null member-node references without throwing.
- Section catalogs support zero inertia for truss sections; model validation still requires positive inertia for frame use and rejects negative or non-finite inertia everywhere.
- All parametric generators reject non-finite dimensions.
- Added regression coverage for generator topology, analytical reactions and deflections, nodal equilibrium, load diagrams, section persistence and invalid inputs.

## 0.1.0 - First technical release

### Release type

First technical release / technical preview.

This release consolidates the solver after Milestones 1-37 and creates a stable baseline for future work.

### Added

- Independent .NET 8 solution for StructuralSolver2D.
- Core structural model with nodes, members, materials, sections, supports, load cases and manual load combinations.
- Frame2D linear elastic analysis.
- Truss2D linear elastic analysis.
- Mixed plane-structure analysis with Frame2D and Truss2D members.
- Nodal forces and moments.
- Uniform, point and linearly varying member loads.
- Local/global load direction handling for inclined members.
- Frame2D member end moment releases.
- Support reactions, nodal displacements and local member end forces.
- Internal-force sampling for `N(x)`, `V(x)` and `M(x)`.
- Deformed-shape/displacement sampling for Frame2D members.
- Characteristic internal-force points for reporting and future graphical output.
- Global equilibrium checker.
- Preliminary sampled deflection checks for serviceability-oriented workflows.
- Parametric section helpers.
- Initial elastic material library.
- JSON examples and benchmark catalog.
- Automated benchmark runner.
- Markdown reports with educational explanations, executive summary, model statistics and optional deflection-check tables.
- CSV export for spreadsheet validation and external post-processing.
- Public API facade through `StructuralSolver2DService`.
- Documentation for scope, architecture, model, units, analysis, validation, reporting, CSV export, public API and theory notes.

### Current scope

- First-order linear elastic static analysis.
- Two-dimensional structures made of one-dimensional members.
- Educational and experimental use.
- Benchmark-backed development.

### Not included

- Certified structural design checks.
- Eurocode, NTC or other normative verification.
- Second-order analysis.
- Material nonlinearity.
- Dynamic, modal or seismic analysis.
- 3D structures.
- Plates, shells, solids or mesh FEM.
- CAD integration.

### Recommended validation command

```powershell
dotnet restore StructuralSolver2D.sln
dotnet build StructuralSolver2D.sln
dotnet test StructuralSolver2D.sln
```
