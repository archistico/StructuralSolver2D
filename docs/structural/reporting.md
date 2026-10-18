# Reporting

StructuralSolver2D currently provides Markdown reporting through `StructuralSolver2D.Reporting.Markdown` and CSV result export through `StructuralSolver2D.Reporting.Csv`.

The report generator is intentionally a presentation layer. It receives model data, analysis results, sampled diagrams, summaries and optional preliminary check results. It must not run the solver itself.

---

## Current Markdown report content

The Markdown report can include:

- report header and source label;
- educational `How to read this report` guidance;
- internal unit table;
- executive summary;
- model-size statistics;
- model tables for nodes, materials, sections, members, supports, load cases, load combinations and loads;
- nodal displacements;
- support reactions;
- local member end forces;
- maximum absolute result summary;
- sampled internal-force diagrams;
- characteristic internal-force points;
- sampled deformed-shape values;
- optional preliminary serviceability deflection checks;
- notes and limitations.

---

## Educational scope

The report is designed to help users understand and debug the analysis.

It explains the meaning of common result symbols such as `Ux`, `Uy`, `Rz`, `N`, `V` and `M`, and reminds the reader that member diagrams are expressed along the local member axis from the start node to the end node.

The support table identifies translational restraints as local `Ux`/`Uy` directions and includes `Orientation [deg]`, measured counterclockwise from the global axes. Nodal displacements and support reactions use global coordinates. This distinguishes supports with identical restraint flags but different mechanical orientations.

The educational sections are useful for examples, teaching material and validation workflows. They are not a substitute for independent engineering review.

---

## Options

`MarkdownReportOptions` controls optional report content:

- `DetailLevel` (`MarkdownReportDetail.Full` or `Summary`);
- `IncludeEducationalExplanations`;
- `IncludeModelStatistics`;
- `IncludeInternalForceSamples`;
- `IncludeCharacteristicPoints`;
- `IncludeDisplacementSamples`;
- `IncludeDeflectionChecks`.

Sample count limits are available for internal-force samples, characteristic points and displacement samples.

`Full` remains the API default. `Summary` retains units, the selected case or combination and its factors, all governing absolute values and locations, signed nodal displacements, reactions and member end forces, requested deflection checks, and limitations. It omits model tables, sampled appendices and the repeated absolute-value table. It explicitly identifies the omitted data.

The CLI commands `report` and its `markdown` alias select `Summary`. Use `report-full input.json output.md [loadCaseId|combinationId]` for the complete profile, including displacement samples.

`IncludeModelStatistics` controls only the model-size subsection; the executive summary remains available. `IncludeDeflectionChecks` controls both the check table and its executive summary. Sample flags and limits apply to the full profile.

User identifiers and labels containing pipes, backticks, HTML characters or line breaks are escaped so they stay inside their table cells. Absolute-value summaries use non-negative magnitudes; detailed result tables preserve the signs from the analysis.

---

## Preliminary deflection checks

When `DeflectionCheckResult` values are supplied, the report includes a table with:

- member id;
- checked direction;
- selected limit in the form `L/denominator`;
- reference length;
- allowed deflection;
- maximum sampled deflection;
- signed critical deflection;
- critical distance along the member;
- utilization ratio;
- pass/fail status.

These checks are preliminary sampled checks only. They are not complete code-compliant serviceability verification.


---

## CSV export

Milestone 36 adds `CsvStructuralResultExporter` for spreadsheet-friendly exports of computed results.

Available CSV tables include:

- nodal displacements;
- support reactions;
- local member end forces;
- internal-force samples;
- displacement/deformed-shape samples;
- compact result summaries.

See `docs/structural/csv-export.md` for the full table layout and CLI usage.
