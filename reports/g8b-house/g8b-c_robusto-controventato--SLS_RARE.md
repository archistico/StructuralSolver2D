# G8B House C&#95;ROBUSTO with crossed X-braces

**Description:** Didactic G8B comparative model C&#95;ROBUSTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-c_robusto-controventato.json`
**Analysis id:** `SLS_RARE`
**Generated UTC:** 2026-10-24 22:04:48

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
| Max \|Ux\| [m] | 0.000540 | node `D2` |
| Max \|Uy\| [m] | 0.004628 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.001783 | node `A2` |
| Max \|Fx\| [kN] | 11.772550 | support `SUP_B0` |
| Max \|Fy\| [kN] | 279.459009 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 6.315466 | support `SUP_B0` |
| Max \|N\| [kN] | 277.545208 | member `COL_D_1`, x = 0.000000 m |
| Max \|V\| [kN] | 82.605686 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 67.603054 | member `F1_CD`, x = 5.000000 m |

Load combination: `SLS_RARE` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 1.000000 × `Q_ROOM` + 1.000000 × `Q_CORR` + 1.000000 × `G_BALC` + 1.000000 × `Q_BALC` + 1.000000 × `G_ROOF` + 1.000000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | -0.000000 | -0.000000 |
| `BAL_L1` | -0.000081 | -0.001550 | 0.001339 |
| `A1` | -0.000081 | -0.000369 | -0.000082 |
| `B1` | 0.000026 | -0.000303 | 0.000374 |
| `C1` | 0.000061 | -0.000308 | -0.000407 |
| `D1` | 0.000158 | -0.000372 | 0.000041 |
| `BAL_R1` | 0.000158 | -0.001601 | -0.001379 |
| `A2` | -0.000237 | -0.000570 | -0.001783 |
| `B2` | 0.000020 | -0.000463 | 0.001072 |
| `C2` | 0.000272 | -0.000467 | -0.001099 |
| `D2` | 0.000540 | -0.000569 | 0.001744 |
| `RIDGE` | 0.000151 | -0.004628 | 0.000010 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 8.305248 | 279.459009 | -2.562461 |
| `SUP_B0` | `B0` | -11.772550 | 229.477795 | 6.315466 |
| `SUP_C0` | `C0` | 5.777173 | 229.912228 | -5.458717 |
| `SUP_D0` | `D0` | -2.309871 | 277.545208 | 3.141765 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 275.415171 | -2.137680 | -2.562461 | -272.918871 | 2.137680 | -3.850580 |
| `COL_B_1` | 225.777353 | 6.177310 | 6.315466 | -223.281053 | -6.177310 | 12.216464 |
| `COL_C_1` | 229.912228 | -5.777173 | -5.458717 | -227.415928 | 5.777173 | -11.872802 |
| `COL_D_1` | 277.545208 | 2.309871 | 3.141765 | -275.048908 | -2.309871 | 3.787847 |
| `COL_A_2` | 149.686111 | -31.040483 | -33.151141 | -147.189811 | 31.040483 | -59.970309 |
| `COL_B_2` | 120.538635 | 22.738033 | 28.608409 | -118.042335 | -22.738033 | 39.605690 |
| `COL_C_2` | 119.141208 | -21.518075 | -26.822212 | -116.644908 | 21.518075 | -37.732014 |
| `COL_D_2` | 147.463837 | 32.144912 | 34.794100 | -144.967537 | -32.144912 | 61.640635 |
| `F1_AB` | -32.711328 | 78.606828 | 66.022827 | 32.711328 | 74.246647 | -55.122373 |
| `F1_BC` | -14.094138 | 33.976379 | 14.297500 | 14.094138 | 34.637805 | -14.958926 |
| `F1_CD` | -29.835041 | 73.636915 | 53.653940 | 29.835041 | 79.216560 | -67.603054 |
| `BAL_L` | 0.000000 | -0.000000 | -0.000000 | 0.000000 | 48.368510 | -29.021106 |
| `BAL_R` | 0.000000 | 48.368510 | 29.021106 | 0.000000 | 0.000000 | 0.000000 |
| `F2_AB` | -78.728304 | 70.247789 | 28.732504 | 78.728304 | 82.605686 | -59.627244 |
| `F2_BC` | -103.253053 | 34.021323 | 20.021554 | 103.253053 | 34.592861 | -20.593091 |
| `F2_CD` | -81.734977 | 82.052048 | 58.325105 | 81.734977 | 70.801427 | -30.198554 |
| `RAFTER_L` | 131.478566 | 34.315868 | 31.237805 | -108.030566 | 36.028132 | -36.652462 |
| `RAFTER_R` | 108.041332 | 35.995833 | 36.652462 | -131.489332 | 34.348167 | -31.442082 |
| `BR_A0_B1` | 7.192559 | 0.000000 | 0.000000 | -7.192559 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 6.525116 | 0.000000 | 0.000000 | -6.525116 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -2.083651 | 0.000000 | 0.000000 | 2.083651 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -4.794327 | 0.000000 | 0.000000 | 4.794327 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

