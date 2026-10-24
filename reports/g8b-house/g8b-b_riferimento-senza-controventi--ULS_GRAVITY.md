# G8B House B&#95;RIFERIMENTO without braces

**Description:** Didactic G8B comparative model B&#95;RIFERIMENTO; steel two-storey frame, no braces, explicit self-weight.
**Source:** `examples/mixed/g8b-house/g8b-b_riferimento-senza-controventi.json`
**Analysis id:** `ULS_GRAVITY`
**Generated UTC:** 2026-10-24 22:04:41

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
| Members | 18 |
| Supports | 4 |
| Load cases | 11 |
| Load combinations | 4 |
| Loads | 41 |

### Governing absolute values

| Result | Value | Location |
|---|---:|---|
| Max \|Ux\| [m] | 0.000660 | node `A2` |
| Max \|Uy\| [m] | 0.008033 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.003431 | node `D2` |
| Max \|Fx\| [kN] | 8.230754 | support `SUP_C0` |
| Max \|Fy\| [kN] | 385.764586 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 8.130455 | support `SUP_C0` |
| Max \|N\| [kN] | 385.764586 | member `COL_A_1`, x = 0.000000 m |
| Max \|V\| [kN] | 114.265119 | member `F2_CD`, x = 0.000000 m |
| Max \|M\| [kNm] | 92.624864 | member `F1_AB`, x = 0.000000 m |

Load combination: `ULS_GRAVITY` = 1.350000 × `G_SELF` + 1.350000 × `G_FLOOR` + 1.500000 × `Q_ROOM` + 1.500000 × `Q_CORR` + 1.350000 × `G_BALC` + 1.500000 × `Q_BALC` + 1.350000 × `G_ROOF` + 1.500000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | -0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | 0.000000 | 0.000000 |
| `BAL_L1` | -0.000183 | -0.002987 | 0.002688 |
| `A1` | -0.000183 | -0.000603 | -0.000120 |
| `B1` | -0.000027 | -0.000498 | 0.000744 |
| `C1` | 0.000027 | -0.000498 | -0.000744 |
| `D1` | 0.000183 | -0.000603 | 0.000120 |
| `BAL_R1` | 0.000183 | -0.002987 | -0.002688 |
| `A2` | -0.000660 | -0.000923 | -0.003431 |
| `B2` | -0.000212 | -0.000755 | 0.002090 |
| `C2` | 0.000212 | -0.000755 | -0.002090 |
| `D2` | 0.000660 | -0.000923 | 0.003431 |
| `RIDGE` | -0.000000 | -0.008033 | -0.000000 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 2.736207 | 385.764586 | -3.426559 |
| `SUP_B0` | `B0` | -8.230754 | 318.614139 | 8.130455 |
| `SUP_C0` | `C0` | 8.230754 | 318.614139 | -8.130455 |
| `SUP_D0` | `D0` | -2.736207 | 385.764586 | 3.426559 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 385.764586 | -2.736207 | -3.426559 | -382.870197 | 2.736207 | -4.782063 |
| `COL_B_1` | 318.614139 | 8.230754 | 8.130455 | -315.719750 | -8.230754 | 16.561806 |
| `COL_C_1` | 318.614139 | -8.230754 | -8.130455 | -315.719750 | 8.230754 | -16.561806 |
| `COL_D_1` | 385.764586 | 2.736207 | 3.426559 | -382.870197 | -2.736207 | 4.782063 |
| `COL_A_2` | 205.401777 | -43.823336 | -46.980181 | -202.507388 | 43.823336 | -84.489828 |
| `COL_B_2` | 165.213147 | 30.702992 | 38.434546 | -162.318758 | -30.702992 | 53.674431 |
| `COL_C_2` | 165.213147 | -30.702992 | -38.434546 | -162.318758 | 30.702992 | -53.674431 |
| `COL_D_2` | 205.401777 | 43.823336 | 46.980181 | -202.507388 | -43.823336 | 84.489828 |
| `F1_AB` | -41.087129 | 109.364054 | 92.624864 | 41.087129 | 102.452964 | -75.347139 |
| `F1_BC` | -18.614891 | 48.053639 | 20.350787 | 18.614891 | 48.053639 | -20.350787 |
| `F1_CD` | -41.087129 | 102.452964 | 75.347139 | 41.087129 | 109.364054 | -92.624864 |
| `BAL_L` | 0.000000 | -0.000000 | 0.000000 | 0.000000 | 68.104366 | -40.862620 |
| `BAL_R` | 0.000000 | 68.104366 | 40.862620 | 0.000000 | -0.000000 | 0.000000 |
| `F2_AB` | -117.790817 | 97.551898 | 40.074008 | 117.790817 | 114.265119 | -81.857061 |
| `F2_BC` | -148.493809 | 48.053639 | 28.182630 | 148.493809 | 48.053639 | -28.182630 |
| `F2_CD` | -117.790817 | 114.265119 | 81.857061 | 117.790817 | 97.551898 | -40.074008 |
| `RAFTER_L` | 186.510488 | 48.462637 | 44.415820 | -153.320648 | 51.106883 | -52.777659 |
| `RAFTER_R` | 153.320648 | 51.106883 | 52.777659 | -186.510488 | 48.462637 | -44.415820 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

