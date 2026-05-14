# Reporting

StructuralSolver2D currently provides Markdown reporting through `StructuralSolver2D.Reporting.Markdown`.

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

The educational sections are useful for examples, teaching material and validation workflows. They are not a substitute for independent engineering review.

---

## Options

`MarkdownReportOptions` controls optional report content:

- `IncludeEducationalExplanations`;
- `IncludeModelStatistics`;
- `IncludeInternalForceSamples`;
- `IncludeCharacteristicPoints`;
- `IncludeDisplacementSamples`;
- `IncludeDeflectionChecks`.

Sample count limits are available for internal-force samples, characteristic points and displacement samples.

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
