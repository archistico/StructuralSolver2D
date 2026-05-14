namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents the geometric properties of a member section.
/// Internal area unit: square meter [m²].
/// Internal second moment of area unit: meter to the fourth power [m⁴].
/// </summary>
/// <param name="Id">Unique section identifier.</param>
/// <param name="Name">Section display name.</param>
/// <param name="Area">Cross-sectional area A in m².</param>
/// <param name="MomentOfInertia">Second moment of area I in m⁴ for in-plane bending.</param>
/// <param name="Height">Optional section height in meters.</param>
/// <param name="Width">Optional section width in meters.</param>
public sealed record StructuralSection(
    string Id,
    string Name,
    double Area,
    double MomentOfInertia,
    double? Height = null,
    double? Width = null);
