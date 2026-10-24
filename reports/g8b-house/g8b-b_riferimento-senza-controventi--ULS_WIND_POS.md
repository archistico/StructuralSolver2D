# G8B House B&#95;RIFERIMENTO without braces

**Description:** Didactic G8B comparative model B&#95;RIFERIMENTO; steel two-storey frame, no braces, explicit self-weight.
**Source:** `examples/mixed/g8b-house/g8b-b_riferimento-senza-controventi.json`
**Analysis id:** `ULS_WIND_POS`
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
| Max \|Ux\| [m] | 0.007136 | node `D2` |
| Max \|Uy\| [m] | 0.002869 | node `BAL_R1` |
| Max \|Rz\| [rad] | 0.002523 | node `BAL_R1` |
| Max \|Fx\| [kN] | 23.664424 | support `SUP_B0` |
| Max \|Fy\| [kN] | 235.937973 | support `SUP_D0` |
| Max \|Mz\| [kNm] | 36.932444 | support `SUP_B0` |
| Max \|N\| [kN] | 235.937973 | member `COL_D_1`, x = 0.000000 m |
| Max \|V\| [kN] | 82.421667 | member `F1_CD`, x = 5.000000 m |
| Max \|M\| [kNm] | 86.367737 | member `F1_CD`, x = 5.000000 m |

Load combination: `ULS_WIND_POS` = 1.000000 × `G_SELF` + 1.000000 × `G_FLOOR` + 0.700000 × `Q_ROOM` + 0.700000 × `Q_CORR` + 1.000000 × `G_BALC` + 0.700000 × `Q_BALC` + 1.000000 × `G_ROOF` + 0.500000 × `SNOW` + 1.500000 × `WIND_POS` + 1.500000 × `WIND_UP`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | -0.000000 |
| `C0` | 0.000000 | 0.000000 | -0.000000 |
| `D0` | 0.000000 | 0.000000 | 0.000000 |
| `BAL_L1` | 0.003434 | -0.000398 | 0.000511 |
| `A1` | 0.003434 | -0.000311 | -0.001243 |
| `B1` | 0.003514 | -0.000317 | -0.000254 |
| `C1` | 0.003533 | -0.000332 | -0.001302 |
| `D1` | 0.003610 | -0.000369 | -0.000768 |
| `BAL_R1` | 0.003610 | -0.002869 | -0.002523 |
| `A2` | 0.006788 | -0.000456 | -0.002339 |
| `B2` | 0.006876 | -0.000480 | 0.000846 |
| `C2` | 0.007012 | -0.000499 | -0.001687 |
| `D2` | 0.007136 | -0.000538 | 0.001307 |
| `RIDGE` | 0.007040 | -0.002660 | 0.000243 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | -11.847396 | 199.344489 | 24.812479 |
| `SUP_B0` | `B0` | -23.664424 | 203.106019 | 36.932444 |
| `SUP_C0` | `C0` | -11.930903 | 212.342545 | 25.270744 |
| `SUP_D0` | `D0` | -18.557276 | 235.937973 | 32.186904 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 199.344489 | 11.847396 | 24.812479 | -197.200497 | -11.847396 | 10.729710 |
| `COL_B_1` | 203.106019 | 23.664424 | 36.932444 | -200.962027 | -23.664424 | 34.060827 |
| `COL_C_1` | 212.342545 | 11.930903 | 25.270744 | -210.198553 | -11.930903 | 10.521966 |
| `COL_D_1` | 235.937973 | 18.557276 | 32.186904 | -233.793981 | -18.557276 | 23.484925 |
| `COL_A_2` | 93.282515 | -15.247049 | -16.665986 | -91.138523 | 15.247049 | -29.075161 |
| `COL_B_2` | 104.662900 | 32.094154 | 41.916223 | -102.518908 | -32.094154 | 54.366238 |
| `COL_C_2` | 108.044975 | -7.583077 | -9.195239 | -105.900983 | 7.583077 | -13.553992 |
| `COL_D_2` | 108.817968 | 32.735972 | 37.350205 | -106.673976 | -32.735972 | 60.857712 |
| `F1_AB` | -21.094445 | 61.363637 | 31.468883 | 21.094445 | 79.093413 | -75.793323 |
| `F1_BC` | -6.664715 | 17.205714 | -0.183727 | 6.664715 | 44.118195 | -26.728754 |
| `F1_CD` | -20.178696 | 58.035383 | 25.402027 | 20.178696 | 82.421667 | -86.367737 |
| `BAL_L` | 0.000000 | 0.000000 | -0.000000 | 0.000000 | 42.554345 | -25.532607 |
| `BAL_R` | 0.000000 | 42.554345 | 25.532607 | 0.000000 | 0.000000 | 0.000000 |
| `F2_AB` | -23.173297 | 61.427397 | 20.779464 | 23.173297 | 79.029653 | -64.785102 |
| `F2_BC` | -47.767451 | 23.489255 | 10.418864 | 47.767451 | 37.834654 | -24.764262 |
| `F2_CD` | -32.684373 | 68.066329 | 38.318255 | 32.684373 | 72.390721 | -49.129233 |
| `RAFTER_L` | 52.959348 | 13.665160 | 8.295697 | -42.840948 | 16.690040 | -17.861209 |
| `RAFTER_R` | 55.670982 | 16.147270 | 17.861209 | -65.789382 | 14.207930 | -11.728480 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

