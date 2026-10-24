# G8B House C&#95;ROBUSTO with crossed X-braces

**Description:** Didactic G8B comparative model C&#95;ROBUSTO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-c_robusto-controventato.json`
**Analysis id:** `ULS_GRAVITY`
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
| Max \|Ux\| [m] | 0.000769 | node `D2` |
| Max \|Uy\| [m] | 0.006573 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.002501 | node `A2` |
| Max \|Fx\| [kN] | 16.340346 | support `SUP_B0` |
| Max \|Fy\| [kN] | 391.404819 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 8.710961 | support `SUP_B0` |
| Max \|N\| [kN] | 388.791650 | member `COL_D_1`, x = 0.000000 m |
| Max \|V\| [kN] | 114.939012 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 94.248628 | member `F1_CD`, x = 5.000000 m |

Load combination: `ULS_GRAVITY` = 1.350000 × `G_SELF` + 1.350000 × `G_FLOOR` + 1.500000 × `Q_ROOM` + 1.500000 × `Q_CORR` + 1.350000 × `G_BALC` + 1.500000 × `Q_BALC` + 1.350000 × `G_ROOF` + 1.500000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | -0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | -0.000000 |
| `D0` | 0.000000 | -0.000000 | 0.000000 |
| `BAL_L1` | -0.000114 | -0.002199 | 0.001902 |
| `A1` | -0.000114 | -0.000518 | -0.000100 |
| `B1` | 0.000036 | -0.000421 | 0.000516 |
| `C1` | 0.000085 | -0.000429 | -0.000562 |
| `D1` | 0.000223 | -0.000522 | 0.000042 |
| `BAL_R1` | 0.000223 | -0.002273 | -0.001960 |
| `A2` | -0.000340 | -0.000799 | -0.002501 |
| `B2` | 0.000029 | -0.000646 | 0.001493 |
| `C2` | 0.000386 | -0.000651 | -0.001532 |
| `D2` | 0.000769 | -0.000799 | 0.002445 |
| `RIDGE` | 0.000215 | -0.006573 | 0.000014 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 11.376675 | 391.404819 | -3.373457 |
| `SUP_B0` | `B0` | -16.340346 | 319.499477 | 8.710961 |
| `SUP_C0` | `C0` | 7.971528 | 320.211268 | -7.524094 |
| `SUP_D0` | `D0` | -3.007857 | 388.791650 | 4.177175 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 385.780905 | -2.775904 | -3.373457 | -382.410900 | 2.775904 | -4.954254 |
| `COL_B_1` | 314.343947 | 8.520215 | 8.710961 | -310.973942 | -8.520215 | 16.849683 |
| `COL_C_1` | 320.211268 | -7.971528 | -7.524094 | -316.841263 | 7.971528 | -16.390491 |
| `COL_D_1` | 388.791650 | 3.007857 | 4.177175 | -385.421645 | -3.007857 | 4.846398 |
| `COL_A_2` | 210.295396 | -43.376568 | -46.144845 | -206.925391 | 43.376568 | -83.984859 |
| `COL_B_2` | 168.045601 | 31.601311 | 39.700864 | -164.675596 | -31.601311 | 55.103067 |
| `COL_C_2` | 166.042443 | -29.853541 | -37.138505 | -162.672438 | 29.853541 | -52.422118 |
| `COL_D_2` | 207.107236 | 44.955290 | 48.495737 | -203.737231 | -44.955290 | 86.370134 |
| `F1_AB` | -45.806792 | 109.271593 | 92.005592 | 45.806792 | 103.080599 | -76.528107 |
| `F1_BC` | -20.065420 | 47.645599 | 19.977559 | 20.065420 | 48.583549 | -20.915509 |
| `F1_CD` | -41.947433 | 102.215271 | 74.444505 | 41.947433 | 110.136920 | -94.248628 |
| `BAL_L` | 0.000000 | -0.000000 | -0.000000 | 0.000000 | 68.177489 | -40.906493 |
| `BAL_R` | 0.000000 | 68.177489 | 40.906493 | 0.000000 | 0.000000 | -0.000000 |
| `F2_AB` | -112.533290 | 97.413179 | 39.280050 | 112.533290 | 114.939012 | -83.094633 |
| `F2_BC` | -146.748604 | 47.704731 | 27.991566 | 146.748604 | 48.524418 | -28.811253 |
| `F2_CD` | -116.895063 | 114.148020 | 81.233371 | 116.895063 | 98.204171 | -41.373750 |
| `RAFTER_L` | 186.901843 | 48.889773 | 44.704809 | -153.537043 | 51.204627 | -52.025018 |
| `RAFTER_R` | 153.552411 | 51.158525 | 52.025018 | -186.917211 | 48.935875 | -44.996384 |
| `BR_A0_B1` | 10.030137 | 0.000000 | 0.000000 | -10.030137 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 9.119762 | 0.000000 | 0.000000 | -9.119762 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -3.048426 | 0.000000 | 0.000000 | 3.048426 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -6.927749 | 0.000000 | 0.000000 | 6.927749 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

