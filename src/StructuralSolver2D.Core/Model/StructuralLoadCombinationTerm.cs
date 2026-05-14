namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents one factored load case inside a manual load combination.
/// </summary>
/// <param name="LoadCaseId">Identifier of the referenced load case.</param>
/// <param name="Factor">Multiplication factor applied to all loads belonging to the load case.</param>
public sealed record StructuralLoadCombinationTerm(
    string LoadCaseId,
    double Factor);
