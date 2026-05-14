using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Serviceability;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Stable high-level facade for the standard StructuralSolver2D analysis workflow.
/// </summary>
/// <remarks>
/// This class is intended as the preferred public entry point for applications that want a complete first-order
/// linear elastic 2D analysis bundle without depending on every low-level solver and post-processing component.
/// Low-level classes remain available for advanced usage and tests.
/// </remarks>
public sealed class StructuralSolver2DService
{
    private readonly PlaneStructureAnalyzer analyzer = new();
    private readonly Frame2DInternalForceSampler internalForceSampler = new();
    private readonly Frame2DDisplacementSampler displacementSampler = new();
    private readonly Frame2DResultSummarizer summarizer = new();
    private readonly PreliminaryDeflectionChecker deflectionChecker = new();

    /// <summary>
    /// Analyzes one load case and returns the complete public result bundle.
    /// </summary>
    public StructuralAnalysisOutput AnalyzeLoadCase(
        StructuralModel model,
        string loadCaseId,
        StructuralAnalysisOptions? options = null) =>
        Analyze(StructuralAnalysisRequest.ForLoadCase(model, loadCaseId, options));

    /// <summary>
    /// Analyzes one manual load combination and returns the complete public result bundle.
    /// </summary>
    public StructuralAnalysisOutput AnalyzeLoadCombination(
        StructuralModel model,
        string loadCombinationId,
        StructuralAnalysisOptions? options = null) =>
        Analyze(StructuralAnalysisRequest.ForLoadCombination(model, loadCombinationId, options));

    /// <summary>
    /// Executes the standard public analysis workflow for a load case or a load combination.
    /// </summary>
    public StructuralAnalysisOutput Analyze(StructuralAnalysisRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Model);

        if (string.IsNullOrWhiteSpace(request.TargetId))
        {
            throw new ArgumentException("The analysis target id cannot be empty.", nameof(request));
        }

        StructuralAnalysisOptions options = request.Options ?? new StructuralAnalysisOptions();
        options.Validate();

        StructuralAnalysisResult result = request.TargetKind switch
        {
            StructuralAnalysisTargetKind.LoadCase => analyzer.Analyze(request.Model, request.TargetId),
            StructuralAnalysisTargetKind.LoadCombination => analyzer.AnalyzeCombination(request.Model, request.TargetId),
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.TargetKind, "Unsupported analysis target kind."),
        };

        IReadOnlyList<MemberInternalForceDiagram> internalForceDiagrams = internalForceSampler.SampleAllMembers(
            request.Model,
            result,
            options.InternalForceSampleCount);

        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams = ShouldSampleDisplacements(options)
            ? displacementSampler.SampleAllMembers(request.Model, result, options.DisplacementSampleCount)
            : Array.Empty<MemberDisplacementDiagram>();

        IReadOnlyList<DeflectionCheckResult> deflectionChecks = options.DeflectionLimit is null
            ? Array.Empty<DeflectionCheckResult>()
            : deflectionChecker.Check(displacementDiagrams, options.DeflectionLimit);

        StructuralAnalysisSummary summary = summarizer.Summarize(result, internalForceDiagrams);

        return new StructuralAnalysisOutput(
            result,
            internalForceDiagrams,
            displacementDiagrams,
            deflectionChecks,
            summary);
    }

    private static bool ShouldSampleDisplacements(StructuralAnalysisOptions options) =>
        options.IncludeDisplacementDiagrams || options.DeflectionLimit is not null;
}
