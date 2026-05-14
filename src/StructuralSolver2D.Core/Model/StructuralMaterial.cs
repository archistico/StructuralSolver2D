namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents an elastic structural material.
/// Internal elastic modulus unit: kilonewton per square meter [kN/m²].
/// Internal density unit, when used: kilonewton per cubic meter [kN/m³].
/// </summary>
/// <param name="Id">Unique material identifier.</param>
/// <param name="Name">Material display name.</param>
/// <param name="ElasticModulus">Young's modulus E in kN/m².</param>
/// <param name="UnitWeight">Optional unit weight in kN/m³.</param>
public sealed record StructuralMaterial(
    string Id,
    string Name,
    double ElasticModulus,
    double? UnitWeight = null);
