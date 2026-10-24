# Modal analysis (M57) — user guide and limitations

**Status (2026-10-23):** M57 implementation ready for Windows .NET 8 acceptance, **not yet accepted**. Previous M51–M56/FIX1 benchmarks passed on user's environment. This module does **free vibration of linear elastic 2D structures**, not forced/transient/seismic response.

**M57-FIX1 (2026-10-23):** The original Windows reporting test detected that
Markdown omitted the `Mass:` prefix from the normalisation metadata. Reports now
print `StructuralModalAnalysisOutput.Normalization` (normally
`Mass: phi^T M phi = 1; sign arbitrary`). The Windows .NET 8 retest of the fix
is pending; the modal computations themselves were not changed.

**M57-FIX2 (2026-10-23):** The distributed-examples **CLI regression test**
previously tried to run the static `analyze` and `export-viewer` commands on the
intentional modal-only example `m57-modal-cantilever.json`. That input has no
`loadCases`, so the static-command rejection was correct. The example test
now distinguishes static models from modal-only models using JSON content and
positively tests all four modal commands; static commands continue to reject
such inputs. No application code or modal calculations changed. Re-run the
complete Windows .NET 8 tests before accepting M57.

## Using the CLI

From the project root (requires .NET SDK 8):

```powershell
dotnet run --project src/StructuralSolver2D.Cli -- modal examples/dynamics/m57-modal-cantilever.json 3
dotnet run --project src/StructuralSolver2D.Cli -- modal-report examples/dynamics/m57-modal-cantilever.json modal.md 3
dotnet run --project src/StructuralSolver2D.Cli -- modal-csv examples/dynamics/m57-modal-cantilever.json modal-results 3
dotnet run --project src/StructuralSolver2D.Cli -- modal-viewer examples/dynamics/m57-modal-cantilever.json modal.html 3
```

Open `modal.html` locally in a browser to select a natural mode, see frequency [Hz], period [s] and residual, animate the **normalized shape**, and inspect relative nodal translational contribution. Amplitude and playback speed are **visual parameters** only: the speed slider controls illustrative cycles per second and is not the physical eigenfrequency. Element interiors on Frame2D members are drawn using cubic Hermite interpolation, including nodal rotations. Truss2D members use linear interpolation. The viewer is independent from the static-results viewer.

The CLI commands for **modal** analysis accept JSON without `loadCases`, but the `dynamics` block is mandatory and needs `schemaVersion:1`. Static CLI commands still require their normal input. Mass comes from positive `materials[].unitWeight` [kN/m³], plus optional additional masses from `dynamics.nodalMasses`; no mass is invented. For the exact contract see `examples/dynamics/m57-modal-cantilever.json` and `docs/theory/dynamic-2d-elements.md`.

For C# applications:

```csharp
var request = new StructuralModalAnalysisRequest(model, dynamics, requestedModeCount: 3);
StructuralModalAnalysisOutput result = new StructuralSolver2DService().AnalyzeModes(request);
foreach (var mode in result.Modes)
    Console.WriteLine($"Mode {mode.ModeNumber}: {mode.FrequencyHertz:G8} Hz, T={mode.PeriodSeconds:G8} s");
```

`StructuralModalAnalysisService` is an equivalent dedicated entry point. Results provide global nodal `Ux`, `Uy`, `Rz` entries and the quality diagnostics from M55. Modes are normalized to `phiᵀ M phi = 1`; the sign is arbitrary, and degenerate-eigenvalue subspaces can admit different bases. Neither components nor rendered amplitudes are real structural displacements. Normal-force/shear/moment diagrams are **not** part of this free-vibration result.

## Supported and rejected cases

- Supports normal or inclined, including redundant supports, are projected by M54 onto admissible coordinates.
- Consistent distributed masses and additional nodal masses are supported; massless unused truss rotations are removed when truly uncoupled.
- Unsupported cases return explicit diagnostics: free-free, remaining rigid motion, mechanisms, singular/coupled massless coordinates, frequency-dependent release models, invalid/absent weights and dense models with more than 120 reduced DOFs.
- Mode count is positive; a request beyond admissible physical DOFs is capped with a warning.
- M53–M56 remain the independent scientific benchmarks. This feature is educational/research software, **not validated for structural or seismic design certification**.

## Exported results and numerical reproducibility

`modal-report` writes the frequency/period/residual Markdown summary and warnings. `modal-csv` writes `modal-frequencies.csv` and `modal-shapes.csv` in invariant-culture numeric representation; node IDs are quoted and guarded against spreadsheet formula injection. `modal-viewer` creates a single standalone HTML file without external resources. The previously implemented static Markdown/CSV/PDF/XLSX/viewer contracts are unchanged. Modal PDF/XLSX extensions are deferred.

**Acceptance:** use `.\scripts\run-m57-tests.ps1` from a freshly extracted M57 **FULL ZIP**, confirm new CLI files, then test mode selection/play/pause/amplitude controls in a browser. The script performs a clean Release build specifically to avoid stale files encountered on M56. If Node.js is available, it also runs `scripts/m57-viewer-smoke.js` against a minimal DOM mock.
