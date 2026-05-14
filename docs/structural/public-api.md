# Public API

Milestone 37 introduces a stable high-level public API for applications that want to use StructuralSolver2D without manually coordinating every low-level analysis and post-processing component.

The preferred entry point is:

```csharp
using StructuralSolver2D.Analysis.PublicApi;

var service = new StructuralSolver2DService();

StructuralAnalysisOutput output = service.AnalyzeLoadCase(
    model,
    "LC1");
```

For manual load combinations:

```csharp
StructuralAnalysisOutput output = service.AnalyzeLoadCombination(
    model,
    "SLS1");
```

## Result bundle

`StructuralAnalysisOutput` contains:

- `Result`: nodal displacements, support reactions and member end forces;
- `InternalForceDiagrams`: sampled `N`, `V` and `M` diagrams;
- `DisplacementDiagrams`: sampled displacement/deformed-shape diagrams, when requested;
- `DeflectionChecks`: preliminary serviceability deflection checks, when requested;
- `Summary`: compact governing result values.

## Options

Use `StructuralAnalysisOptions` to configure the standard workflow:

```csharp
var options = new StructuralAnalysisOptions
{
    InternalForceSampleCount = 21,
    DisplacementSampleCount = 21,
    IncludeDisplacementDiagrams = true,
    DeflectionLimit = null,
};
```

To run a preliminary deflection check:

```csharp
using StructuralSolver2D.Analysis.Serviceability;

var options = new StructuralAnalysisOptions
{
    DeflectionLimit = new DeflectionLimit(250.0, DeflectionCheckDirection.GlobalY),
};
```

When `DeflectionLimit` is set, displacement diagrams are sampled even if `IncludeDisplacementDiagrams` is `false`, because the check requires sampled displacement values.

## Scope

This facade wraps the current first-order linear elastic plane-structure workflow. It is designed to keep application code stable while internals continue to evolve.

It does not introduce normative design verification. Deflection checks remain preliminary sampled checks of the form `maximum deflection <= L / denominator`.

Low-level classes such as `PlaneStructureAnalyzer`, `Frame2DInternalForceSampler`, `Frame2DDisplacementSampler`, `Frame2DResultSummarizer`, `MarkdownStructuralReportGenerator` and `CsvStructuralResultExporter` remain available for advanced workflows.
