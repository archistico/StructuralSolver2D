namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Identifies whether a public analysis request targets a load case or a manual load combination.
/// </summary>
public enum StructuralAnalysisTargetKind
{
    /// <summary>
    /// Analyze one load case by id.
    /// </summary>
    LoadCase,

    /// <summary>
    /// Analyze one manual load combination by id.
    /// </summary>
    LoadCombination,
}
