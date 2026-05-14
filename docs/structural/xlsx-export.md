# XLSX report export

Milestone 43 adds a lightweight `.xlsx` export for structural analysis results.

The goal is not to replace the Markdown/PDF technical report. The workbook is intended for:

- spreadsheet-based review;
- quick filtering/sorting of result tables;
- external checks in Excel or LibreOffice;
- sharing numeric results with other tools.

## Implementation strategy

The exporter writes a minimal OpenXML workbook directly through `System.IO.Compression`.

This keeps the project:

- independent from commercial spreadsheet libraries;
- free from additional NuGet dependencies;
- easy to test as a deterministic ZIP/OpenXML package.

## Main class

```csharp
using StructuralSolver2D.Reporting.Xlsx;

byte[] workbook = new XlsxStructuralResultExporter().Export(
    result,
    internalForceDiagrams,
    displacementDiagrams,
    summary);
```

The caller decides where to save the returned byte array.

## Workbook sheets

The generated workbook currently contains:

```text
Summary
Nodal displacements
Support reactions
Member end forces
Internal force samples
Displacement samples
```

## CLI usage

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-xlsx examples\beams\simple-supported-beam.json reports\xlsx\simple-supported-beam.xlsx
```

With an explicit load case or combination:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-xlsx examples\combinations\load-combination.json reports\xlsx\combination.xlsx ULS1
```

## Intentional limitations

The first XLSX export is deliberately simple.

It does not yet include:

- charts;
- formulas;
- images or embedded SVG diagrams;
- professional report layout;
- model input sheets;
- section/material catalogs.

These can be added in later reporting milestones once the core workbook format is stable.
