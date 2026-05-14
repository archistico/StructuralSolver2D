# Changelog

All notable project-level release changes are documented in this file.

The format follows a simple milestone-based structure. StructuralSolver2D is still experimental and educational; version numbers indicate technical snapshots, not certified engineering releases.

---

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
