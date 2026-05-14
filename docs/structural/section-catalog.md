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
- positive finite moment of inertia;
- positive finite optional height;
- positive finite optional width;
- duplicate section ids, matched case-insensitively.

## Current scope

This milestone intentionally does not yet add:

- a steel-profile database;
- national-code profile tables;
- material-specific resistance properties;
- GUI editing;
- automatic section selection.

Those features can be layered on top of the persisted catalog format.
