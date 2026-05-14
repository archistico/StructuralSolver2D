# PDF technical report

Milestone 44 adds a lightweight PDF technical report exporter.

The exporter is intended for a compact engineering-oriented computation report that can be shared or archived without requiring spreadsheet software.

## Current implementation

The PDF export is implemented in:

```text
src/StructuralSolver2D.Reporting/Pdf
```

Main classes:

- `PdfTechnicalReportExporter`
- `PdfTechnicalReportOptions`

The implementation writes a minimal PDF document directly and does not introduce external PDF-generation dependencies.

## CLI usage

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-pdf examples\beams\simple-supported-beam.json reports\pdf\simple-supported-beam.pdf
```

With explicit load case or combination:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-pdf examples\combinations\load-combination.json reports\pdf\combination.pdf ULS1
```

## Report content

The current PDF contains:

- title, source and analysis id;
- model overview;
- assumptions and limitations;
- executive summary with governing values;
- nodal displacements;
- support reactions;
- member end forces;
- sampled internal-force values;
- sampled displacement values.

Long tabular sections are intentionally truncated in the PDF. Full tabular data should be exported through CSV or XLSX.

## Limitations

This first PDF milestone intentionally does not include:

- embedded SVG result figures;
- table of contents;
- rich typographic styling;
- digital signatures;
- normative code-check chapters;
- professional calculation-cover templates.

Those features can be introduced later once the reporting structure stabilizes.
