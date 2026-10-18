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
- model statistics and a vector geometry/deformation figure;
- assumptions and limitations;
- executive summary with governing values;
- nodal displacements;
- support reactions;
- member end forces;
- sampled internal-force values;
- sampled displacement values.

Long tabular sections are intentionally truncated in the PDF. Full tabular data should be exported through CSV or XLSX.

The A4 layout uses navy and teal accents, numbered sections, a compact statistics strip, three highlighted governing values, and page numbers with the total page count. Tables have dark headers, alternating row backgrounds and right-aligned numeric columns. All graphics and text remain vector content suitable for printing and zooming; no external PDF-generation dependency is required.

The figure overlays the original member geometry in grey with sampled global displacements in teal. Deformation is automatically amplified for readability, as stated in its legend; it is not a displacement scale drawing. Short node identifiers are shown on small models; dense models omit labels. The figure is a schematic of geometry and deformation, without support or load symbols. The highlighted Uy value is explicitly the **nodal** maximum: a simply supported beam can have zero nodal translations and a nonzero span deflection.

Every wrapped line and table row is checked against the remaining page height. Ordinary multiline rows stay together; identifiers longer than a page continue without losing text. Table column headers repeat on subsequent pages, with a section continuation label. Sections retain enough space for their first content lines. Long titles and source labels flow across pages; running headers are shortened while the full title remains in the body.

Horizontal wrapping uses Helvetica advances for text and fixed Courier advances for numeric cells at the actual font size. Identifiers wrap inside their own column instead of shifting neighboring results. Member end forces have separate `Start` and `End` rows with N/V/M units. Absolute summary values are magnitudes; detailed tables keep signed values and six decimal places. `MaxRowsPerSection` and the two sample-section options retain their existing behavior.

Tracked PDF files are marked binary in `.gitattributes`: line-ending normalization would invalidate their byte offsets and stream lengths.

Text uses Windows-1252 with PDF WinAnsi font encoding, preserving Italian and other supported Western European accents, typographic punctuation and the euro sign. Text outside that character set raises `NotSupportedException` instead of silently replacing characters with `?`. Use Markdown or XLSX for other scripts; Unicode font embedding is not implemented.

## Limitations

The PDF currently does not include:

- embedded SVG result figures;
- table of contents;
- digital signatures;
- normative code-check chapters;
- professional calculation-cover templates.

Example PDFs are available in `reports/pdf/simple-supported-beam.pdf` and `reports/pdf/combination.pdf`.
