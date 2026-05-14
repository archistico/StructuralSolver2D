# Simply supported beam with uniform load

**Description:** L = 5 m, q = 10 kN/m downward.
**Source:** `examples\beams\simple-supported-beam.json`
**Analysis id:** `LC1`
**Generated UTC:** 2026-05-14 16:27:54

## How to read this report

This report is organized as a learning-oriented structural analysis note: first the assumptions and model data, then the numerical results, then sampled diagrams and checks.

Key conventions:

- `Ux` and `Uy` are global nodal translations.
- `Rz` is the nodal rotation about the out-of-plane Z axis.
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
| Nodes | 2 |
| Members | 1 |
| Supports | 2 |
| Load cases | 1 |
| Load combinations | 0 |
| Loads | 1 |

### Governing absolute values

| Result | Value | Location |
|---|---:|---|
| Max \|Ux\| [m] | 0.000000 | node `A` |
| Max \|Uy\| [m] | 0.000000 | node `A` |
| Max \|M\| [kNm] | 31.250000 | member `M1`, x = 2.500000 m |

## Model

### Nodes

| Id | X [m] | Y [m] | Label |
|---|---:|---:|---|
| `A` | 0.000000 | 0.000000 | - |
| `B` | 5.000000 | 0.000000 | - |

### Materials

| Id | Name | E [kN/m²] | Unit weight [kN/m³] |
|---|---|---:|---:|
| `MAT` | Generic elastic material | 210000000.000000 | - |

### Sections

| Id | Name | Area [m²] | I [m⁴] | Height [m] | Width [m] |
|---|---|---:|---:|---:|---:|
| `SEC` | Generic section | 0.003000 | 0.000020 | - | - |

### Members

| Id | Type | Start | End | Material | Section | Length [m] | Start M release | End M release | Label |
|---|---|---|---|---|---|---:|---:|---:|---|
| `M1` | Frame2D | `A` | `B` | `MAT` | `SEC` | 5.000000 | no | no | - |

### Supports

| Id | Node | Type | Ux | Uy | Rz | Label |
|---|---|---|---:|---:|---:|---|
| `SA` | `A` | Hinge | yes | yes | no | - |
| `SB` | `B` | SimpleSupport | no | yes | no | - |

### Load cases

| Id | Name | Description |
|---|---|---|
| `LC1` | Default load case | - |

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

## Characteristic internal-force points

The values below are detected from sampled N/V/M diagrams. Zero crossings between adjacent samples are linearly interpolated; exact analytical characteristic points may require dedicated closed-form post-processing for some load configurations.

### Member `M1`

Length: **5.000000 m**

| Kind | Quantity | Position | x [m] | Value | Description |
|---|---|---:|---:|---:|---|
| EndPoint | Multiple | 0.000000 | 0.000000 | 0.000000 | member start |
| ZeroCrossing | NormalForce | 0.000000 | 0.000000 | 0.000000 | zero N throughout sampled member |
| ZeroCrossing | BendingMoment | 0.000000 | 0.000000 | 0.000000 | zero M |
| SampledMinimum | NormalForce | 0.000000 | 0.000000 | -0.000000 | sampled minimum N |
| SampledMinimum | BendingMoment | 0.000000 | 0.000000 | 0.000000 | sampled minimum M |
| SampledMaximum | NormalForce | 0.000000 | 0.000000 | -0.000000 | sampled maximum N |
| SampledMaximum | ShearForce | 0.000000 | 0.000000 | 25.000000 | sampled maximum V |
| SampledMaximumAbsolute | NormalForce | 0.000000 | 0.000000 | -0.000000 | sampled maximum absolute N |
| SampledMaximumAbsolute | ShearForce | 0.000000 | 0.000000 | 25.000000 | sampled maximum absolute V |
| ZeroCrossing | ShearForce | 0.500000 | 2.500000 | 0.000000 | zero V |
| SampledMaximum | BendingMoment | 0.500000 | 2.500000 | 31.250000 | sampled maximum M |
| SampledMaximumAbsolute | BendingMoment | 0.500000 | 2.500000 | 31.250000 | sampled maximum absolute M |
| BendingMomentExtremumCandidate | BendingMoment | 0.500000 | 2.500000 | 31.250000 | bending moment extremum candidate from zero shear |
| EndPoint | Multiple | 1.000000 | 5.000000 | 0.000000 | member end |
| ZeroCrossing | BendingMoment | 1.000000 | 5.000000 | 0.000000 | zero M |
| SampledMinimum | ShearForce | 1.000000 | 5.000000 | -25.000000 | sampled minimum V |

## Deformed shape samples

The values below are finite-element interpolated displacements from nodal results. They are suitable for drawing the deformed shape; internal deflections under distributed loads are not always identical to closed-form beam solutions unless the checked point is explicitly modeled as a node.

### Member `M1`

Length: **5.000000 m**

| Position | x [m] | u local [m] | v local [m] | rz local [rad] | Ux global [m] | Uy global [m] |
|---:|---:|---:|---:|---:|---:|---:|
| 0.000000 | 0.000000 | 0.000000 | 0.000000 | -0.012401 | 0.000000 | 0.000000 |
| 0.050000 | 0.250000 | 0.000000 | -0.002945 | -0.011161 | 0.000000 | -0.002945 |
| 0.100000 | 0.500000 | 0.000000 | -0.005580 | -0.009921 | 0.000000 | -0.005580 |
| 0.150000 | 0.750000 | 0.000000 | -0.007906 | -0.008681 | 0.000000 | -0.007906 |
| 0.200000 | 1.000000 | 0.000000 | -0.009921 | -0.007440 | 0.000000 | -0.009921 |
| 0.250000 | 1.250000 | 0.000000 | -0.011626 | -0.006200 | 0.000000 | -0.011626 |
| 0.300000 | 1.500000 | 0.000000 | -0.013021 | -0.004960 | 0.000000 | -0.013021 |
| 0.350000 | 1.750000 | 0.000000 | -0.014106 | -0.003720 | 0.000000 | -0.014106 |
| 0.400000 | 2.000000 | 0.000000 | -0.014881 | -0.002480 | 0.000000 | -0.014881 |
| 0.450000 | 2.250000 | 0.000000 | -0.015346 | -0.001240 | 0.000000 | -0.015346 |
| 0.500000 | 2.500000 | 0.000000 | -0.015501 | 0.000000 | 0.000000 | -0.015501 |
| 0.550000 | 2.750000 | 0.000000 | -0.015346 | 0.001240 | 0.000000 | -0.015346 |
| 0.600000 | 3.000000 | 0.000000 | -0.014881 | 0.002480 | 0.000000 | -0.014881 |
| 0.650000 | 3.250000 | 0.000000 | -0.014106 | 0.003720 | 0.000000 | -0.014106 |
| 0.700000 | 3.500000 | 0.000000 | -0.013021 | 0.004960 | 0.000000 | -0.013021 |
| 0.750000 | 3.750000 | 0.000000 | -0.011626 | 0.006200 | 0.000000 | -0.011626 |
| 0.800000 | 4.000000 | 0.000000 | -0.009921 | 0.007440 | 0.000000 | -0.009921 |
| 0.850000 | 4.250000 | 0.000000 | -0.007906 | 0.008681 | 0.000000 | -0.007906 |
| 0.900000 | 4.500000 | 0.000000 | -0.005580 | 0.009921 | 0.000000 | -0.005580 |
| 0.950000 | 4.750000 | 0.000000 | -0.002945 | 0.011161 | 0.000000 | -0.002945 |
| 1.000000 | 5.000000 | 0.000000 | 0.000000 | 0.012401 | 0.000000 | 0.000000 |

## Preliminary serviceability deflection checks

No preliminary deflection checks were supplied.

## Notes and limitations

- The report is generated from a 2D frame linear static analysis result.
- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.
- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.
- The current scope is analysis of simple plane structural schemes, not complete normative design verification.
- Results must be checked by a qualified professional before any practical engineering use.

