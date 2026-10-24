# G8B House B&#95;RIFERIMENTO with crossed X-braces

**Description:** Didactic G8B comparative model B&#95;RIFERIMENTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-b_riferimento-controventato.json`
**Analysis id:** `SLS_RARE`
**Generated UTC:** 2026-10-24 22:04:44

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
| Max \|Ux\| [m] | 0.000641 | node `D2` |
| Max \|Uy\| [m] | 0.005627 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.002475 | node `A2` |
| Max \|Fx\| [kN] | 12.673207 | support `SUP_B0` |
| Max \|Fy\| [kN] | 277.689062 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 6.255952 | support `SUP_B0` |
| Max \|N\| [kN] | 275.844678 | member `COL_D_1`, x = 0.000000 m |
| Max \|V\| [kN] | 82.398477 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 67.128805 | member `F1_CD`, x = 5.000000 m |

Load combination: `SLS_RARE` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 1.000000 × `Q_ROOM` + 1.000000 × `Q_CORR` + 1.000000 × `G_BALC` + 1.000000 × `Q_BALC` + 1.000000 × `G_ROOF` + 1.000000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | -0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | -0.000000 |
| `D0` | 0.000000 | 0.000000 | 0.000000 |
| `BAL_L1` | -0.000091 | -0.002069 | 0.001866 |
| `A1` | -0.000091 | -0.000427 | -0.000126 |
| `B1` | 0.000033 | -0.000350 | 0.000519 |
| `C1` | 0.000073 | -0.000357 | -0.000559 |
| `D1` | 0.000185 | -0.000431 | 0.000075 |
| `BAL_R1` | 0.000185 | -0.002134 | -0.001917 |
| `A2` | -0.000264 | -0.000659 | -0.002475 |
| `B2` | 0.000034 | -0.000537 | 0.001483 |
| `C2` | 0.000330 | -0.000542 | -0.001517 |
| `D2` | 0.000641 | -0.000659 | 0.002425 |
| `RIDGE` | 0.000189 | -0.005627 | 0.000012 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 9.133870 | 277.689062 | -2.460051 |
| `SUP_B0` | `B0` | -12.673207 | 228.561408 | 6.255952 |
| `SUP_C0` | `C0` | 5.787705 | 228.786386 | -5.513438 |
| `SUP_D0` | `D0` | -2.248369 | 275.844678 | 2.946540 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 273.134380 | -2.114895 | -2.460051 | -270.990388 | 2.114895 | -3.884633 |
| `COL_B_1` | 224.291955 | 6.129614 | 6.255952 | -222.147963 | -6.129614 | 12.132890 |
| `COL_C_1` | 228.786386 | -5.787705 | -5.513438 | -226.642394 | 5.787705 | -11.849677 |
| `COL_D_1` | 275.844678 | 2.248369 | 2.946540 | -273.700686 | -2.248369 | 3.798566 |
| `COL_A_2` | 148.952400 | -30.761374 | -32.838591 | -146.808408 | 30.761374 | -59.445530 |
| `COL_B_2` | 120.305912 | 22.683642 | 28.563515 | -118.161920 | -22.683642 | 39.487411 |
| `COL_C_2` | 118.556270 | -21.578007 | -26.943660 | -116.412278 | 21.578007 | -37.790361 |
| `COL_D_2` | 146.460987 | 31.766113 | 34.341633 | -144.316995 | -31.766113 | 60.956707 |
| `F1_AB` | -32.831520 | 78.378334 | 65.711831 | 32.831520 | 74.078716 | -54.962785 |
| `F1_BC` | -13.727442 | 33.969481 | 14.266380 | 13.727442 | 34.554428 | -14.851327 |
| `F1_CD` | -29.517744 | 73.531697 | 53.644664 | 29.517744 | 78.925353 | -67.128805 |
| `BAL_L` | 0.000000 | -0.000000 | -0.000000 | 0.000000 | 48.314345 | -28.988607 |
| `BAL_R` | 0.000000 | 48.314345 | 28.988607 | 0.000000 | -0.000000 | 0.000000 |
| `F2_AB` | -78.480811 | 70.058573 | 28.533565 | 78.480811 | 82.398477 | -59.383327 |
| `F2_BC` | -103.523004 | 34.005015 | 19.895916 | 103.523004 | 34.518894 | -20.409796 |
| `F2_CD` | -81.944998 | 81.893383 | 58.200156 | 81.944998 | 70.563667 | -29.875865 |
| `RAFTER_L` | 131.189781 | 33.983239 | 30.911965 | -107.871381 | 35.971961 | -37.200858 |
| `RAFTER_R` | 107.880282 | 35.945260 | 37.200858 | -131.198682 | 34.009940 | -31.080842 |
| `BR_A0_B1` | 8.185462 | 0.000000 | 0.000000 | -8.185462 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 7.631075 | 0.000000 | 0.000000 | -7.631075 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -2.750520 | 0.000000 | 0.000000 | 2.750520 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -5.211618 | 0.000000 | 0.000000 | 5.211618 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

