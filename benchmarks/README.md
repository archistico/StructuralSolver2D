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
    B05-simple-supported-eccentric-point-load.json
    B06-simple-supported-load-combination.json
    B07-continuous-two-span-udl.json
    B08-cantilever-axial-tip-force.json
    B09-propped-cantilever-udl.json
    B10-fixed-fixed-beam-udl.json
    B11-simple-supported-triangular-load.json
    B12-released-simply-supported-beam-udl.json
  trusses/
    T01-symmetric-triangular-truss.json
    T02-horizontal-bar-axial-tension.json
    T03-cantilever-truss-horizontal-load.json
  frames/
    F01-portal-symmetric-gravity.json
    F02-inclined-cantilever-local-y.json
    F03-inclined-cantilever-global-y.json
    F04-portal-lateral-load-validator.json
  mixed/
    M01-braced-portal-inclined-truss.json
    M02-two-storey-braced-frame-validator.json
  edge-cases/
    E01-unstable-free-frame.json
    E02-insufficient-horizontal-restraint.json
    E03-nearly-vertical-frame-member.json
    E04-nearly-horizontal-frame-member.json
    E05-release-mechanism-cantilever.json
    E06-negative-load-combination.json
    E07-zero-load-combination-factor-invalid.json
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
| `E` | Edge-case robustness validator |

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
| B05 | Simply supported beam with eccentric point load | unsymmetric reactions, maximum moment, loaded-node deflection |
| B06 | Simply supported beam with factored load combination | combination arithmetic, reactions, moment, midspan deflection |
| B07 | Continuous two-span beam with uniform load | indeterminate beam reactions, internal support moment, moment continuity |
| B08 | Cantilever frame member with axial tip force | Frame2D axial stiffness, axial displacement, horizontal reaction |
| B09 | Propped cantilever with uniform distributed load | one-degree indeterminate beam, compatibility at prop, fixed-end moment |
| B10 | Fixed-fixed beam with uniform distributed load | fixed-end moments, double rotational restraint, member end-force signs |
| B11 | Simply supported beam with triangular distributed load | linearly varying distributed load, asymmetric reactions |
| B12 | Moment-released simply supported beam with uniform load | end releases, zero member end moments, simply supported bending |

### Trusses

| ID | Description | Main checks |
|---|---|---|
| T01 | Symmetric triangular truss | support reactions, axial forces, horizontal displacement behavior |
| T02 | Horizontal truss bar in axial tension | axial stiffness, tension sign convention, truss section without inertia |
| T03 | Cantilever triangular truss with horizontal top load | method of joints, uplift reaction, zero-force member |

### Frames

| ID | Description | Main checks |
|---|---|---|
| F01 | Single-bay portal frame with symmetric gravity load | symmetry, stable solution, balanced vertical reactions |
| F02 | Inclined cantilever with LocalY uniform load | local-to-global load projection |
| F03 | Inclined cantilever with GlobalY uniform load | global load direction on inclined member |
| F04 | Fixed-base portal frame with lateral nodal load | frame sway, base shear split, beam-column coupling |

### Mixed models

| ID | Description | Main checks |
|---|---|---|
| M01 | Braced portal frame with inclined Truss2D brace | mixed analyzer, global equilibrium, non-zero brace force |
| M02 | Two-storey braced mixed frame validator | larger mixed model, reaction resultants, finite/non-zero response |

### Edge cases

The `edge-cases/` directory contains robustness validators rather than normal closed-form benchmarks.

| ID | Description | Expected behavior |
|---|---|---|
| E01 | Free unsupported frame member | fails with a clear singular/unstable-model diagnostic |
| E02 | Beam with only vertical rollers | fails because the horizontal rigid-body mode is unconstrained |
| E03 | Nearly vertical frame member | solves with finite results and global equilibrium |
| E04 | Nearly horizontal frame member | solves with finite results and global equilibrium |
| E05 | Cantilever with a released fixed-end moment | fails as a release-created mechanism |
| E06 | Combination with a negative factor | solves and applies the signed factor correctly |
| E07 | Combination with a zero factor | is rejected by validation policy |

These files are exercised by `EdgeCaseValidationTests`, not by the main expected-results catalog.

---

## Expected results

Expected values and validation rules are documented in:

```text
benchmarks/expected/expected-results.md
benchmarks/expected/expected-results.json
docs/structural/validation-manual.md
```

The JSON file is used by the automated benchmark runner.
The Markdown file explains formulas and expected values.
The Validation Manual explains the full workflow, validation levels, tolerances, sign conventions and release checklist.

---

## Recommended validation workflow

For now, each benchmark can be run manually from the CLI:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze benchmarks\beams\B01-simple-supported-udl.json
dotnet run --project src\StructuralSolver2D.Cli -- report benchmarks\beams\B01-simple-supported-udl.json reports\B01-simple-supported-udl.md
```

The automated runner reads `expected-results.json`, executes each benchmark model, and compares reactions, displacements, member end forces, axial forces and internal-force extrema against the expected values within declared tolerances.

## Convergence benchmarks

The `convergence/` folder contains mesh-refinement models used to validate how selected results change when the same structure is discretized with more elements.

Current convergence cases:

```text
C01-simple-supported-udl-1-elements.json
C01-simple-supported-udl-2-elements.json
C01-simple-supported-udl-4-elements.json
C01-simple-supported-udl-8-elements.json
C02-point-load-single-element.json
C02-point-load-explicit-node.json
```

These files are not meant to replace the main expected-results catalog. They document convergence behavior and are exercised by dedicated tests in `MeshRefinementConvergenceTests`.


---

## Relationship with examples

Benchmarks are not the same as examples.

- `examples/` contains user-facing models intended for learning and CLI usage.
- `benchmarks/` contains validation and regression models with expected results.

A benchmark should include expected values and should be suitable for automated testing.

An example should be readable and easy to modify, even if it does not include formal expected-result metadata.
