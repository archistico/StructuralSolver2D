using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Serviceability;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Contains the complete result bundle produced by the stable public analysis facade.
/// </summary>
/// <param name="Result">Nodal displacements, reactions and member end forces.</param>
/// <param name="InternalForceDiagrams">Sampled member internal-force diagrams.</param>
/// <param name="DisplacementDiagrams">Sampled member displacement diagrams, when requested.</param>
/// <param name="DeflectionChecks">Preliminary deflection checks, when requested.</param>
/// <param name="Summary">Compact governing result summary.</param>
public sealed record StructuralAnalysisOutput(
    StructuralAnalysisResult Result,
    IReadOnlyList<MemberInternalForceDiagram> InternalForceDiagrams,
    IReadOnlyList<MemberDisplacementDiagram> DisplacementDiagrams,
    IReadOnlyList<DeflectionCheckResult> DeflectionChecks,
    StructuralAnalysisSummary Summary);
