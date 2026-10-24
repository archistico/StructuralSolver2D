# G8B House A&#95;LEGGERO with crossed X-braces

**Description:** Didactic G8B comparative model A&#95;LEGGERO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-a_leggero-controventato.json`
**Analysis id:** `SLS_RARE`
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
| Max \|Ux\| [m] | 0.000759 | node `D2` |
| Max \|Uy\| [m] | 0.006969 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.003515 | node `A2` |
| Max \|Fx\| [kN] | 13.701385 | support `SUP_B0` |
| Max \|Fy\| [kN] | 276.064356 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 6.188609 | support `SUP_B0` |
| Max \|N\| [kN] | 274.292674 | member `COL_D_1`, x = 0.000000 m |
| Max \|V\| [kN] | 82.259367 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 66.694387 | member `F1_CD`, x = 5.000000 m |

Load combination: `SLS_RARE` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 1.000000 × `Q_ROOM` + 1.000000 × `Q_CORR` + 1.000000 × `G_BALC` + 1.000000 × `Q_BALC` + 1.000000 × `G_ROOF` + 1.000000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | -0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | 0.000000 | 0.000000 |
| `BAL_L1` | -0.000104 | -0.002819 | 0.002647 |
| `A1` | -0.000104 | -0.000494 | -0.000192 |
| `B1` | 0.000042 | -0.000406 | 0.000734 |
| `C1` | 0.000087 | -0.000415 | -0.000784 |
| `D1` | 0.000216 | -0.000500 | 0.000130 |
| `BAL_R1` | 0.000216 | -0.002899 | -0.002709 |
| `A2` | -0.000295 | -0.000764 | -0.003515 |
| `B2` | 0.000050 | -0.000625 | 0.002097 |
| `C2` | 0.000396 | -0.000630 | -0.002138 |
| `D2` | 0.000759 | -0.000765 | 0.003452 |
| `RIDGE` | 0.000232 | -0.006969 | 0.000016 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 10.101126 | 276.064356 | -2.364670 |
| `SUP_B0` | `B0` | -13.701385 | 227.869202 | 6.188609 |
| `SUP_C0` | `C0` | 5.789108 | 227.857776 | -5.558462 |
| `SUP_D0` | `D0` | -2.188849 | 274.292674 | 2.763716 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 270.912602 | -2.087032 | -2.364670 | -269.073818 | 2.087032 | -3.896425 |
| `COL_B_1` | 222.951275 | 6.077002 | 6.188609 | -221.112491 | -6.077002 | 12.042397 |
| `COL_C_1` | 227.857776 | -5.789108 | -5.558462 | -226.018992 | 5.789108 | -11.808863 |
| `COL_D_1` | 274.292674 | 2.188849 | 2.763716 | -272.453890 | -2.188849 | 3.802832 |
| `COL_A_2` | 148.314898 | -30.571061 | -32.608897 | -146.476114 | 30.571061 | -59.104286 |
| `COL_B_2` | 120.255565 | 22.620298 | 28.495193 | -118.416781 | -22.620298 | 39.365702 |
| `COL_C_2` | 118.099191 | -21.658279 | -27.086226 | -116.260407 | 21.658279 | -37.888610 |
| `COL_D_2` | 145.520729 | 31.449959 | 33.930585 | -143.681945 | -31.449959 | 60.419292 |
| `F1_AB` | -33.088626 | 78.190545 | 65.466291 | 33.088626 | 73.921105 | -54.792688 |
| `F1_BC` | -13.391939 | 33.974106 | 14.255098 | 13.391939 | 34.473030 | -14.754023 |
| `F1_CD` | -29.261110 | 73.446770 | 53.649112 | 29.261110 | 78.664880 | -66.694387 |
| `BAL_L` | 0.000000 | -0.000000 | -0.000000 | 0.000000 | 48.268282 | -28.960969 |
| `BAL_R` | 0.000000 | 48.268282 | 28.960969 | 0.000000 | 0.000000 | 0.000000 |
| `F2_AB` | -77.903378 | 69.852283 | 28.175965 | 77.903378 | 82.259367 | -59.193677 |
| `F2_BC` | -103.543463 | 34.002245 | 19.827975 | 103.543463 | 34.444891 | -20.270621 |
| `F2_CD` | -81.885184 | 81.815516 | 58.159231 | 81.885184 | 70.296134 | -29.360776 |
| `RAFTER_L` | 130.718926 | 33.759588 | 30.928321 | -107.515726 | 35.850012 | -37.538821 |
| `RAFTER_R` | 107.522588 | 35.829426 | 37.538821 | -130.725788 | 33.780174 | -31.058516 |
| `BR_A0_B1` | 9.345960 | 0.000000 | 0.000000 | -9.345960 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 8.891482 | 0.000000 | 0.000000 | -8.891482 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -3.521646 | 0.000000 | 0.000000 | 3.521646 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -5.668506 | 0.000000 | 0.000000 | 5.668506 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

