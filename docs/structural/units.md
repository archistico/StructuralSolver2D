# StructuralSolver2D - Units

StructuralSolver2D uses a fixed internal unit system.

The user interface, CLI or future OpenCad2D integration may display configurable units, but all values must be converted to the internal system before analysis.

## Internal units

The internal units are:

```text
length:              m
force:               kN
moment:              kNm
elastic modulus E:   kN/m²
area:                m²
second moment area:  m⁴
```

## Reason for fixed internal units

A structural solver must use coherent units internally.

Fixed internal units reduce the risk of mixing:

- meters and millimeters;
- newtons and kilonewtons;
- kNm and Nmm;
- m² and mm²;
- m⁴ and mm⁴.

The UI can be flexible, but the engine must be strict.

## Elastic modulus examples

Steel example:

```text
E = 210000 N/mm²
E = 210000 MPa
E = 210 GPa
E = 210000000 kN/m²
```

Timber example:

```text
E = 11000 N/mm²
E = 11000 MPa
E = 11 GPa
E = 11000000 kN/m²
```

Internally, the solver stores `E` in `kN/m²`.

## Section property examples

If a section area is entered in `cm²`, it must be converted to `m²`.

```text
1 cm² = 0.0001 m²
```

If a second moment of area is entered in `cm⁴`, it must be converted to `m⁴`.

```text
1 cm⁴ = 0.00000001 m⁴
```

If a second moment of area is entered in `mm⁴`, it must be converted to `m⁴`.

```text
1 mm⁴ = 0.000000000001 m⁴
```

## Load units

Nodal forces are stored in:

```text
kN
```

Nodal moments are stored in:

```text
kNm
```

Uniform distributed loads are stored in:

```text
kN/m
```

## UI display units

The UI may allow users to display and input values in more familiar units, for example:

```text
length: mm, cm, m
force: N, daN, kN
moment: Nm, kNm, Nmm
elastic modulus: N/mm², MPa, GPa, kN/m²
area: mm², cm², m²
second moment area: mm⁴, cm⁴, m⁴
```

Every external value must be converted to internal units before it reaches the solver.

## Recommended rule

The analysis layer should not guess units.

All numerical values passed to the solver must already be in internal units.

Unit conversion should live in a dedicated layer or helper, not inside the matrix assembly code.

## Dynamic extension (M52, schema version 1; contracts only)

**All quantities below use the existing kN–m system extended with seconds.** No mass matrix,
frequency solver, time integration, automatic gravity load or seismic code check is present in M52.

| Quantity | Internal unit | Notes |
| --- | --- | --- |
| Time and period | `s` | Frequency is inverse of period |
| Frequency | `Hz` = `1/s` | `f = omega / (2*pi)` |
| Angular frequency | `rad/s` | Radians are dimensionless |
| Linear acceleration | `m/s²` | Reference gravity `g0 = 9.80665 m/s²` |
| Nodal force | `kN` | Translational DOF |
| Nodal moment | `kNm` | Rotational DOF |
| Nodal translational mass | `kN*s²/m` | One such unit is **1000 kg**, applied to both Ux and Uy |
| Nodal polar moment of mass / rotational inertia | `kN*s²*m` | One such unit is **1000 kg*m²**, for Rz |
| Volumetric mass density | `kN*s²/m⁴` | `rho = gamma / g0` |
| Distributed line mass | `kN*s²/m²` | `mu = gamma*A/g0` |
| Member total mass | `kN*s²/m` | `m_member = gamma*A*L/g0` |

The existing `StructuralMaterial.UnitWeight` is **specific weight** `gamma` in `kN/m³`,
**not** volumetric mass density. For example, `gamma=78.4532 kN/m³`, `A=0.005 m²`
and `L=5 m` give `rho=8 kN*s²/m⁴`, `mu=0.04 kN*s²/m²`,
`m_member=0.2 kN*s²/m` (=200 kg).

`DynamicMassUnits` contains explicit conversion methods. External masses must not be passed
as raw `kg`, `kg*m²` or `N*s²/m`; convert before constructing `StructuralNodalMass`.

### Additive policy and dynamic validation

- Every member derives its distributed mass from its material `UnitWeight` and section `Area`.
  Absence of `UnitWeight` blocks a **future modal** request; it does not block static analysis.
- `dynamics.nodalMasses` entries are **additional** to those distributed masses. They never
  replace or cancel self-mass. Only one user entry per node is allowed; aggregate equipment
  masses upstream to avoid unintended duplicates. `translationalMass` is isotropic in the plane;
  `rotationalInertia` is optional and only compatible with a node attached to a Frame2D member.
- All supplied masses must be positive, finite and expressed in the internal units. A model with
  truss-only members may have massless Rz DOFs: M53–M54 must remove/handle these properly;
  **do not inject arbitrary masses**.
- Adding material `UnitWeight` or a nodal mass does **not** add a gravitational load.
  Self-weight loads remain explicit static inputs and must be modeled separately.
- Dynamic checks reside in `StructuralDynamicsValidator` and are opt-in. The existing static
  model, validator, static request and CLI commands retain their original behavior.

### JSON example: versioned, optional contract

```json
"dynamics": {
  "schemaVersion": 1,
  "nodalMasses": [
    { "id": "EQUIPMENT", "nodeId": "N2", "translationalMass": 0.5, "rotationalInertia": 0.02 }
  ]
}
```

This snippet belongs inside an otherwise standard StructuralSolver2D model JSON document.
Unknown properties **within** `dynamics` or its mass entries are rejected by the CLI reader.
Older JSON files without the block remain fully supported. An explicit version other than 1
is rejected by `StructuralDynamicsValidator` with `DYNAMIC_SCHEMA_UNSUPPORTED`.
The reader does not execute modal analysis, and the CLI does not yet validate dynamic metadata
unless its consumer explicitly invokes `StructuralDynamicsValidator`.

The C# request `StructuralModalAnalysisRequest` and `StructuralModalAnalysisOutput` are
**reserved contracts**; no modal solver or CLI modal command exists until later milestones.
Time-varying loads, damping and initial displacements/velocities are not modeled in schema v1.

See `examples/dynamics/m52-cantilever-with-additional-mass.json`.
