namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Represents an extreme scalar value found in an analysis result.
/// </summary>
/// <param name="EntityId">Identifier of the node, support or member associated with the extreme value.</param>
/// <param name="Value">Extreme value.</param>
public sealed record AnalysisResultExtreme(string EntityId, double Value);
