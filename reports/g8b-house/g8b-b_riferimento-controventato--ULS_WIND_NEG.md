# G8B House B&#95;RIFERIMENTO with crossed X-braces

**Description:** Didactic G8B comparative model B&#95;RIFERIMENTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-b_riferimento-controventato.json`
**Analysis id:** `ULS_WIND_NEG`
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
| Max \|Ux\| [m] | 0.001302 | node `A2` |
| Max \|Uy\| [m] | 0.002528 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.001901 | node `D2` |
| Max \|Fx\| [kN] | 38.968412 | support `SUP_A0` |
| Max \|Fy\| [kN] | 276.747467 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 12.019860 | support `SUP_C0` |
| Max \|N\| [kN] | 256.855629 | member `COL_A_1`, x = 0.000000 m |
| Max \|V\| [kN] | 74.291543 | member `F2_CD`, x = 0.000000 m |
| Max \|M\| [kNm] | 62.907023 | member `F1_AB`, x = 0.000000 m |

Load combination: `ULS_WIND_NEG` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 0.700000 × `Q_ROOM` + 0.700000 × `Q_CORR` + 1.000000 × `G_BALC` + 0.700000 × `Q_BALC` + 1.000000 × `G_ROOF` + 0.500000 × `SNOW` + 1.500000 × `WIND_NEG` + 1.500000 × `WIND_UP`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | -0.000000 |
| `D0` | 0.000000 | 0.000000 | -0.000000 |
| `BAL_L1` | -0.000749 | -0.001903 | 0.001690 |
| `A1` | -0.000749 | -0.000401 | -0.000065 |
| `B1` | -0.000662 | -0.000264 | 0.000655 |
| `C1` | -0.000666 | -0.000321 | -0.000395 |
| `D1` | -0.000603 | -0.000333 | 0.000421 |
| `BAL_R1` | -0.000603 | -0.001406 | -0.001333 |
| `A2` | -0.001302 | -0.000580 | -0.001738 |
| `B2` | -0.001214 | -0.000414 | 0.001327 |
| `C2` | -0.001129 | -0.000485 | -0.001216 |
| `D2` | -0.001068 | -0.000485 | 0.001901 |
| `RIDGE` | -0.001265 | -0.002528 | -0.000024 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 38.968412 | 276.747467 | -9.213836 |
| `SUP_B0` | `B0` | 17.740858 | 157.773298 | -0.071444 |
| `SUP_C0` | `C0` | 9.503879 | 205.944819 | -12.019860 |
| `SUP_D0` | `D0` | -0.213150 | 213.011820 | -2.064821 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 256.855629 | -6.387511 | -9.213836 | -254.711637 | 6.387511 | -9.948697 |
| `COL_B_1` | 169.530529 | 2.426690 | -0.071444 | -167.386537 | -2.426690 | 7.351513 |
| `COL_C_1` | 205.944819 | -9.503879 | -12.019860 | -203.800827 | 9.503879 | -16.491778 |
| `COL_D_1` | 213.011820 | 0.213150 | -2.064821 | -210.867828 | -0.213150 | 2.704271 |
| `COL_A_2` | 114.679907 | -24.600676 | -27.425718 | -112.535915 | 24.600676 | -46.376311 |
| `COL_B_2` | 96.517884 | 18.281955 | 23.617856 | -94.373892 | -18.281955 | 31.228009 |
| `COL_C_2` | 105.502171 | -21.733308 | -27.950666 | -103.358179 | 21.733308 | -37.249259 |
| `COL_D_2` | 98.266898 | 22.786447 | 25.799164 | -96.122906 | -22.786447 | 42.560178 |
| `F1_AB` | -22.852904 | 73.405890 | 62.907023 | 22.852904 | 67.051160 | -47.020198 |
| `F1_BC` | 1.656131 | 33.435719 | 16.050830 | -1.656131 | 27.888190 | -10.503302 |
| `F1_CD` | -16.573298 | 70.410466 | 54.945746 | 16.573298 | 70.046584 | -54.036042 |
| `BAL_L` | 0.000000 | 0.000000 | -0.000000 | 0.000000 | 42.554345 | -25.532607 |
| `BAL_R` | 0.000000 | 42.554345 | 25.532607 | 0.000000 | -0.000000 | -0.000000 |
| `F2_AB` | -23.070446 | 67.399357 | 36.299294 | 23.070446 | 73.057693 | -50.445133 |
| `F2_BC` | -30.045115 | 32.257273 | 19.217124 | 30.045115 | 29.066636 | -16.026486 |
| `F2_CD` | -15.811807 | 74.291543 | 53.275745 | 15.811807 | 66.165507 | -32.960654 |
| `RAFTER_L` | 65.880282 | 13.918035 | 10.077016 | -55.761882 | 16.437165 | -18.043207 |
| `RAFTER_R` | 43.087605 | 16.512664 | 18.043207 | -53.206005 | 13.842536 | -9.599524 |
| `BR_A0_B1` | 37.995533 | 0.000000 | 0.000000 | -37.995533 | 0.000000 | 0.000000 |
| `BR_B0_A1` | -23.519200 | 0.000000 | 0.000000 | 23.519200 | 0.000000 | 0.000000 |
| `BR_A1_B2` | 21.932876 | 0.000000 | 0.000000 | -21.932876 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -20.906448 | 0.000000 | 0.000000 | 20.906448 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

