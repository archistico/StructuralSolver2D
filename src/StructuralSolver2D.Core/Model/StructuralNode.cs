namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents a structural node in the global XY plane.
/// Internal length unit: meter [m].
/// </summary>
/// <param name="Id">Unique node identifier.</param>
/// <param name="X">Global X coordinate in meters.</param>
/// <param name="Y">Global Y coordinate in meters.</param>
/// <param name="Label">Optional user-facing label.</param>
public sealed record StructuralNode(
    string Id,
    double X,
    double Y,
    string? Label = null);
