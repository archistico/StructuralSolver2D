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
