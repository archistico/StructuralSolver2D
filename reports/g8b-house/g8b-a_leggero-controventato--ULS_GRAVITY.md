# G8B House A&#95;LEGGERO with crossed X-braces

**Description:** Didactic G8B comparative model A&#95;LEGGERO; steel two-storey frame with 5+2+5m bays, 4m tributary width, timber roof, explicit member self-weight; X-braces in left bay on both storeys, linear Truss2D both tension AND compression.
**Source:** `examples/mixed/g8b-house/g8b-a_leggero-controventato.json`
**Analysis id:** `ULS_GRAVITY`
**Generated UTC:** 2026-10-24 22:04:45

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
| Max \|Ux\| [m] | 0.001082 | node `D2` |
| Max \|Uy\| [m] | 0.009907 | node `RIDGE` |
| Max \|Rz\| [rad] | 0.004930 | node `A2` |
| Max \|Fx\| [kN] | 19.045853 | support `SUP_B0` |
| Max \|Fy\| [kN] | 386.793064 | support `SUP_A0` |
| Max \|Mz\| [kNm] | 8.535780 | support `SUP_B0` |
| Max \|N\| [kN] | 384.380527 | member `COL_D_1`, x = 0.000000 m |
| Max \|V\| [kN] | 114.471457 | member `F2_AB`, x = 5.000000 m |
| Max \|M\| [kNm] | 92.980685 | member `F1_CD`, x = 5.000000 m |

Load combination: `ULS_GRAVITY` = 1.350000 × `G_SELF` + 1.350000 × `G_FLOOR` + 1.500000 × `Q_ROOM` + 1.500000 × `Q_CORR` + 1.350000 × `G_BALC` + 1.500000 × `Q_BALC` + 1.350000 × `G_ROOF` + 1.500000 × `SNOW`.

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A0` | 0.000000 | 0.000000 | 0.000000 |
| `B0` | 0.000000 | 0.000000 | 0.000000 |
| `C0` | 0.000000 | 0.000000 | 0.000000 |
| `D0` | 0.000000 | -0.000000 | 0.000000 |
| `BAL_L1` | -0.000147 | -0.004004 | 0.003760 |
| `A1` | -0.000147 | -0.000692 | -0.000242 |
| `B1` | 0.000058 | -0.000566 | 0.001013 |
| `C1` | 0.000122 | -0.000579 | -0.001083 |
| `D1` | 0.000304 | -0.000701 | 0.000154 |
| `BAL_R1` | 0.000304 | -0.004118 | -0.003848 |
| `A2` | -0.000423 | -0.001071 | -0.004930 |
| `B2` | 0.000071 | -0.000870 | 0.002922 |
| `C2` | 0.000563 | -0.000877 | -0.002981 |
| `D2` | 0.001082 | -0.001073 | 0.004840 |
| `RIDGE` | 0.000330 | -0.009907 | 0.000022 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SUP_A0` | `A0` | 13.900792 | 386.793064 | -3.103475 |
| `SUP_B0` | `B0` | -19.045853 | 317.366771 | 8.535780 |
| `SUP_C0` | `C0` | 7.990914 | 317.448038 | -7.667763 |
| `SUP_D0` | `D0` | -2.845853 | 384.380527 | 3.652772 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `COL_A_1` | 379.616918 | -2.712968 | -3.103475 | -377.134559 | 2.712968 | -5.035429 |
| `COL_B_1` | 310.505012 | 8.382008 | 8.535780 | -308.022654 | -8.382008 | 16.610243 |
| `COL_C_1` | 317.448038 | -7.990914 | -7.667763 | -314.965680 | 7.990914 | -16.304980 |
| `COL_D_1` | 384.380527 | 2.845853 | 3.652772 | -381.898169 | -2.845853 | 4.884787 |
| `COL_A_2` | 208.493933 | -42.710804 | -45.375653 | -206.011575 | 42.710804 | -82.756758 |
| `COL_B_2` | 167.737069 | 31.441051 | 39.549451 | -165.254711 | -31.441051 | 54.773702 |
| `COL_C_2` | 164.638853 | -30.059401 | -37.522826 | -162.156495 | 30.059401 | -52.655377 |
| `COL_D_2` | 204.476099 | 43.969493 | 47.270589 | -201.993740 | -43.969493 | 84.637890 |
| `F1_AB` | -46.267563 | 108.706321 | 91.236390 | 46.267563 | 102.644406 | -76.081602 |
| `F1_BC` | -19.055154 | 47.647645 | 19.921907 | 19.055154 | 48.355989 | -20.630251 |
| `F1_CD` | -41.123640 | 101.970838 | 74.458056 | 41.123640 | 109.379889 | -92.980685 |
| `BAL_L` | 0.000000 | 0.000000 | -0.000000 | 0.000000 | 68.042180 | -40.825308 |
| `BAL_R` | 0.000000 | 68.042180 | 40.825308 | 0.000000 | 0.000000 | 0.000000 |
| `F2_AB` | -111.453709 | 96.879270 | 38.517233 | 111.453709 | 114.471457 | -82.497701 |
| `F2_BC` | -147.288878 | 47.683331 | 27.724000 | 147.288878 | 48.320302 | -28.360970 |
| `F2_CD` | -117.229477 | 113.836193 | 81.016347 | 117.229477 | 97.514535 | -40.212202 |
| `RAFTER_L` | 185.956185 | 48.112652 | 44.239525 | -152.921865 | 50.990308 | -53.339471 |
| `RAFTER_R` | 152.931677 | 50.960873 | 53.339471 | -185.965997 | 48.142087 | -44.425688 |
| `BR_A0_B1` | 13.047133 | 0.000000 | 0.000000 | -13.047133 | 0.000000 | 0.000000 |
| `BR_B0_A1` | 12.436074 | 0.000000 | 0.000000 | -12.436074 | 0.000000 | 0.000000 |
| `BR_A1_B2` | -5.124378 | 0.000000 | 0.000000 | 5.124378 | 0.000000 | 0.000000 |
| `BR_B1_A2` | -8.203517 | 0.000000 | 0.000000 | 8.203517 | 0.000000 | 0.000000 |

Model tables and sampled appendices are omitted in this summary. Use a full report or CSV/XLSX for detailed data.

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

