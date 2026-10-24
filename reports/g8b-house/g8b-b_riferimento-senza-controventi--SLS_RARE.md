# G8B House B&#95;RIFERIMENTO without braces

**Description:** Didactic G8B comparative model B&#95;RIFERIMENTO; steel two-storey frame, no braces, explicit self-weight.
**Source:** `examples/mixed/g8b-house/g8b-b_riferimento-senza-controventi.json`
**Analysis id:** `SLS_RARE`
**Generated UTC:** 2026-10-24 22:04:42

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
| Max \|Ux\| [m] | 0.000462 | node `A2` |
| Max \|Uy\| [m] | 0.005652 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.002447 | node `D2` |
| Max \|Fx\| [kN] | 5.965251 | support `SUP_C0` |
| Max \|Fy\| [kN] | 275.337374 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 5.894675 | support `SUP_C0` |
| Max \|N\| [kN] | 275.337374 | member `COL_A_1`, x = 0.000000 m |
| Max \|V\| [kN] | 82.123296 | member `F2_CD`, x = 0.000000 m |
| Max \|M\| [kNm] | 66.450281 | member `F1_AB`, x = 0.000000 m |

Load combination: `SLS_RARE` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 1.000000 × `Q_ROOM` + 1.000000 × `Q_CORR` + 1.000000 × `G_BALC` + 1.000000 × `Q_BALC` + 1.000000 × `G_ROOF` + 1.000000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | -0.000000 |
| `C0` | 0.000000 | 0.000000 | -0.000000 |
| `D0` | 0.000000 | -0.000000 | -0.000000 |
| `BAL_L1` | -0.000130 | -0.002104 | 0.001892 |
| `A1` | -0.000130 | -0.000430 | -0.000100 |
| `B1` | -0.000019 | -0.000357 | 0.000539 |
| `C1` | 0.000019 | -0.000357 | -0.000539 |
| `D1` | 0.000130 | -0.000430 | 0.000100 |
| `BAL_R1` | 0.000130 | -0.002104 | -0.001892 |
| `A2` | -0.000462 | -0.000658 | -0.002447 |
| `B2` | -0.000149 | -0.000542 | 0.001500 |
| `C2` | 0.000149 | -0.000542 | -0.001500 |
| `D2` | 0.000462 | -0.000658 | 0.002447 |
| `RIDGE` | -0.000000 | -0.005652 | -0.000000 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 2.109249 | 275.337374 | -2.599769 |
| `SUP_B0` | `B0` | -5.965251 | 228.730204 | 5.894675 |
| `SUP_C0` | `C0` | 5.965251 | 228.730204 | -5.894675 |
| `SUP_D0` | `D0` | -2.109249 | 275.337374 | 2.599769 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 275.337374 | -2.109249 | -2.599769 | -273.193382 | 2.109249 | -3.727979 |
| `COL_B_1` | 228.730204 | 5.965251 | 5.894675 | -226.586212 | -5.965251 | 12.001079 |
| `COL_C_1` | 228.730204 | -5.965251 | -5.894675 | -226.586212 | 5.965251 | -12.001079 |
| `COL_D_1` | 275.337374 | 2.109249 | 2.599769 | -273.193382 | -2.109249 | 3.727979 |
| `COL_A_2` | 146.217002 | -31.351155 | -33.733695 | -144.073010 | 31.351155 | -60.319769 |
| `COL_B_2` | 118.529242 | 22.108580 | 27.722896 | -116.385250 | -22.108580 | 38.602844 |
| `COL_C_2` | 118.529242 | -22.108580 | -27.722896 | -116.385250 | 22.108580 | -38.602844 |
| `COL_D_2` | 146.217002 | 31.351155 | 33.733695 | -144.073010 | -31.351155 | 60.319769 |
| `F1_AB` | -29.241906 | 78.662034 | 66.450281 | 29.241906 | 73.795016 | -54.282734 |
| `F1_BC` | -13.098577 | 34.261955 | 14.558759 | 13.098577 | 34.261954 | -14.558759 |
| `F1_CD` | -29.241906 | 73.795016 | 54.282734 | 29.241906 | 78.662034 | -66.450281 |
| `BAL_L` | 0.000000 | 0.000000 | -0.000000 | 0.000000 | 48.314345 | -28.988607 |
| `BAL_R` | 0.000000 | 48.314345 | 28.988607 | 0.000000 | -0.000000 | -0.000000 |
| `F2_AB` | -82.319090 | 70.333754 | 29.279003 | 82.319090 | 82.123296 | -58.752856 |
| `F2_BC` | -104.427670 | 34.261955 | 20.150012 | 104.427670 | 34.261954 | -20.150012 |
| `F2_CD` | -82.319090 | 82.123296 | 58.752856 | 82.319090 | 70.333754 | -29.279003 |
| `RAFTER_L` | 131.155463 | 34.009512 | 31.040767 | -107.837063 | 35.945688 | -37.163490 |
| `RAFTER_R` | 107.837063 | 35.945688 | 37.163490 | -131.155463 | 34.009512 | -31.040767 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

