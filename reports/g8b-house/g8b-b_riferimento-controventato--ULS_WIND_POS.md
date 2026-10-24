# G8B House B&#95;RIFERIMENTO with crossed X-braces

**Description:** Didactic G8B comparative model B&#95;RIFERIMENTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-b_riferimento-controventato.json`
**Analysis id:** `ULS_WIND_POS`
**Generated UTC:** 2026-10-24 22:04:43

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
| Max \|Ux\| [m] | 0.001597 | node `D2` |
| Max \|Uy\| [m] | 0.002784 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.001932 | node `A2` |
| Max \|Fx\| [kN] | 40.926200 | support `SUP_B0` |
| Max \|Fy\| [kN] | 258.884435 | support `SUP_B0` |
| Max \|Mz\| [kNm] | 12.518113 | support `SUP_B0` |
| Max \|N\| [kN] | 239.834709 | member `COL_B_1`, x = 0.000000 m |
| Max \|V\| [kN] | 74.249350 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 64.512192 | member `F1_CD`, x = 5.000000 m |

Load combination: `ULS_WIND_POS` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 0.700000 × `Q_ROOM` + 0.700000 × `Q_CORR` + 1.000000 × `G_BALC` + 0.700000 × `Q_BALC` + 1.000000 × `G_ROOF` + 0.500000 × `SNOW` + 1.500000 × `WIND_POS` + 1.500000 × `WIND_UP`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | -0.000000 |
| `D0` | 0.000000 | 0.000000 | 0.000000 |
| `BAL_L1` | 0.000622 | -0.001329 | 0.001320 |
| `A1` | 0.000622 | -0.000272 | -0.000434 |
| `B1` | 0.000733 | -0.000375 | 0.000372 |
| `C1` | 0.000775 | -0.000327 | -0.000675 |
| `D1` | 0.000869 | -0.000348 | 0.000028 |
| `BAL_R1` | 0.000869 | -0.001893 | -0.001726 |
| `A2` | 0.001137 | -0.000409 | -0.001932 |
| `B2` | 0.001260 | -0.000556 | 0.001196 |
| `C2` | 0.001445 | -0.000494 | -0.001329 |
| `D2` | 0.001597 | -0.000509 | 0.001731 |
| `RIDGE` | 0.001447 | -0.002784 | 0.000033 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | -19.980390 | 162.401134 | 2.124443 |
| `SUP_B0` | `B0` | -40.926200 | 258.884435 | 12.518113 |
| `SUP_C0` | `C0` | 1.787255 | 209.483181 | 1.140396 |
| `SUP_D0` | `D0` | -6.880665 | 222.708653 | 10.160867 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 174.180320 | -0.223749 | 2.124443 | -172.036328 | 0.223749 | -2.795689 |
| `COL_B_1` | 239.834709 | 9.748818 | 12.518113 | -237.690717 | -9.748818 | 16.728341 |
| `COL_C_1` | 209.483181 | -1.787255 | 1.140396 | -207.339189 | 1.787255 | -6.502161 |
| `COL_D_1` | 222.708653 | 6.880665 | 10.160867 | -220.564661 | -6.880665 | 10.481128 |
| `COL_A_2` | 88.530031 | -22.918033 | -25.892595 | -86.386039 | 22.918033 | -42.861505 |
| `COL_B_2` | 116.813682 | 21.727421 | 27.924581 | -114.669690 | -21.727421 | 37.257682 |
| `COL_C_2` | 107.172402 | -17.641363 | -22.755119 | -105.028410 | 17.641363 | -30.168970 |
| `COL_D_2` | 103.996398 | 25.429835 | 28.498457 | -101.852406 | -25.429835 | 47.791049 |
| `F1_AB` | -29.322629 | 70.101209 | 54.220892 | 29.322629 | 70.355841 | -54.857473 |
| `F1_BC` | -14.695062 | 27.600255 | 10.204551 | 14.695062 | 33.723654 | -16.327950 |
| `F1_CD` | -24.549170 | 66.443133 | 45.585231 | 24.549170 | 74.013917 | -64.512192 |
| `BAL_L` | 0.000000 | 0.000000 | -0.000000 | 0.000000 | 42.554345 | -25.532607 |
| `BAL_R` | 0.000000 | 42.554345 | 25.532607 | 0.000000 | 0.000000 | 0.000000 |
| `F2_AB` | -32.202709 | 66.207700 | 33.008637 | 32.202709 | 74.249350 | -53.112763 |
| `F2_BC` | -64.979168 | 28.947620 | 15.855081 | 64.979168 | 32.376289 | -19.283751 |
| `F2_CD` | -39.837804 | 72.652121 | 49.452721 | 39.837804 | 67.804929 | -37.334740 |
| `RAFTER_L` | 52.889038 | 13.937129 | 9.852867 | -42.770638 | 16.418071 | -17.698297 |
| `RAFTER_R` | 55.451553 | 16.322659 | 17.698297 | -65.569953 | 14.032541 | -10.456309 |
| `BR_A0_B1` | -23.561872 | 0.000000 | 0.000000 | 23.561872 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 36.358763 | 0.000000 | 0.000000 | -36.358763 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -21.631709 | 0.000000 | 0.000000 | 21.631709 | 0.000000 | 0.000000 |
| `BR_B1_A2` | 19.653926 | 0.000000 | 0.000000 | -19.653926 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

