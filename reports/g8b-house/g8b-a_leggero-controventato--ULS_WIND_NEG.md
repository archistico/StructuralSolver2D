# G8B House A&#95;LEGGERO with crossed X-braces

**Description:** Didactic G8B comparative model A&#95;LEGGERO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-a_leggero-controventato.json`
**Analysis id:** `ULS_WIND_NEG`
**Generated UTC:** 2026-10-24 22:04:46

## How to read this report

This report is organized as a learning-oriented structural analysis note: first the assumptions and model data, then the numerical results, then sampled diagrams and checks.

Key conventions:

- `Ux` and `Uy` are global nodal translations.
- `Rz` is the nodal rotation about the out-of-plane Z axis.
- Support `Ux` and `Uy` restraints use the local support axes, rotated by `Orientation [deg]` counterclockwise from the global axes. Reactions are reported in global coordinates.
- `N`, `V` and `M` are local member axial force, shear force and bending moment.
- Member diagram positions are reported both as normalized position `0..1` and distance `x` from the start node.
- Positive and negative signs follow the solver sign convention; for design interpretation, always check the model orientation and support layout.

## Units

StructuralSolver2D uses fixed coherent internal units:

| Quantity | Unit |
|---|---:|
| Length | m |
| Force | kN |
| Moment | kNm |
| Elastic modulus | kN/m² |
| Area | m² |
| Second moment of area | m⁴ |

## Executive summary

### Model size

| Item | Count |
|---|---:|
| Nodes | 15 |
| Members | 22 |
| Supports | 4 |
| Load cases | 11 |
| Load combinations | 4 |
| Loads | 41 |

### Governing absolute values

| Result | Value | Location |
|---|---:|---|
| Max \|Ux\| [m] | 0.001392 | node `A2` |
| Max \|Uy\| [m] | 0.003088 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.002677 | node `D2` |
| Max \|Fx\| [kN] | 40.665125 | support `SUP_A0` |
| Max \|Fy\| [kN] | 277.496671 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 10.477230 | support `SUP_C0` |
| Max \|N\| [kN] | 256.087844 | member `COL_A_1`, x = 0.000000 m |
| Max \|V\| [kN] | 73.998634 | member `F2_CD`, x = 0.000000 m |
| Max \|M\| [kNm] | 61.617555 | member `F1_AB`, x = 0.000000 m |

Load combination: `ULS_WIND_NEG` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 0.700000 × `Q_ROOM` + 0.700000 × `Q_CORR` + 1.000000 × `G_BALC` + 0.700000 × `Q_BALC` + 1.000000 × `G_ROOF` + 0.500000 × `SNOW` + 1.500000 × `WIND_NEG` + 1.500000 × `WIND_UP`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | -0.000000 | 0.000000 |
| `BAL_L1` | -0.000807 | -0.002526 | 0.002341 |
| `A1` | -0.000807 | -0.000467 | -0.000159 |
| `B1` | -0.000703 | -0.000302 | 0.000878 |
| `C1` | -0.000711 | -0.000374 | -0.000602 |
| `D1` | -0.000640 | -0.000387 | 0.000539 |
| `BAL_R1` | -0.000640 | -0.001990 | -0.001962 |
| `A2` | -0.001392 | -0.000674 | -0.002501 |
| `B2` | -0.001295 | -0.000475 | 0.001857 |
| `C2` | -0.001199 | -0.000565 | -0.001740 |
| `D2` | -0.001134 | -0.000563 | 0.002677 |
| `RIDGE` | -0.001363 | -0.003088 | -0.000024 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 40.665125 | 277.496671 | -7.699771 |
| `SUP_B0` | `B0` | 17.641787 | 153.637419 | 1.400182 |
| `SUP_C0` | `C0` | 8.586227 | 205.336922 | -10.477230 |
| `SUP_D0` | `D0` | -0.893138 | 212.208865 | -0.808177 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 256.087844 | -5.555909 | -7.699771 | -254.249060 | 5.555909 | -8.967956 |
| `COL_B_1` | 165.840225 | 3.268385 | 1.400182 | -164.001441 | -3.268385 | 8.404972 |
| `COL_C_1` | 205.336922 | -8.586227 | -10.477230 | -203.498138 | 8.586227 | -15.281449 |
| `COL_D_1` | 212.208865 | 0.893138 | -0.808177 | -210.370081 | -0.893138 | 3.487591 |
| `COL_A_2` | 113.953687 | -24.320504 | -27.144631 | -112.114903 | 24.320504 | -45.816880 |
| `COL_B_2` | 95.425038 | 18.663643 | 24.095039 | -93.586254 | -18.663643 | 31.895892 |
| `COL_C_2` | 105.197342 | -21.276199 | -27.377372 | -103.358558 | 21.276199 | -36.451223 |
| `COL_D_2` | 97.558633 | 23.018024 | 26.000760 | -95.719849 | -23.018024 | 43.053311 |
| `F1_AB` | -23.432955 | 72.807274 | 61.617555 | 23.432955 | 67.304376 | -47.860308 |
| `F1_BC` | 2.565086 | 32.754823 | 15.360297 | -2.565086 | 28.492313 | -11.097788 |
| `F1_CD` | -16.124886 | 69.808483 | 53.756610 | 16.124886 | 70.303167 | -54.993321 |
| `BAL_L` | 0.000000 | 0.000000 | 0.000000 | 0.000000 | 42.508282 | -25.504969 |
| `BAL_R` | 0.000000 | 42.508282 | 25.504969 | 0.000000 | 0.000000 | 0.000000 |
| `F2_AB` | -21.949354 | 67.008787 | 35.470569 | 21.949354 | 73.102863 | -50.705759 |
| `F2_BC` | -28.534466 | 31.887212 | 18.809868 | 28.534466 | 29.359924 | -16.282579 |
| `F2_CD` | -14.758268 | 73.998634 | 52.733803 | 14.758268 | 66.113016 | -33.019757 |
| `RAFTER_L` | 64.980958 | 13.819339 | 10.346311 | -54.977758 | 16.190261 | -17.843827 |
| `RAFTER_R` | 42.312164 | 16.239713 | 17.843827 | -52.315364 | 13.769887 | -10.033553 |
| `BR_A0_B1` | 40.944030 | 0.000000 | 0.000000 | -40.944030 | 0.000000 | 0.000000 |
| `BR_B0_A1` | -24.385240 | 0.000000 | 0.000000 | 24.385240 | 0.000000 | 0.000000 |
| `BR_A1_B2` | 22.832295 | 0.000000 | 0.000000 | -22.832295 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -21.582025 | 0.000000 | 0.000000 | 21.582025 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

