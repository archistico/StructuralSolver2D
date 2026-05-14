namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents a load case used to group compatible loads.
/// Examples: permanent loads, variable loads, wind loads or manually defined cases.
/// </summary>
/// <param name="Id">Unique load case identifier.</param>
/// <param name="Name">Load case display name.</param>
/// <param name="Description">Optional user-facing description.</param>
public sealed record StructuralLoadCase(
    string Id,
    string Name,
    string? Description = null);
