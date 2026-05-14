using StructuralSolver2D.Analysis.Results;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Builds compact result summaries from Frame2D analysis results and sampled internal-force diagrams.
/// </summary>
public sealed class Frame2DResultSummarizer
{
    /// <summary>
    /// Summarizes displacements, reactions and internal-force extrema for one load case.
    /// </summary>
    /// <param name="analysisResult">Analysis result to summarize.</param>
    /// <param name="diagrams">Sampled internal-force diagrams belonging to the analysis result.</param>
    /// <returns>A compact result summary.</returns>
    public StructuralAnalysisSummary Summarize(
        StructuralAnalysisResult analysisResult,
        IReadOnlyList<MemberInternalForceDiagram> diagrams)
    {
        ArgumentNullException.ThrowIfNull(analysisResult);
        ArgumentNullException.ThrowIfNull(diagrams);

        IReadOnlyList<MemberInternalForceExtrema> memberExtrema = diagrams
            .Select(MemberInternalForceExtrema.FromDiagram)
            .ToList();

        return new StructuralAnalysisSummary(
            analysisResult.LoadCaseId,
            FindMaxAbsDisplacement(analysisResult, displacement => displacement.Ux),
            FindMaxAbsDisplacement(analysisResult, displacement => displacement.Uy),
            FindMaxAbsDisplacement(analysisResult, displacement => displacement.Rz),
            FindMaxAbsReaction(analysisResult, reaction => reaction.Fx),
            FindMaxAbsReaction(analysisResult, reaction => reaction.Fy),
            FindMaxAbsReaction(analysisResult, reaction => reaction.Mz),
            memberExtrema);
    }

    private static AnalysisResultExtreme FindMaxAbsDisplacement(
        StructuralAnalysisResult result,
        Func<NodalDisplacementResult, double> valueSelector)
    {
        if (result.Displacements.Count == 0)
        {
            return new AnalysisResultExtreme(string.Empty, 0.0);
        }

        NodalDisplacementResult extreme = result.Displacements.MaxBy(displacement => Math.Abs(valueSelector(displacement)))!;
        return new AnalysisResultExtreme(extreme.NodeId, valueSelector(extreme));
    }

    private static AnalysisResultExtreme FindMaxAbsReaction(
        StructuralAnalysisResult result,
        Func<SupportReactionResult, double> valueSelector)
    {
        if (result.Reactions.Count == 0)
        {
            return new AnalysisResultExtreme(string.Empty, 0.0);
        }

        SupportReactionResult extreme = result.Reactions.MaxBy(reaction => Math.Abs(valueSelector(reaction)))!;
        return new AnalysisResultExtreme(extreme.SupportId, valueSelector(extreme));
    }
}
