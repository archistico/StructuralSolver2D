# Two-storey steel house with timber pitched roof

**Description:** Design-oriented example: two-storey steel frame with 5 m + 2 m + 5 m bays, 3 m interstorey height, first-floor side balconies of 1.2 m, and timber pitched roof. Tributary depth = 4 m. Includes classical permanent, live, snow and wind actions with manual ULS/SLS combinations.
**Source:** `examples\mixed\two-storey-steel-house-timber-roof.json`
**Analysis id:** `ULS_GRAVITY`
**Generated UTC:** 2026-10-24 21:49:25

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
| Load cases | 10 |
| Load combinations | 4 |
| Loads | 40 |

### Governing absolute values

| Result | Value | Location |
|---|---:|---|
| Max \|Ux\| [m] | 0.000619 | node `A2` |
| Max \|Uy\| [m] | 0.007782 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.003374 | node `D2` |
| Max \|Fx\| [kN] | 8.070985 | support `SUP_C0` |
| Max \|Fy\| [kN] | 374.052587 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 7.986904 | support `SUP_C0` |
| Max \|N\| [kN] | 374.052587 | member `COL_A_1`, x = 0.000000 m |
| Max \|V\| [kN] | 112.496456 | member `F2_CD`, x = 0.000000 m |
| Max \|M\| [kNm] | 91.140928 | member `F1_AB`, x = 0.000000 m |

Load combination: `ULS_GRAVITY` = 1.350000 × `G_FLOOR` + 1.500000 × `Q_ROOM` + 1.500000 × `Q_CORR` + 1.350000 × `G_BALC` + 1.500000 × `Q_BALC` + 1.350000 × `G_ROOF` + 1.500000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | -0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | -0.000000 | 0.000000 |
| `BAL_L1` | -0.000175 | -0.002960 | 0.002675 |
| `A1` | -0.000175 | -0.000587 | -0.000117 |
| `B1` | -0.000022 | -0.000485 | 0.000727 |
| `C1` | 0.000022 | -0.000485 | -0.000727 |
| `D1` | 0.000175 | -0.000587 | 0.000117 |
| `BAL_R1` | 0.000175 | -0.002960 | -0.002675 |
| `A2` | -0.000619 | -0.000899 | -0.003374 |
| `B2` | -0.000180 | -0.000736 | 0.002049 |
| `C2` | 0.000180 | -0.000736 | -0.002049 |
| `D2` | 0.000619 | -0.000899 | 0.003374 |
| `RIDGE` | -0.000000 | -0.007782 | -0.000000 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 2.651614 | 374.052587 | -3.313473 |
| `SUP_B0` | `B0` | -8.070985 | 308.769814 | 7.986904 |
| `SUP_C0` | `C0` | 8.070985 | 308.769814 | -7.986904 |
| `SUP_D0` | `D0` | -2.651614 | 374.052587 | 3.313473 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 374.052587 | -2.651614 | -3.313473 | -374.052587 | 2.651614 | -4.641368 |
| `COL_B_1` | 308.769814 | 8.070985 | 7.986904 | -308.769814 | -8.070985 | 16.226050 |
| `COL_C_1` | 308.769814 | -8.070985 | -7.986904 | -308.769814 | 8.070985 | -16.226050 |
| `COL_D_1` | 374.052587 | 2.651614 | 3.313473 | -374.052587 | -2.651614 | 4.641368 |
| `COL_A_2` | 198.745945 | -42.889009 | -45.891560 | -198.745945 | 42.889009 | -82.775468 |
| `COL_B_2` | 160.196456 | 30.255489 | 37.900068 | -160.196456 | -30.255489 | 52.866398 |
| `COL_C_2` | 160.196456 | -30.255489 | -37.900068 | -160.196456 | 30.255489 | -52.866398 |
| `COL_D_2` | 198.745945 | 42.889009 | 45.891560 | -198.745945 | -42.889009 | 82.775468 |
| `F1_AB` | -40.237396 | 107.626642 | 91.140928 | 40.237396 | 100.873358 | -74.257719 |
| `F1_BC` | -18.052892 | 47.700000 | 20.131601 | 18.052892 | 47.700000 | -20.131601 |
| `F1_CD` | -40.237396 | 100.873358 | 74.257719 | 40.237396 | 107.626642 | -91.140928 |
| `BAL_L` | 0.000000 | 0.000000 | -0.000000 | 0.000000 | 67.680000 | -40.608000 |
| `BAL_R` | 0.000000 | 67.680000 | 40.608000 | 0.000000 | -0.000000 | 0.000000 |
| `F2_AB` | -115.456472 | 96.003544 | 39.452381 | 115.456472 | 112.496456 | -80.684662 |
| `F2_BC` | -145.711961 | 47.700000 | 27.818264 | 145.711961 | 47.700000 | -27.818264 |
| `F2_CD` | -115.456472 | 112.496456 | 80.684662 | 115.456472 | 96.003544 | -39.452381 |
| `RAFTER_L` | 182.709713 | 47.396762 | 43.323087 | -150.219713 | 50.073238 | -51.786846 |
| `RAFTER_R` | 150.219713 | 50.073238 | 51.786846 | -182.709713 | 47.396762 | -43.323087 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

