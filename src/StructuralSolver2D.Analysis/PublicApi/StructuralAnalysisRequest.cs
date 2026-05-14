using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Describes one stable public analysis request.
/// </summary>
/// <param name="Model">Structural model to analyze.</param>
/// <param name="TargetId">Load case id or load combination id.</param>
/// <param name="TargetKind">Type of analysis target.</param>
/// <param name="Options">Optional workflow options.</param>
public sealed record StructuralAnalysisRequest(
    StructuralModel Model,
    string TargetId,
    StructuralAnalysisTargetKind TargetKind = StructuralAnalysisTargetKind.LoadCase,
    StructuralAnalysisOptions? Options = null)
{
    /// <summary>
    /// Creates a request for one load case.
    /// </summary>
    public static StructuralAnalysisRequest ForLoadCase(
        StructuralModel model,
        string loadCaseId,
        StructuralAnalysisOptions? options = null) =>
        new(model, loadCaseId, StructuralAnalysisTargetKind.LoadCase, options);

    /// <summary>
    /// Creates a request for one manual load combination.
    /// </summary>
    public static StructuralAnalysisRequest ForLoadCombination(
        StructuralModel model,
        string loadCombinationId,
        StructuralAnalysisOptions? options = null) =>
        new(model, loadCombinationId, StructuralAnalysisTargetKind.LoadCombination, options);
}
