# Section catalog persistence

Milestone 45 adds a small reusable section catalog layer.

The goal is to make section definitions reusable across models without turning the structural model itself into a database.

## Main classes

Located under `StructuralSolver2D.Core/Model/Sections`:

- `StructuralSectionCatalog`
- `StructuralSectionCatalogJsonSerializer`

The catalog stores ordinary `StructuralSection` records. No new solver-specific section type is introduced.

## JSON format

A catalog JSON file has this shape:

```json
{
  "schemaVersion": 1,
  "title": "Example section catalog",
  "sections": [
    {
      "id": "RECT_200x400",
      "name": "Rectangular 0.2 x 0.4 m",
      "area": 0.08,
      "momentOfInertia": 0.0010666666666666667,
      "height": 0.4,
      "width": 0.2
    }
  ]
}
```

Units remain the internal project units:

- area: square meters `[m²]`;
- second moment of area: meters to the fourth power `[m⁴]`;
- height and width: meters `[m]`.

## Example file

A small demonstration catalog is included at:

```text
examples/sections/basic-sections.json
```

## Basic usage

```csharp
var catalog = new StructuralSectionCatalog()
    .Add(StructuralSectionFactory.Rectangular("RECT_200x400", 0.20, 0.40))
    .Add(StructuralSectionFactory.CircularHollow("CHS_100_80", 0.10, 0.08));

StructuralSectionCatalogJsonSerializer.Save(
    catalog,
    "sections/example-sections.json",
    "Example section catalog");
```

Loading:

```csharp
StructuralSectionCatalog catalog = StructuralSectionCatalogJsonSerializer.Load(
    "sections/example-sections.json");

StructuralSection section = catalog.Find("RECT_200x400");
```

Applying a catalog to a model:

```csharp
catalog.ApplyToModel(model);
```

By default, existing model sections are not replaced. To replace matching ids:

```csharp
catalog.ApplyToModel(model, replaceExisting: true);
```

## Validation rules

The catalog validates:

- non-empty id;
- non-empty name;
- positive finite area;
- non-negative finite moment of inertia (zero is allowed for axial-only sections);
- positive finite optional height;
- positive finite optional width;
- duplicate section ids, matched case-insensitively.

The model validator requires strictly positive inertia whenever a section is assigned to a `Frame2D` member.
Negative and non-finite inertia are rejected for all sections, including those assigned only to `Truss2D` members.

JSON `area` and `momentOfInertia` must be present and non-null. An explicit `"momentOfInertia": 0` remains valid for axial-only sections; omitting or misspelling the property is rejected with its `sections[index]` path. Optional height and width may be null; when supplied, their positive/finite checks agree with the model validator.

The geometric section factories compute products with binary scaling and factor hollow-section formulas to avoid losing thin walls through subtraction of large powers. Calculated area and inertia must be representable as positive finite `double` values; otherwise the factory throws `ArgumentOutOfRangeException`. Positive subnormal properties are preserved when representable. These factory checks concern geometric sections; the catalog still supports explicitly supplied zero inertia for axial-only sections.

## Current scope

This milestone intentionally does not yet add:

- a steel-profile database;
- national-code profile tables;
- material-specific resistance properties;
- GUI editing;
- automatic section selection.

Those features can be layered on top of the persisted catalog format.

## Estensione opzionale M80: geometria parametrica e proprietà di verifica

M80 aggiunge `SectionGeometryFactoryM80`, `SectionGeometricPropertiesM80` e
`SectionProfileCatalogM80`. Sono **aggiuntivi**: il JSON storico con `sections`
e `schemaVersion: 1` rimane invariato. Il nuovo formato con `profiles` e dimensioni
in millimetri è descritto in [M80 — proprietà geometriche](m80-section-geometric-properties.md).
Le figure RHS e I sono idealizzate, senza raccordi; nessun valore commerciale o
normativo è attribuito a queste forme. Esempio:
`examples/sections/m80-parametric-example.json`.
