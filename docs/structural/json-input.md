# JSON input format

StructuralSolver2D can read structural models from JSON files through the CLI. The JSON format intentionally mirrors the internal model so that examples remain readable and easy to debug.

A JSON input file describes:

- metadata used by the CLI;
- nodes;
- materials;
- sections;
- members;
- supports;
- load cases;
- loads;
- optional manual load combinations.

The main command is:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\beams\simple-supported-beam.json
```

A specific load case or combination can be selected with the final argument:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json ULS1
```

The same JSON input can be reused by report/export commands such as `report`, `export-csv`, `export-xlsx`, `export-pdf`, `export-svg`, `export-html` and `export-viewer`.

---

## Complete minimal example

```json
{
  "title": "Simply supported beam with uniform load",
  "description": "L = 5 m, q = 10 kN/m downward.",
  "loadCaseId": "LC1",
  "nodes": [
    { "id": "A", "x": 0.0, "y": 0.0 },
    { "id": "B", "x": 5.0, "y": 0.0 }
  ],
  "materials": [
    { "id": "MAT", "name": "Generic elastic material", "elasticModulus": 210000000.0 }
  ],
  "sections": [
    { "id": "SEC", "name": "Generic section", "area": 0.003, "momentOfInertia": 0.00002 }
  ],
  "members": [
    { "id": "M1", "startNodeId": "A", "endNodeId": "B", "materialId": "MAT", "sectionId": "SEC", "type": "Frame2D" }
  ],
  "supports": [
    { "id": "SA", "nodeId": "A", "restrainedUx": true, "restrainedUy": true, "restrainedRz": false, "type": "Hinge" },
    { "id": "SB", "nodeId": "B", "restrainedUx": false, "restrainedUy": true, "restrainedRz": false, "type": "SimpleSupport" }
  ],
  "loadCases": [
    { "id": "LC1", "name": "Default load case" }
  ],
  "loads": [
    { "id": "Q1", "loadCaseId": "LC1", "type": "UniformDistributedLoad", "targetType": "Member", "targetId": "M1", "direction": "GlobalY", "value": -10.0 }
  ]
}
```

---

## Units and sign convention

Use these units consistently:

| Quantity | Unit |
|---|---:|
| Coordinates | m |
| Area | m² |
| Moment of inertia | m⁴ |
| Elastic modulus | kN/m² |
| Unit weight | kN/m³ |
| Nodal force | kN |
| Nodal moment | kNm |
| Member point load | kN |
| Distributed load | kN/m |
| Displacement result | m |
| Rotation result | rad |

Global axes:

- `GlobalX`: positive to the right;
- `GlobalY`: positive upward;
- `MomentZ`: positive around the out-of-plane Z axis.

A vertical downward load normally has a negative `GlobalY` value.

Example:

```json
{ "direction": "GlobalY", "value": -10.0 }
```

---

## Root properties

| Property | Required | Meaning |
|---|---:|---|
| `title` | no | Human-readable title used by reports/exporters. |
| `description` | no | Short description of the structural scheme. |
| `loadCaseId` | no | Default load case to analyze. If omitted, the first load case is used unless `loadCombinationId` is present. |
| `loadCombinationId` | no | Default load combination to analyze. Takes precedence over `loadCaseId`. |
| `nodes` | yes | Structural nodes. |
| `materials` | yes | Materials referenced by members. |
| `sections` | yes | Sections referenced by members. |
| `members` | yes | Frame/truss members. |
| `supports` | usually yes | Nodal supports. A model without enough supports will be unstable. |
| `loadCases` | yes | Load cases. |
| `loads` | no | Loads assigned to load cases. |
| `loadCombinations` | no | Manual load combinations. |

---

## Nodes

Nodes define the geometry of the model.

```json
{
  "id": "A",
  "x": 0.0,
  "y": 0.0,
  "label": "Left support"
}
```

| Property | Required | Meaning |
|---|---:|---|
| `id` | yes | Unique node identifier. |
| `x` | yes | X coordinate in m. |
| `y` | yes | Y coordinate in m. |
| `label` | no | User-facing label. |

---

## Materials

Materials define elastic stiffness and optional unit weight.

```json
{
  "id": "S355",
  "name": "Steel S355",
  "elasticModulus": 210000000.0,
  "unitWeight": 78.5
}
```

| Property | Required | Meaning |
|---|---:|---|
| `id` | yes | Unique material identifier. |
| `name` | yes | Human-readable name. |
| `elasticModulus` | yes | Young modulus in kN/m². For steel use about `210000000.0`. |
| `unitWeight` | no | Unit weight in kN/m³. Currently useful as data; self-weight generation is not the main JSON workflow yet. |

---

## Sections

Sections define member area and bending stiffness.

```json
{
  "id": "RECT_200x400",
  "name": "Rectangular 200 x 400 mm",
  "area": 0.08,
  "momentOfInertia": 0.0010666666666666667,
  "height": 0.40,
  "width": 0.20
}
```

| Property | Required | Meaning |
|---|---:|---|
| `id` | yes | Unique section identifier. |
| `name` | yes | Human-readable name. |
| `area` | yes | Area `A` in m². |
| `momentOfInertia` | yes | Bending inertia `I` in m⁴. |
| `height` | no | Optional display/checking height in m. |
| `width` | no | Optional display/checking width in m. |

For a rectangular section:

```text
A = b · h
I = b · h³ / 12
```

Example for 200 mm x 400 mm:

```text
b = 0.20 m
h = 0.40 m
A = 0.20 · 0.40 = 0.08 m²
I = 0.20 · 0.40³ / 12 = 0.0010666667 m⁴
```

---

## Members

Members connect two nodes and reference a material and section.

```json
{
  "id": "M1",
  "startNodeId": "A",
  "endNodeId": "B",
  "materialId": "S355",
  "sectionId": "RECT_200x400",
  "type": "Frame2D",
  "label": "Beam A-B",
  "releaseStartMoment": false,
  "releaseEndMoment": false
}
```

| Property | Required | Meaning |
|---|---:|---|
| `id` | yes | Unique member identifier. |
| `startNodeId` | yes | Start node id. |
| `endNodeId` | yes | End node id. |
| `materialId` | yes | Existing material id. |
| `sectionId` | yes | Existing section id. |
| `type` | no | `Frame2D` by default. Supported values: `Frame2D`, `Truss2D`. |
| `label` | no | User-facing label. |
| `releaseStartMoment` | no | If true, releases bending moment at start. Frame2D only. |
| `releaseEndMoment` | no | If true, releases bending moment at end. Frame2D only. |

Use `Frame2D` for beams, columns and rigid-jointed frames. Use `Truss2D` for axial-only bars.

---

## Supports and restraints

A support is attached to a node and restrains one or more degrees of freedom.

The three degrees of freedom are:

- `Ux`: horizontal translation;
- `Uy`: vertical translation;
- `Rz`: in-plane rotation.

### Hinge / pinned support

Restrains `Ux` and `Uy`, leaves `Rz` free.

```json
{
  "id": "SA",
  "nodeId": "A",
  "restrainedUx": true,
  "restrainedUy": true,
  "restrainedRz": false,
  "type": "Hinge"
}
```

### Simple support / roller

Restrains one translational direction and leaves rotation free.

```json
{
  "id": "SB",
  "nodeId": "B",
  "restrainedUx": false,
  "restrainedUy": true,
  "restrainedRz": false,
  "type": "SimpleSupport"
}
```

### Fixed support

Restrains `Ux`, `Uy` and `Rz`.

```json
{
  "id": "SA",
  "nodeId": "A",
  "restrainedUx": true,
  "restrainedUy": true,
  "restrainedRz": true,
  "type": "Fixed"
}
```

### Rotated support

Supports can be oriented with `orientationDegrees`.

```json
{
  "id": "SB",
  "nodeId": "B",
  "restrainedUx": false,
  "restrainedUy": true,
  "restrainedRz": false,
  "type": "SimpleSupport",
  "orientationDegrees": 30.0,
  "label": "Inclined roller"
}
```

For an oriented support:

- `restrainedUx` restrains the local support X direction;
- `restrainedUy` restrains the local support Y direction;
- `restrainedRz` restrains the nodal rotation.

This is the correct way to model an inclined roller or a simple one-direction restraint.

---

## Load cases

A load case groups loads that act together.

```json
{
  "id": "G1",
  "name": "Permanent loads",
  "description": "Self-weight and permanent actions"
}
```

At least one load case is required.

The root `loadCaseId` selects the default case analyzed by the CLI:

```json
{
  "loadCaseId": "G1"
}
```

---

## Loads

Every load has these common properties:

| Property | Required | Meaning |
|---|---:|---|
| `id` | yes | Unique load identifier. |
| `loadCaseId` | yes | Existing load case id. |
| `type` | yes | Load type. |
| `targetType` | yes | `Node` or `Member`. |
| `targetId` | yes | Target node/member id. |
| `direction` | yes | Load direction. |
| `value` | yes | Load value. Unit depends on type. |
| `position` | for member point loads | Normalized position from `0.0` to `1.0`. |
| `endValue` | for linear distributed loads | End value in kN/m. |
| `label` | no | User-facing label. |

Supported directions:

- `GlobalX`
- `GlobalY`
- `LocalX`
- `LocalY`
- `MomentZ`

Supported load types:

- `NodalForce`
- `NodalMoment`
- `UniformDistributedLoad`
- `PointLoadOnMember`
- `LinearDistributedLoad`

### Nodal force

```json
{
  "id": "P1",
  "loadCaseId": "LC1",
  "type": "NodalForce",
  "targetType": "Node",
  "targetId": "B",
  "direction": "GlobalY",
  "value": -20.0,
  "label": "20 kN downward"
}
```

### Nodal moment

```json
{
  "id": "MZ1",
  "loadCaseId": "LC1",
  "type": "NodalMoment",
  "targetType": "Node",
  "targetId": "B",
  "direction": "MomentZ",
  "value": 12.0
}
```

### Uniform distributed load on a member

```json
{
  "id": "Q1",
  "loadCaseId": "LC1",
  "type": "UniformDistributedLoad",
  "targetType": "Member",
  "targetId": "M1",
  "direction": "GlobalY",
  "value": -10.0
}
```

### Point load on a member

`position` is normalized along the member:

```text
0.0 = start node
0.5 = middle
1.0 = end node
```

```json
{
  "id": "P_MID",
  "loadCaseId": "LC1",
  "type": "PointLoadOnMember",
  "targetType": "Member",
  "targetId": "M1",
  "direction": "GlobalY",
  "value": -15.0,
  "position": 0.5
}
```

### Linearly varying distributed load

Use `value` for the start value and `endValue` for the end value.

```json
{
  "id": "Q_TRI",
  "loadCaseId": "LC1",
  "type": "LinearDistributedLoad",
  "targetType": "Member",
  "targetId": "M1",
  "direction": "GlobalY",
  "value": 0.0,
  "endValue": -12.0
}
```

---

## How to define an inclined single force

The current JSON format does not use a direct `angle` property for loads. To apply a single inclined force, decompose it into global components.

If a force `P` acts at an angle `α` measured counterclockwise from the positive global X axis:

```text
Fx = P · cos(α)
Fy = P · sin(α)
```

Then create two `NodalForce` entries on the same node:

```json
{
  "id": "P_X",
  "loadCaseId": "LC1",
  "type": "NodalForce",
  "targetType": "Node",
  "targetId": "B",
  "direction": "GlobalX",
  "value": 8.660254
},
{
  "id": "P_Y",
  "loadCaseId": "LC1",
  "type": "NodalForce",
  "targetType": "Node",
  "targetId": "B",
  "direction": "GlobalY",
  "value": -5.0
}
```

This example represents a 10 kN force acting 30 degrees downward from the positive X direction:

```text
P = 10 kN
α = -30°
Fx = 10 · cos(-30°) = +8.660254 kN
Fy = 10 · sin(-30°) = -5.000000 kN
```

A complete example is available here:

```text
examples/loads/inclined-nodal-force.json
```

Run it with:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\loads\inclined-nodal-force.json
```

The same rule works for an inclined point load on a member: use two `PointLoadOnMember` loads at the same `position`, one in `GlobalX` and one in `GlobalY`.

Example at midspan:

```json
{
  "id": "PM_X",
  "loadCaseId": "LC1",
  "type": "PointLoadOnMember",
  "targetType": "Member",
  "targetId": "M1",
  "direction": "GlobalX",
  "value": 8.660254,
  "position": 0.5
},
{
  "id": "PM_Y",
  "loadCaseId": "LC1",
  "type": "PointLoadOnMember",
  "targetType": "Member",
  "targetId": "M1",
  "direction": "GlobalY",
  "value": -5.0,
  "position": 0.5
}
```

### Alternative: local member directions

For member loads you can also use local directions:

- `LocalX`: along the member axis;
- `LocalY`: perpendicular to the member axis.

This is useful when the load is naturally described relative to the member, for example a load perpendicular to an inclined beam.

```json
{
  "id": "Q_LOCAL",
  "loadCaseId": "LC1",
  "type": "UniformDistributedLoad",
  "targetType": "Member",
  "targetId": "M1",
  "direction": "LocalY",
  "value": -5.0
}
```

---

## Load combinations

Manual load combinations combine existing load cases.

```json
{
  "id": "ULS1",
  "name": "ULS 1",
  "description": "Example ultimate combination",
  "terms": [
    { "loadCaseId": "G1", "factor": 1.35 },
    { "loadCaseId": "Q1", "factor": 1.50 }
  ]
}
```

Root selection:

```json
{
  "loadCombinationId": "ULS1"
}
```

Run explicitly:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- analyze examples\combinations\load-combination.json ULS1
```

---

## Common mistakes

### Missing referenced ids

Every `member.materialId`, `member.sectionId`, `member.startNodeId`, `member.endNodeId`, `support.nodeId`, `load.loadCaseId` and `load.targetId` must reference an existing item.

### Wrong sign for downward loads

A downward vertical force must usually be negative in `GlobalY`:

```json
{ "direction": "GlobalY", "value": -10.0 }
```

### Using degrees directly as a load direction

Loads currently do not have an `angle` property. Use `GlobalX`/`GlobalY` components or `LocalX`/`LocalY` for member loads.

### Forgetting member point-load position

`PointLoadOnMember` requires `position` from `0.0` to `1.0`.

### Unstable supports

A model must have enough restraints to eliminate rigid-body motion. If the reduced stiffness matrix is singular, the structure is probably unstable or under-constrained.

### Units mixed between mm and m

Coordinates and section dimensions are in meters. Convert millimeters before writing the JSON.

Example:

```text
200 mm = 0.20 m
400 mm = 0.40 m
```
