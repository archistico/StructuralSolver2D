# G8B House B&#95;RIFERIMENTO with crossed X-braces

**Description:** Didactic G8B comparative model B&#95;RIFERIMENTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-b_riferimento-controventato.json`
**Analysis id:** `ULS_GRAVITY`
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
| Max \|Ux\| [m] | 0.000914 | node `D2` |
| Max \|Uy\| [m] | 0.007996 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.003471 | node `A2` |
| Max \|Fx\| [kN] | 17.603660 | support `SUP_B0` |
| Max \|Fy\| [kN] | 389.002500 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 8.628771 | support `SUP_B0` |
| Max \|N\| [kN] | 386.487395 | member `COL_D_1`, x = 0.000000 m |
| Max \|V\| [kN] | 114.658174 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 93.587223 | member `F1_CD`, x = 5.000000 m |

Load combination: `ULS_GRAVITY` = 1.350000 × `G_SELF` + 1.350000 × `G_FLOOR` + 1.500000 × `Q_ROOM` + 1.500000 × `Q_CORR` + 1.350000 × `G_BALC` + 1.500000 × `Q_BALC` + 1.350000 × `G_ROOF` + 1.500000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | -0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | -0.000000 | 0.000000 |
| `BAL_L1` | -0.000129 | -0.002937 | 0.002651 |
| `A1` | -0.000129 | -0.000598 | -0.000157 |
| `B1` | 0.000046 | -0.000488 | 0.000716 |
| `C1` | 0.000102 | -0.000498 | -0.000773 |
| `D1` | 0.000260 | -0.000604 | 0.000085 |
| `BAL_R1` | 0.000260 | -0.003029 | -0.002723 |
| `A2` | -0.000378 | -0.000924 | -0.003471 |
| `B2` | 0.000048 | -0.000749 | 0.002066 |
| `C2` | 0.000468 | -0.000755 | -0.002115 |
| `D2` | 0.000914 | -0.000925 | 0.003400 |
| `RIDGE` | 0.000268 | -0.007996 | 0.000018 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 12.541130 | 389.002500 | -3.233618 |
| `SUP_B0` | `B0` | -17.603660 | 318.279676 | 8.628771 |
| `SUP_C0` | `C0` | 7.987842 | 318.695489 | -7.603010 |
| `SUP_D0` | `D0` | -2.925312 | 386.487395 | 3.906033 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 382.663232 | -2.748102 | -3.233618 | -379.768843 | 2.748102 | -5.010689 |
| `COL_B_1` | 312.326729 | 8.454499 | 8.628771 | -309.432339 | -8.454499 | 16.734727 |
| `COL_C_1` | 318.695489 | -7.987842 | -7.603010 | -315.801100 | 7.987842 | -16.360515 |
| `COL_D_1` | 386.487395 | 2.925312 | 3.906033 | -383.593006 | -2.925312 | 4.869902 |
| `COL_A_2` | 209.329719 | -42.981331 | -45.701980 | -206.435330 | 42.981331 | -83.242013 |
| `COL_B_2` | 167.764248 | 31.527745 | 39.642053 | -164.869859 | -31.527745 | 54.941183 |
| `COL_C_2` | 165.253022 | -29.941938 | -37.315447 | -162.358633 | 29.941938 | -52.510368 |
| `COL_D_2` | 205.751073 | 44.418839 | 47.854701 | -202.856684 | -44.418839 | 85.401816 |
| `F1_AB` | -45.942954 | 108.961012 | 91.575289 | 45.942954 | 102.856006 | -76.312773 |
| `F1_BC` | -19.539431 | 47.638650 | 19.935993 | 19.539431 | 48.468627 | -20.765971 |
| `F1_CD` | -41.493527 | 102.079451 | 74.441933 | 41.493527 | 109.737567 | -93.587223 |
| `BAL_L` | 0.000000 | 0.000000 | -0.000000 | 0.000000 | 68.104366 | -40.862620 |
| `BAL_R` | 0.000000 | 68.104366 | 40.862620 | 0.000000 | 0.000000 | -0.000000 |
| `F2_AB` | -112.228656 | 97.158843 | 39.010285 | 112.228656 | 114.658174 | -82.758612 |
| `F2_BC` | -147.195836 | 47.684572 | 27.817428 | 147.195836 | 48.422705 | -28.555561 |
| `F2_CD` | -117.253898 | 113.935928 | 81.065929 | 117.253898 | 97.881090 | -40.928835 |
| `RAFTER_L` | 186.559708 | 48.425039 | 44.231728 | -153.369868 | 51.144481 | -52.831360 |
| `RAFTER_R` | 153.382583 | 51.106336 | 52.831360 | -186.572423 | 48.463184 | -44.472982 |
| `BR_A0_B1` | 11.420534 | 0.000000 | 0.000000 | -11.420534 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 10.669663 | 0.000000 | 0.000000 | -10.669663 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -4.011036 | 0.000000 | 0.000000 | 4.011036 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -7.536797 | 0.000000 | 0.000000 | 7.536797 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

