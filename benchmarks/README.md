# StructuralSolver2D Benchmark Catalog

This directory contains benchmark models used to validate the structural solver.

The benchmark catalog has two goals:

1. provide small, transparent, hand-checkable structural examples;
2. prepare a future automated benchmark runner that will compare solver results against documented expected values.

The benchmarks are intentionally simple. They are not meant to replace professional verification suites such as NAFEMS benchmarks, but they follow the same principle: each case should be small enough to understand, documented enough to reproduce, and precise enough to detect regressions.

---

## Directory structure

```text
benchmarks/
  README.md
  beams/
    B01-simple-supported-udl.json
    B02-simple-supported-point-midspan.json
    B03-cantilever-tip-point-load.json
    B04-cantilever-udl.json
  trusses/
    T01-symmetric-triangular-truss.json
  frames/
    F01-portal-symmetric-gravity.json
    F02-inclined-cantilever-local-y.json
    F03-inclined-cantilever-global-y.json
  mixed/
    M01-braced-portal-inclined-truss.json
  expected/
    expected-results.json
    expected-results.md
```

---

## Internal units

All benchmark models use the internal StructuralSolver2D units:

| Quantity | Unit |
|---|---:|
| Length | m |
| Force | kN |
| Moment | kNm |
| Elastic modulus | kN/m² |
| Area | m² |
| Second moment of area | m⁴ |
| Distributed load | kN/m |

---

## Benchmark naming convention

| Prefix | Meaning |
|---|---|
| `B` | Beam benchmark |
| `T` | Truss benchmark |
| `F` | Frame benchmark |
| `M` | Mixed Frame2D + Truss2D benchmark |

Examples:

```text
B01-simple-supported-udl.json
T01-symmetric-triangular-truss.json
F01-portal-symmetric-gravity.json
```

---

## Current benchmark set

### Beams

| ID | Description | Main checks |
|---|---|---|
| B01 | Simply supported beam with uniform distributed load | reactions, shear, bending moment, midspan deflection |
| B02 | Simply supported beam with point load at midspan | reactions, bending moment, midspan deflection |
| B03 | Cantilever with point load at free end | reaction, fixed-end moment, tip deflection, tip rotation |
| B04 | Cantilever with uniform distributed load | reaction, fixed-end moment, tip deflection, tip rotation |

### Trusses

| ID | Description | Main checks |
|---|---|---|
| T01 | Symmetric triangular truss | support reactions, axial forces, horizontal displacement behavior |

### Frames

| ID | Description | Main checks |
|---|---|---|
| F01 | Single-bay portal frame with symmetric gravity load | symmetry, stable solution, balanced vertical reactions |
| F02 | Inclined cantilever with LocalY uniform load | local-to-global load projection |
| F03 | Inclined cantilever with GlobalY uniform load | global load direction on inclined member |

### Mixed models

| ID | Description | Main checks |
|---|---|---|
| M01 | Braced portal frame with inclined Truss2D brace | mixed analyzer, global equilibrium, non-zero brace force |

---

## Expected results

Expected values are documented in:

```text
benchmarks/expected/expected-results.md
benchmarks/expected/expected-results.json
```

The JSON file is used by the automated benchmark runner.
The Markdown file is intended for humans and explains the formulas behind the main expected results.

---

## Recommended validation workflow

For now, each benchmark can be run manually from the CLI:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze benchmarks\beams\B01-simple-supported-udl.json
dotnet run --project src\StructuralSolver2D.Cli -- report benchmarks\beams\B01-simple-supported-udl.json reports\B01-simple-supported-udl.md
```

The automated runner reads `expected-results.json`, executes each benchmark model, and compares reactions, displacements and internal-force extrema against the expected values within declared tolerances.
