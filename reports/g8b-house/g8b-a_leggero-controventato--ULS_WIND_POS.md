# G8B House A&#95;LEGGERO with crossed X-braces

**Description:** Didactic G8B comparative model A&#95;LEGGERO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-a_leggero-controventato.json`
**Analysis id:** `ULS_WIND_POS`
**Generated UTC:** 2026-10-24 22:04:45

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
| Max \|Ux\| [m] | 0.001750 | node `D2` |
| Max \|Uy\| [m] | 0.003403 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.002715 | node `A2` |
| Max \|Fx\| [kN] | 42.471335 | support `SUP_B0` |
| Max \|Fy\| [kN] | 261.365410 | support `SUP_B0` |
| Max \|Mz\| [kNm] | 10.920098 | support `SUP_B0` |
| Max \|N\| [kN] | 240.823253 | member `COL_B_1`, x = 0.000000 m |
| Max \|V\| [kN] | 73.956951 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 63.003541 | member `F1_CD`, x = 5.000000 m |

Load combination: `ULS_WIND_POS` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 0.700000 × `Q_ROOM` + 0.700000 × `Q_CORR` + 1.000000 × `G_BALC` + 0.700000 × `Q_BALC` + 1.000000 × `G_ROOF` + 0.500000 × `SNOW` + 1.500000 × `WIND_POS` + 1.500000 × `WIND_UP`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | -0.000000 | 0.000000 | 0.000000 |
| `B0` | -0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | 0.000000 | 0.000000 |
| `BAL_L1` | 0.000661 | -0.001895 | 0.001945 |
| `A1` | 0.000661 | -0.000311 | -0.000555 |
| `B1` | 0.000795 | -0.000439 | 0.000574 |
| `C1` | 0.000847 | -0.000379 | -0.000902 |
| `D1` | 0.000958 | -0.000402 | 0.000115 |
| `BAL_R1` | 0.000958 | -0.002514 | -0.002385 |
| `A2` | 0.001216 | -0.000469 | -0.002715 |
| `B2` | 0.001357 | -0.000652 | 0.001716 |
| `C2` | 0.001575 | -0.000573 | -0.001858 |
| `D2` | 0.001750 | -0.000588 | 0.002493 |
| `RIDGE` | 0.001585 | -0.003403 | 0.000035 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | -20.210751 | 158.591716 | 0.844959 |
| `SUP_B0` | `B0` | -42.471335 | 261.365410 | 10.920098 |
| `SUP_C0` | `C0` | 2.689897 | 208.182743 | -0.438130 |
| `SUP_D0` | `D0` | -6.007811 | 220.540009 | 8.553657 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 170.921922 | -0.911755 | 0.844959 | -169.083138 | 0.911755 | -3.580225 |
| `COL_B_1` | 240.823253 | 8.806570 | 10.920098 | -238.984469 | -8.806570 | 15.499611 |
| `COL_C_1` | 208.182743 | -2.689897 | -0.438130 | -206.343959 | 2.689897 | -7.631560 |
| `COL_D_1` | 220.540009 | 6.007811 | 8.553657 | -218.701225 | -6.007811 | 9.469776 |
| `COL_A_2` | 87.462287 | -23.125275 | -26.074082 | -85.623503 | 23.125275 | -43.301745 |
| `COL_B_2` | 117.140065 | 21.252999 | 27.326830 | -115.301281 | -21.252999 | 36.432168 |
| `COL_C_2` | 106.569914 | -18.136885 | -23.395068 | -104.731130 | 18.136885 | -31.015587 |
| `COL_D_2` | 102.860044 | 25.006613 | 28.028796 | -101.021260 | -25.006613 | 46.991042 |
| `F1_AB` | -30.331718 | 70.352774 | 55.159275 | 30.331718 | 69.758876 | -53.674530 |
| `F1_BC` | -15.551813 | 28.251842 | 10.848089 | 15.551813 | 32.995294 | -15.591542 |
| `F1_CD` | -24.998802 | 66.778751 | 46.618170 | 24.998802 | 73.332899 | -63.003541 |
| `BAL_L` | 0.000000 | 0.000000 | 0.000000 | 0.000000 | 42.508282 | -25.504969 |
| `BAL_R` | 0.000000 | 42.508282 | 25.504969 | 0.000000 | -0.000000 | -0.000000 |
| `F2_AB` | -31.856793 | 66.154699 | 33.074978 | 31.856793 | 73.956951 | -52.580608 |
| `F2_BC` | -65.156360 | 29.273092 | 16.148440 | 65.156360 | 31.974044 | -18.849391 |
| `F2_CD` | -39.519475 | 72.757086 | 49.864979 | 39.519475 | 67.354564 | -36.358672 |
| `RAFTER_L` | 52.075553 | 13.841669 | 10.226766 | -42.072353 | 16.167931 | -17.583056 |
| `RAFTER_R` | 54.742841 | 16.103800 | 17.583056 | -64.746041 | 13.905800 | -10.632371 |
| `BR_A0_B1` | -24.632863 | 0.000000 | 0.000000 | 24.632863 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 39.259525 | 0.000000 | 0.000000 | -39.259525 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -22.795019 | 0.000000 | 0.000000 | 22.795019 | 0.000000 | 0.000000 |
| `BR_B1_A2` | 20.356997 | 0.000000 | 0.000000 | -20.356997 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

