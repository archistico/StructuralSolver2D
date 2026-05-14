# CSV export

Milestone 36 adds spreadsheet-friendly CSV export through `StructuralSolver2D.Reporting.Csv`.

CSV export is intentionally a reporting/post-processing feature. It receives already computed analysis results, sampled diagrams and summaries. It does not run the solver and does not modify model data.

---

## Main API

```csharp
var exporter = new CsvStructuralResultExporter();

string displacements = exporter.ExportNodalDisplacements(result);
string reactions = exporter.ExportSupportReactions(result);
string memberEndForces = exporter.ExportMemberEndForces(result);
string internalForceSamples = exporter.ExportInternalForceSamples(result.LoadCaseId, diagrams);
string displacementSamples = exporter.ExportDisplacementSamples(result.LoadCaseId, displacementDiagrams);
string summary = exporter.ExportSummary(summary);
```

All numeric values are written with invariant culture and `.` as decimal separator.

The default separator is comma `,`.

CSV fields are quoted only when required, for example when an identifier contains a comma, a quote or a newline.

---

## CLI usage

The CLI can write a set of CSV files into an output directory:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\beams\simple-supported-beam.json reports\csv\simple-supported-beam
```

Optional load case or combination id:

```powershell
dotnet run --project src\StructuralSolver2D.Cli -- export-csv examples\combinations\load-combination.json reports\csv\combination ULS1
```

Generated files:

```text
nodal-displacements.csv
support-reactions.csv
member-end-forces.csv
internal-force-samples.csv
displacement-samples.csv
summary.csv
```

---

## Exported tables

### Nodal displacements

Columns:

```text
LoadCaseId,NodeId,Ux_m,Uy_m,Rz_rad
```

### Support reactions

Columns:

```text
LoadCaseId,SupportId,NodeId,Fx_kN,Fy_kN,Mz_kNm
```

### Member end forces

Columns:

```text
LoadCaseId,MemberId,StartAxial_kN,StartShear_kN,StartMoment_kNm,EndAxial_kN,EndShear_kN,EndMoment_kNm
```

### Internal-force samples

Columns:

```text
AnalysisId,MemberId,Position,Distance_m,NormalForce_kN,ShearForce_kN,BendingMoment_kNm
```

### Displacement samples

Columns:

```text
AnalysisId,MemberId,Position,Distance_m,LocalUx_m,LocalUy_m,LocalRz_rad,GlobalUx_m,GlobalUy_m
```

### Summary

Columns:

```text
AnalysisId,Quantity,EntityId,MemberId,Position,Distance_m,Value,Unit
```

---

## Scope and limitations

CSV export is meant for:

- spreadsheet validation;
- external post-processing;
- plotting diagrams outside the solver;
- checking benchmark values manually.

It is not a database format and is not intended to preserve the complete structural model.

Use JSON input files for model exchange and reproducible analysis inputs.
