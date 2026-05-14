# B01 - Simply supported beam with uniform distributed load

**Description:** L = 5 m, q = 10 kN/m downward. Reference values: RA = RB = 25 kN, Mmax = 31.25 kNm.
**Source:** `benchmarks\beams\B01-simple-supported-udl.json`
**Analysis id:** `LC1`
**Generated UTC:** 2026-05-14 13:12:06

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

## Model

### Nodes

| Id | X [m] | Y [m] | Label |
|---|---:|---:|---|
| `A` | 0.000000 | 0.000000 | - |
| `B` | 5.000000 | 0.000000 | - |

### Materials

| Id | Name | E [kN/m²] | Unit weight [kN/m³] |
|---|---|---:|---:|
| `MAT` | Benchmark elastic material | 210000000.000000 | - |

### Sections

| Id | Name | Area [m²] | I [m⁴] | Height [m] | Width [m] |
|---|---|---:|---:|---:|---:|
| `SEC` | Benchmark frame section | 0.003000 | 0.000020 | - | - |

### Members

| Id | Type | Start | End | Material | Section | Length [m] | Label |
|---|---|---|---|---|---|---:|---|
| `M1` | Frame2D | `A` | `B` | `MAT` | `SEC` | 5.000000 | - |

### Supports

| Id | Node | Type | Ux | Uy | Rz | Label |
|---|---|---|---:|---:|---:|---|
| `SA` | `A` | Hinge | yes | yes | no | - |
| `SB` | `B` | SimpleSupport | no | yes | no | - |

### Load cases

| Id | Name | Description |
|---|---|---|
| `LC1` | Uniform distributed load | - |

### Load combinations

| Id | Name | Expression | Description |
|---|---|---|---|

### Loads

| Id | Load case | Type | Target | Direction | Value | End value | Position | Label |
|---|---|---|---|---|---:|---:|---:|---|
| `Q1` | `LC1` | UniformDistributedLoad | Member: `M1` | GlobalY | -10.000000 | - | - | - |

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A` | 0.000000 | 0.000000 | -0.012401 |
| `B` | 0.000000 | 0.000000 | 0.012401 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SA` | `A` | 0.000000 | 25.000000 | 0.000000 |
| `SB` | `B` | 0.000000 | 25.000000 | 0.000000 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `M1` | 0.000000 | 25.000000 | 0.000000 | 0.000000 | 25.000000 | 0.000000 |

### Maximum absolute results

| Quantity | Value | Location |
|---|---:|---|
| Max \|Ux\| [m] | 0.000000 | node `A` |
| Max \|Uy\| [m] | 0.000000 | node `A` |
| Max \|Rz\| [rad] | -0.012401 | node `A` |
| Max \|Fx\| [kN] | 0.000000 | support `SA` |
| Max \|Fy\| [kN] | 25.000000 | support `SA` |
| Max \|Mz\| [kNm] | 0.000000 | support `SA` |
| Max \|N\| [kN] | -0.000000 | member `M1`, x = 0.000000 m |
| Max \|V\| [kN] | 25.000000 | member `M1`, x = 0.000000 m |
| Max \|M\| [kNm] | 31.250000 | member `M1`, x = 2.500000 m |

## Internal force diagrams

### Member `M1`

Length: **5.000000 m**

| Position | x [m] | N [kN] | V [kN] | M [kNm] |
|---:|---:|---:|---:|---:|
| 0.000000 | 0.000000 | -0.000000 | 25.000000 | 0.000000 |
| 0.050000 | 0.250000 | -0.000000 | 22.500000 | 5.937500 |
| 0.100000 | 0.500000 | -0.000000 | 20.000000 | 11.250000 |
| 0.150000 | 0.750000 | -0.000000 | 17.500000 | 15.937500 |
| 0.200000 | 1.000000 | -0.000000 | 15.000000 | 20.000000 |
| 0.250000 | 1.250000 | -0.000000 | 12.500000 | 23.437500 |
| 0.300000 | 1.500000 | -0.000000 | 10.000000 | 26.250000 |
| 0.350000 | 1.750000 | -0.000000 | 7.500000 | 28.437500 |
| 0.400000 | 2.000000 | -0.000000 | 5.000000 | 30.000000 |
| 0.450000 | 2.250000 | -0.000000 | 2.500000 | 30.937500 |
| 0.500000 | 2.500000 | -0.000000 | 0.000000 | 31.250000 |
| 0.550000 | 2.750000 | -0.000000 | -2.500000 | 30.937500 |
| 0.600000 | 3.000000 | -0.000000 | -5.000000 | 30.000000 |
| 0.650000 | 3.250000 | -0.000000 | -7.500000 | 28.437500 |
| 0.700000 | 3.500000 | -0.000000 | -10.000000 | 26.250000 |
| 0.750000 | 3.750000 | -0.000000 | -12.500000 | 23.437500 |
| 0.800000 | 4.000000 | -0.000000 | -15.000000 | 20.000000 |
| 0.850000 | 4.250000 | -0.000000 | -17.500000 | 15.937500 |
| 0.900000 | 4.500000 | -0.000000 | -20.000000 | 11.250000 |
| 0.950000 | 4.750000 | -0.000000 | -22.500000 | 5.937500 |
| 1.000000 | 5.000000 | -0.000000 | -25.000000 | 0.000000 |

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces are sampled values; exact extrema may require analytical post-processing for some load configurations.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

