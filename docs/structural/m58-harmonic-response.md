# M58 — Linear harmonic frequency response (experimental)

**Status:** implementation delivered 2026-10-23, pending clean .NET 8 test execution and HTML acceptance. M57-FIX2 also remains awaiting user rerun.

This milestone implements a **linear, steady-state, sinusoidal** response for M53/M54-supported 2D FEM models. It is not a time-history solver, seismic response calculation, or structural design/safety verdict. Static and free-vibration APIs remain unchanged.

## Mechanics and reference formulation

For a harmonic nodal load in a coherent kN–m–s system,

`M u¨(t) + C u˙(t) + K u(t) = F0 sin(Ωt)`.

In the 1-DOF case, `ωn = sqrt(k/m)`, `ζ = c/(2*m*ωn)`, `r=Ω/ωn`. Relative to the static displacement `F0/k`, the steady-state amplification factor is

`H = 1/sqrt((1-r*r)^2 + (2*ζ*r)^2)`.

The phase shift relative to **the input sine** is `phase = atan2(-2*ζ*r,1-r*r)`. For the complex displacement phasor `U`, our convention is

`u(t) = Re(U) sin(Ωt) + Im(U) cos(Ωt) = |U| sin(Ωt + arg U)`.

The negative imaginary component means the displacement **lags** the applied harmonic force. At undamped resonance (`ζ=0`, `Ω=ωn`) no finite steady-state amplitude exists and the solver must reject it, not return an infinite or arbitrary value. A zero-force mode is omitted from its modal sum.

## MDOF formulation

We reuse the M53 consistent mass and stiffness, and the M54 admissible basis `Z`. The M55 eigensolver calculates the **complete** reduced set of mass-normalized modes (one for every admissible coordinate), with `φᵀ M φ=1`. For node-level force `F0`, reduced force is `Fr=ZᵀF0` and modal generalized force is `pj=φjᵀFr`. We obtain the modal transfer

`qj = pj / (ωj² - Ω² + i*2*ζj*ωj*Ω)`, then `U = Σ (Z φj)*qj`.

The resulting complex global nodal coordinates are physically scaled amplitudes (m / radians), unlike normalized free-vibration shapes from M57. We do **not** use mode truncation or spurious masses. No new solvers or inverses are introduced in the static path.

### Explicit damping, not implied

Two mutually exclusive models are supported:

- Uniform **modal** damping, ratio `ζ ≥ 0` applied identically to every eigenmode.
- Proportional **Rayleigh damping** `C = αM + βK`, with `α [1/s]` and `β [s]`, both finite and ≥0; each mode uses `ζj = α/(2ωj) + βωj/2`.

Both coefficients **must** be supplied for Rayleigh damping. No damping defaults are inserted. The Rayleigh damping model is entirely distinct from the Rayleigh **quotient** used to check the eigenvalues in M55. These are idealized linear viscous models, not a prediction of material hysteresis or structural friction.

### Input/output and supported scope

- Input forces `Fx/Fy` [kN], `Mz` [kN*m], global `Ux/Uy` [m], `Rz` [rad]; all nodal harmonic loads are in phase, using the same `sin(Ωt)` origin. Opposite phase is represented by negative force.
- Excitation `frequenciesHertz` [Hz] is sampled exactly at user-provided frequencies, including 0 as the static limit. For a plotted sweep provide ordered frequencies, with enough points around a resonance peak.
- Up to 1001 frequency samples and 120 M54-reduced DOFs. The same end releases, free-free, singular-mass/rigid modes and massless-stiffness-coupled coordinates that were unsupported in M53–M57 remain unsupported.
- API results: frequency, node ID, complex real/imaginary phasor, amplitude, phase in radians/degrees, modal natural frequencies, ratios and warnings.
- CLI: `harmonic`, `harmonic-report`, `harmonic-csv`, `harmonic-viewer` (CSV/Markdown plus standalone HTML amplitude/phase plots).
- The HTML viewer linearly joins supplied frequency samples; it is **not** a peak-finder. The physical amplitude curve may have unsampled narrow peaks; the viewing interface must not be mistaken for time history.

## JSON example and CLI

`examples/dynamics/m58-harmonic-cantilever.json` remains a **modal-only input**: no static load case is introduced. It adds the opt-in block:

```json
"harmonic": {
  "forces": [{"nodeId":"N8", "fy":-1.0}],
  "frequenciesHertz": [0, 2, 8, 12.5, 13, 15, 25, 50],
  "damping": {"uniformModalRatio":0.05}
}
```

Alternatively, `"damping": {"rayleighAlphaInverseSeconds":0.02,"rayleighBetaSeconds":0.0001}`.

From the root:

```powershell
dotnet run -c Release --project src/StructuralSolver2D.Cli -- harmonic examples/dynamics/m58-harmonic-cantilever.json
dotnet run -c Release --project src/StructuralSolver2D.Cli -- harmonic-report examples/dynamics/m58-harmonic-cantilever.json out/m58.md
dotnet run -c Release --project src/StructuralSolver2D.Cli -- harmonic-csv examples/dynamics/m58-harmonic-cantilever.json out/m58.csv
dotnet run -c Release --project src/StructuralSolver2D.Cli -- harmonic-viewer examples/dynamics/m58-harmonic-cantilever.json out/m58.html
```

## Acceptance evidence required

1. Extract new FULL ZIP into a **clean directory**, then run `scripts/run-m58-tests.ps1`. It performs clean .NET 8 build, M57-FIX2 regression, focused M58 tests, full solution regression, and harmonic CLI smoke output checks.
2. SDOF reference checks at low, resonant and high excitation frequencies; damping and phase; reject zero-damping resonance.
3. MDOF cantilever static-frequency limit (independent Euler–Bernoulli analytical tip displacement), first-mode resonance peak, finite values and physically scaled outputs.
4. End-to-end CSV/Markdown/viewer and static CLI backward-compatibility. Confirm the viewer manually in at least one browser and check node/DOF selector, chart labels, scales and HTML behavior.
5. Check explicit errors for missing/invalid damping, non-finite frequency/force, absent nodes, unsupported releases and models not ready for modal analysis.

**Important:** This delivery cannot be called tested/accepted until the Windows .NET 8 execution and viewer inspection. The prior M57-FIX2 release is likewise awaiting full validation.
