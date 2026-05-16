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

### Support reaction convention

`SupportReactionResult.Fx` and `SupportReactionResult.Fy` are always global force components.
`SupportReactionResult.Mz` is the global support moment component.

For inclined supports, a single restrained local support direction may generate both global components.
Applications should therefore not assume that a roller has only one non-zero reaction component unless the support is aligned with the global axes.

### Solver-size expectation

The current implementation uses a dense linear solver. It is suitable for small/medium educational and prototype models, but it is not intended as a production-scale sparse finite element solver. The public facade is designed so that a future sparse solver can replace the dense implementation without changing application code.

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
