namespace StructuralSolver2D.Core.Model.Materials;

using StructuralSolver2D.Core.Model;

/// <summary>
/// Provides predefined elastic material presets for preliminary structural analysis.
/// Elastic modulus values use the internal unit kilonewton per square meter [kN/m²].
/// Unit weights use kilonewton per cubic meter [kN/m³].
/// </summary>
/// <remarks>
/// These presets are analysis conveniences only. They are not complete normative design definitions.
/// Strength classes, partial factors, national annexes, duration factors and durability rules are outside the current scope.
/// </remarks>
public static class StructuralMaterialLibrary
{
    /// <summary>
    /// Creates a structural steel S235 elastic material preset.
    /// </summary>
    /// <param name="id">Optional material identifier. Defaults to <c>S235</c>.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>An elastic structural material preset.</returns>
    public static StructuralMaterial SteelS235(
        string id = "S235",
        string name = "Steel S235") =>
        Create(id, name, elasticModulus: 210_000_000.0, unitWeight: 78.5);

    /// <summary>
    /// Creates a structural steel S275 elastic material preset.
    /// </summary>
    /// <param name="id">Optional material identifier. Defaults to <c>S275</c>.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>An elastic structural material preset.</returns>
    public static StructuralMaterial SteelS275(
        string id = "S275",
        string name = "Steel S275") =>
        Create(id, name, elasticModulus: 210_000_000.0, unitWeight: 78.5);

    /// <summary>
    /// Creates a structural steel S355 elastic material preset.
    /// </summary>
    /// <param name="id">Optional material identifier. Defaults to <c>S355</c>.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>An elastic structural material preset.</returns>
    public static StructuralMaterial SteelS355(
        string id = "S355",
        string name = "Steel S355") =>
        Create(id, name, elasticModulus: 210_000_000.0, unitWeight: 78.5);

    /// <summary>
    /// Creates a timber C24 elastic material preset.
    /// </summary>
    /// <param name="id">Optional material identifier. Defaults to <c>C24</c>.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>An elastic structural material preset.</returns>
    public static StructuralMaterial TimberC24(
        string id = "C24",
        string name = "Timber C24") =>
        Create(id, name, elasticModulus: 11_000_000.0, unitWeight: 4.2);

    /// <summary>
    /// Creates a glulam GL24h elastic material preset.
    /// </summary>
    /// <param name="id">Optional material identifier. Defaults to <c>GL24H</c>.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>An elastic structural material preset.</returns>
    public static StructuralMaterial GlulamGL24h(
        string id = "GL24H",
        string name = "Glulam GL24h") =>
        Create(id, name, elasticModulus: 11_500_000.0, unitWeight: 4.3);

    /// <summary>
    /// Creates a generic normal-weight concrete elastic material preset.
    /// </summary>
    /// <param name="id">Optional material identifier. Defaults to <c>CONCRETE</c>.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>An elastic structural material preset.</returns>
    public static StructuralMaterial GenericConcrete(
        string id = "CONCRETE",
        string name = "Generic concrete") =>
        Create(id, name, elasticModulus: 30_000_000.0, unitWeight: 25.0);

    /// <summary>
    /// Creates a concrete C25/30 elastic material preset.
    /// </summary>
    /// <param name="id">Optional material identifier. Defaults to <c>C25_30</c>.</param>
    /// <param name="name">Optional display name.</param>
    /// <returns>An elastic structural material preset.</returns>
    public static StructuralMaterial ConcreteC25_30(
        string id = "C25_30",
        string name = "Concrete C25/30") =>
        Create(id, name, elasticModulus: 31_000_000.0, unitWeight: 25.0);

    private static StructuralMaterial Create(
        string id,
        string name,
        double elasticModulus,
        double unitWeight)
    {
        ValidateIdentifier(id);
        ValidateName(name);

        return new StructuralMaterial(
            id,
            name,
            elasticModulus,
            unitWeight);
    }

    private static void ValidateIdentifier(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("The material identifier must not be empty.", nameof(id));
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("The material name must not be empty.", nameof(name));
        }
    }
}
