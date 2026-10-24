# G8B House C&#95;ROBUSTO with crossed X-braces

**Description:** Didactic G8B comparative model C&#95;ROBUSTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-c_robusto-controventato.json`
**Analysis id:** `ULS_WIND_NEG`
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
| Max \|Ux\| [m] | 0.001207 | node `A2` |
| Max \|Uy\| [m] | 0.002109 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.001384 | node `D2` |
| Max \|Fx\| [kN] | 37.256937 | support `SUP_A0` |
| Max \|Fy\| [kN] | 275.822663 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 13.777232 | support `SUP_C0` |
| Max \|N\| [kN] | 257.521857 | member `COL_A_1`, x = 0.000000 m |
| Max \|V\| [kN] | 74.681606 | member `F2_CD`, x = 0.000000 m |
| Max \|M\| [kNm] | 64.437972 | member `F1_AB`, x = 0.000000 m |

Load combination: `ULS_WIND_NEG` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 0.700000 × `Q_ROOM` + 0.700000 × `Q_CORR` + 1.000000 × `G_BALC` + 0.700000 × `Q_BALC` + 1.000000 × `G_ROOF` + 0.500000 × `SNOW` + 1.500000 × `WIND_NEG` + 1.500000 × `WIND_UP`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | -0.000000 |
| `D0` | 0.000000 | 0.000000 | 0.000000 |
| `BAL_L1` | -0.000688 | -0.001464 | 0.001245 |
| `A1` | -0.000688 | -0.000345 | -0.000006 |
| `B1` | -0.000614 | -0.000232 | 0.000501 |
| `C1` | -0.000616 | -0.000277 | -0.000258 |
| `D1` | -0.000560 | -0.000287 | 0.000337 |
| `BAL_R1` | -0.000560 | -0.001008 | -0.000914 |
| `A2` | -0.001207 | -0.000499 | -0.001234 |
| `B2` | -0.001128 | -0.000363 | 0.000971 |
| `C2` | -0.001051 | -0.000418 | -0.000865 |
| `D2` | -0.000996 | -0.000418 | 0.001384 |
| `RIDGE` | -0.001166 | -0.002109 | -0.000024 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 37.256937 | 275.822663 | -10.940988 |
| `SUP_B0` | `B0` | 17.632881 | 162.603353 | -1.777033 |
| `SUP_C0` | `C0` | 10.542739 | 206.690951 | -13.777232 |
| `SUP_D0` | `D0` | 0.567444 | 213.873143 | -3.507552 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 257.521857 | -7.327756 | -10.940988 | -255.025557 | 7.327756 | -11.042282 |
| `COL_B_1` | 173.709407 | 1.449372 | -1.777033 | -171.213107 | -1.449372 | 6.125150 |
| `COL_C_1` | 206.690951 | -10.542739 | -13.777232 | -204.194651 | 10.542739 | -17.850984 |
| `COL_D_1` | 213.873143 | -0.567444 | -3.507552 | -211.376843 | 0.567444 | 1.805220 |
| `COL_A_2` | 115.435642 | -25.002183 | -27.830584 | -112.939342 | 25.002183 | -47.175965 |
| `COL_B_2` | 97.822972 | 17.809842 | 23.013417 | -95.326672 | -17.809842 | 30.416109 |
| `COL_C_2` | 105.906909 | -22.281219 | -28.641785 | -103.410609 | 22.281219 | -38.201873 |
| `COL_D_2` | 99.017855 | 22.538799 | 25.559087 | -96.521555 | -22.538799 | 42.057309 |
| `F1_AB` | -22.477787 | 74.114090 | 64.437972 | 22.477787 | 66.739385 | -46.001208 |
| `F1_BC` | 0.632238 | 34.229439 | 16.862641 | -0.632238 | 27.184745 | -9.817946 |
| `F1_CD` | -17.106243 | 71.102998 | 56.310715 | 17.106243 | 69.750477 | -52.929414 |
| `BAL_L` | 0.000000 | -0.000000 | -0.000000 | 0.000000 | 42.608510 | -25.565106 |
| `BAL_R` | 0.000000 | 42.608510 | 25.565106 | 0.000000 | 0.000000 | 0.000000 |
| `F2_AB` | -24.133749 | 67.823913 | 37.117738 | 24.133749 | 73.029562 | -50.131859 |
| `F2_BC` | -31.557977 | 32.685181 | 19.715750 | 31.557977 | 28.729003 | -15.759572 |
| `F2_CD` | -16.776758 | 74.681606 | 53.961445 | 16.776758 | 66.171869 | -32.687102 |
| `RAFTER_L` | 66.695923 | 14.096647 | 10.058226 | -56.447923 | 16.647353 | -18.124265 |
| `RAFTER_R` | 43.762550 | 16.756138 | 18.124265 | -54.010550 | 13.987862 | -9.370207 |
| `BR_A0_B1` | 34.903122 | 0.000000 | 0.000000 | -34.903122 | 0.000000 | 0.000000 |
| `BR_B0_A1` | -22.253540 | 0.000000 | 0.000000 | 22.253540 | 0.000000 | 0.000000 |
| `BR_A1_B2` | 20.858031 | 0.000000 | 0.000000 | -20.858031 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -20.034713 | 0.000000 | 0.000000 | 20.034713 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

