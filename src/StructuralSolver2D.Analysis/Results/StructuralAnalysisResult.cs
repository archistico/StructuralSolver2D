namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Contains the result of a structural analysis for a single load case.
/// </summary>
public sealed class StructuralAnalysisResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralAnalysisResult"/> class.
    /// </summary>
    public StructuralAnalysisResult(
        string loadCaseId,
        IReadOnlyList<NodalDisplacementResult> displacements,
        IReadOnlyList<SupportReactionResult> reactions,
        IReadOnlyList<MemberEndForceResult> memberEndForces)
    {
        LoadCaseId = loadCaseId;
        Displacements = displacements;
        Reactions = reactions;
        MemberEndForces = memberEndForces;
    }

    /// <summary>
    /// Gets the analyzed load case identifier.
    /// </summary>
    public string LoadCaseId { get; }

    /// <summary>
    /// Gets nodal displacement results.
    /// </summary>
    public IReadOnlyList<NodalDisplacementResult> Displacements { get; }

    /// <summary>
    /// Gets support reaction results.
    /// </summary>
    public IReadOnlyList<SupportReactionResult> Reactions { get; }

    /// <summary>
    /// Gets local member end forces.
    /// </summary>
    public IReadOnlyList<MemberEndForceResult> MemberEndForces { get; }

    /// <summary>
    /// Finds the displacement result for the specified node.
    /// </summary>
    public NodalDisplacementResult GetDisplacement(string nodeId) =>
        Displacements.First(result => string.Equals(result.NodeId, nodeId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Finds the reaction result for the specified support.
    /// </summary>
    public SupportReactionResult GetReaction(string supportId) =>
        Reactions.First(result => string.Equals(result.SupportId, supportId, StringComparison.OrdinalIgnoreCase));
}
