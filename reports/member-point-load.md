# member-point-load

**Description:** Structural model loaded from JSON.
**Source:** `examples\member-point-load.json`
**Load case:** `LC1`
**Generated UTC:** 2026-05-14 12:38:45

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
| `B` | 8.000000 | 0.000000 | - |

### Materials

| Id | Name | E [kN/m²] | Unit weight [kN/m³] |
|---|---|---:|---:|
| `MAT` | Generic steel | 210000000.000000 | - |

### Sections

| Id | Name | Area [m²] | I [m⁴] | Height [m] | Width [m] |
|---|---|---:|---:|---:|---:|
| `SEC` | Generic section | 0.003000 | 0.000020 | - | - |

### Members

| Id | Type | Start | End | Material | Section | Length [m] | Label |
|---|---|---|---|---|---|---:|---|
| `M1` | Frame2D | `A` | `B` | `MAT` | `SEC` | 8.000000 | - |

### Supports

| Id | Node | Type | Ux | Uy | Rz | Label |
|---|---|---|---:|---:|---:|---|
| `SA` | `A` | Hinge | yes | yes | no | - |
| `SB` | `B` | SimpleSupport | no | yes | no | - |

### Load cases

| Id | Name | Description |
|---|---|---|
| `LC1` | Point load | - |

### Loads

| Id | Load case | Type | Target | Direction | Value | Position | Label |
|---|---|---|---|---|---:|---:|---|
| `P1` | `LC1` | PointLoadOnMember | Member: `M1` | GlobalY | -12.000000 | 0.250000 | - |

## Results

### Nodal displacements

| Node | Ux [m] | Uy [m] | Rz [rad] |
|---|---:|---:|---:|
| `A` | 0.000000 | 0.000000 | -0.010000 |
| `B` | 0.000000 | 0.000000 | 0.007143 |

### Support reactions

| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |
|---|---|---:|---:|---:|
| `SA` | `A` | 0.000000 | 9.000000 | 0.000000 |
| `SB` | `B` | 0.000000 | 3.000000 | 0.000000 |

### Local member end forces

| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |
|---|---:|---:|---:|---:|---:|---:|
| `M1` | 0.000000 | 9.000000 | 0.000000 | 0.000000 | 3.000000 | 0.000000 |

### Maximum absolute results

| Quantity | Value | Location |
|---|---:|---|
| Max \|Ux\| [m] | 0.000000 | node `A` |
| Max \|Uy\| [m] | 0.000000 | node `A` |
| Max \|Rz\| [rad] | -0.010000 | node `A` |
| Max \|Fx\| [kN] | 0.000000 | support `SA` |
| Max \|Fy\| [kN] | 9.000000 | support `SA` |
| Max \|Mz\| [kNm] | 0.000000 | support `SA` |
| Max \|N\| [kN] | -0.000000 | member `M1`, x = 0.000000 m |
| Max \|V\| [kN] | 9.000000 | member `M1`, x = 0.000000 m |
| Max \|M\| [kNm] | 18.000000 | member `M1`, x = 2.000000 m |

## Internal force diagrams

### Member `M1`

Length: **8.000000 m**

| Position | x [m] | N [kN] | V [kN] | M [kNm] |
|---:|---:|---:|---:|---:|
| 0.000000 | 0.000000 | -0.000000 | 9.000000 | 0.000000 |
| 0.050000 | 0.400000 | -0.000000 | 9.000000 | 3.600000 |
| 0.100000 | 0.800000 | -0.000000 | 9.000000 | 7.200000 |
| 0.150000 | 1.200000 | -0.000000 | 9.000000 | 10.800000 |
| 0.200000 | 1.600000 | -0.000000 | 9.000000 | 14.400000 |
| 0.250000 | 2.000000 | 0.000000 | -3.000000 | 18.000000 |
| 0.300000 | 2.400000 | 0.000000 | -3.000000 | 16.800000 |
| 0.350000 | 2.800000 | 0.000000 | -3.000000 | 15.600000 |
| 0.400000 | 3.200000 | 0.000000 | -3.000000 | 14.400000 |
| 0.450000 | 3.600000 | 0.000000 | -3.000000 | 13.200000 |
| 0.500000 | 4.000000 | 0.000000 | -3.000000 | 12.000000 |
| 0.550000 | 4.400000 | 0.000000 | -3.000000 | 10.800000 |
| 0.600000 | 4.800000 | 0.000000 | -3.000000 | 9.600000 |
| 0.650000 | 5.200000 | 0.000000 | -3.000000 | 8.400000 |
| 0.700000 | 5.600000 | 0.000000 | -3.000000 | 7.200000 |
| 0.750000 | 6.000000 | 0.000000 | -3.000000 | 6.000000 |
| 0.800000 | 6.400000 | 0.000000 | -3.000000 | 4.800000 |
| 0.850000 | 6.800000 | 0.000000 | -3.000000 | 3.600000 |
| 0.900000 | 7.200000 | 0.000000 | -3.000000 | 2.400000 |
| 0.950000 | 7.600000 | 0.000000 | -3.000000 | 1.200000 |
| 1.000000 | 8.000000 | 0.000000 | -3.000000 | 0.000000 |

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces are sampled values; exact extrema may require analytical post-processing for some load configurations.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

