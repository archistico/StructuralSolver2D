# G8B House C&#95;ROBUSTO with crossed X-braces

**Description:** Didactic G8B comparative model C&#95;ROBUSTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-c_robusto-controventato.json`
**Analysis id:** `ULS_WIND_POS`
**Generated UTC:** 2026-10-24 22:04:47

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
| Max \|Ux\| [m] | 0.001445 | node `D2` |
| Max \|Uy\| [m] | 0.002315 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.001409 | node `A2` |
| Max \|Fx\| [kN] | 39.375219 | support `SUP_B0` |
| Max \|Fy\| [kN] | 256.100591 | support `SUP_B0` |
| Max \|Mz\| [kNm] | 14.330869 | support `SUP_B0` |
| Max \|N\| [kN] | 238.620388 | member `COL_B_1`, x = 0.000000 m |
| Max \|V\| [kN] | 74.794990 | member `F1_CD`, x = 5.000000 m |
| Max \|M\| [kNm] | 66.237367 | member `F1_CD`, x = 5.000000 m |

Load combination: `ULS_WIND_POS` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 0.700000 × `Q_ROOM` + 0.700000 × `Q_CORR` + 1.000000 × `G_BALC` + 0.700000 × `Q_BALC` + 1.000000 × `G_ROOF` + 0.500000 × `SNOW` + 1.500000 × `WIND_POS` + 1.500000 × `WIND_UP`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | -0.000000 | 0.000000 |
| `BAL_L1` | 0.000576 | -0.000947 | 0.000903 |
| `A1` | 0.000576 | -0.000238 | -0.000348 |
| `B1` | 0.000669 | -0.000320 | 0.000240 |
| `C1` | 0.000703 | -0.000283 | -0.000517 |
| `D1` | 0.000782 | -0.000302 | -0.000023 |
| `BAL_R1` | 0.000782 | -0.001456 | -0.001275 |
| `A2` | 0.001052 | -0.000357 | -0.001409 |
| `B2` | 0.001158 | -0.000475 | 0.000849 |
| `C2` | 0.001315 | -0.000426 | -0.000973 |
| `D2` | 0.001445 | -0.000442 | 0.001228 |
| `RIDGE` | 0.001314 | -0.002315 | 0.000031 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | -19.536899 | 166.716222 | 3.602085 |
| `SUP_B0` | `B0` | -39.375219 | 256.100591 | 14.330869 |
| `SUP_C0` | `C0` | 0.760804 | 211.015573 | 2.933087 |
| `SUP_D0` | `D0` | -7.848687 | 225.157724 | 11.957636 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 177.750799 | 0.573775 | 3.602085 | -175.254499 | -0.573775 | -1.880761 |
| `COL_B_1` | 238.620388 | 10.813711 | 14.330869 | -236.124088 | -10.813711 | 18.110263 |
| `COL_C_1` | 211.015573 | -0.760804 | 2.933087 | -208.519273 | 0.760804 | -5.215500 |
| `COL_D_1` | 225.157724 | 7.848687 | 11.957636 | -222.661424 | -7.848687 | 11.588424 |
| `COL_A_2` | 89.744817 | -22.701292 | -25.685518 | -87.248517 | 22.701292 | -42.418359 |
| `COL_B_2` | 116.514804 | 22.287686 | 28.632435 | -114.018504 | -22.287686 | 38.230623 |
| `COL_C_2` | 107.899632 | -17.063859 | -21.998676 | -105.403332 | 17.063859 | -29.192901 |
| `COL_D_2` | 105.257924 | 25.967030 | 29.083837 | -102.761624 | -25.967030 | 48.817252 |
| `F1_AB` | -28.406186 | 69.809715 | 53.131385 | 28.406186 | 71.043760 | -56.216495 |
| `F1_BC` | -13.815288 | 26.853028 | 9.473798 | 13.815288 | 34.561156 | -17.181926 |
| `F1_CD` | -24.118343 | 66.058485 | 44.396102 | 24.118343 | 74.794990 | -66.237367 |
| `BAL_L` | 0.000000 | -0.000000 | -0.000000 | 0.000000 | 42.608510 | -25.565106 |
| `BAL_R` | 0.000000 | 42.608510 | 25.565106 | 0.000000 | -0.000000 | 0.000000 |
| `F2_AB` | -32.284091 | 66.216766 | 32.735001 | 32.284091 | 74.636709 | -53.784859 |
| `F2_BC` | -64.502166 | 28.580265 | 15.554236 | 64.502166 | 32.833919 | -19.807891 |
| `F2_CD` | -39.938307 | 72.569413 | 49.000793 | 39.938307 | 68.284062 | -38.287416 |
| `RAFTER_L` | 53.617205 | 14.105057 | 9.683357 | -43.369205 | 16.638943 | -17.696205 |
| `RAFTER_R` | 56.062929 | 16.505102 | 17.696205 | -66.310929 | 14.238898 | -10.529837 |
| `BR_A0_B1` | -22.114613 | 0.000000 | 0.000000 | 22.114613 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 33.308156 | 0.000000 | 0.000000 | -33.308156 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -20.327152 | 0.000000 | 0.000000 | 20.327152 | 0.000000 | 0.000000 |
| `BR_B1_A2` | 18.752395 | 0.000000 | 0.000000 | -18.752395 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

