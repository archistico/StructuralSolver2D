namespace StructuralSolver2D.Core.Model.Sections;

using StructuralSolver2D.Core.Model;

/// <summary>
/// Creates common structural sections from geometric dimensions.
/// All input dimensions use the internal length unit: meter [m].
/// Generated properties use square meter [m²] for area and meter to the fourth power [m⁴] for second moment of area.
/// </summary>
public static class StructuralSectionFactory
{
    /// <summary>
    /// Creates a rectangular section.
    /// The generated bending inertia is I = b h³ / 12, where height is the in-plane bending depth.
    /// </summary>
    /// <param name="id">Unique section identifier.</param>
    /// <param name="width">Section width b in meters.</param>
    /// <param name="height">Section height h in meters.</param>
    /// <param name="name">Optional display name. When omitted, a generated name is used.</param>
    /// <returns>A structural section with area and second moment of area.</returns>
    public static StructuralSection Rectangular(
        string id,
        double width,
        double height,
        string? name = null)
    {
        ValidateIdentifier(id);
        ValidatePositiveFinite(width, nameof(width));
        ValidatePositiveFinite(height, nameof(height));

        double area = width * height;
        double momentOfInertia = width * Math.Pow(height, 3.0) / 12.0;

        return new StructuralSection(
            id,
            name ?? $"Rectangular {width:0.###} x {height:0.###} m",
            area,
            momentOfInertia,
            Height: height,
            Width: width);
    }

    /// <summary>
    /// Creates a simple timber rectangular section.
    /// This helper currently uses the same geometric formula as <see cref="Rectangular"/> and only generates a timber-oriented display name.
    /// </summary>
    /// <param name="id">Unique section identifier.</param>
    /// <param name="width">Section width b in meters.</param>
    /// <param name="height">Section height h in meters.</param>
    /// <param name="name">Optional display name. When omitted, a generated name is used.</param>
    /// <returns>A structural section with area and second moment of area.</returns>
    public static StructuralSection TimberRectangular(
        string id,
        double width,
        double height,
        string? name = null)
    {
        return Rectangular(
            id,
            width,
            height,
            name ?? $"Timber rectangular {width:0.###} x {height:0.###} m");
    }

    /// <summary>
    /// Creates a solid circular section.
    /// The generated bending inertia is I = π d⁴ / 64.
    /// </summary>
    /// <param name="id">Unique section identifier.</param>
    /// <param name="diameter">Outer diameter d in meters.</param>
    /// <param name="name">Optional display name. When omitted, a generated name is used.</param>
    /// <returns>A structural section with area and second moment of area.</returns>
    public static StructuralSection CircularSolid(
        string id,
        double diameter,
        string? name = null)
    {
        ValidateIdentifier(id);
        ValidatePositiveFinite(diameter, nameof(diameter));

        double area = Math.PI * Math.Pow(diameter, 2.0) / 4.0;
        double momentOfInertia = Math.PI * Math.Pow(diameter, 4.0) / 64.0;

        return new StructuralSection(
            id,
            name ?? $"Circular solid Ø {diameter:0.###} m",
            area,
            momentOfInertia,
            Height: diameter,
            Width: diameter);
    }

    /// <summary>
    /// Creates a hollow circular section.
    /// The generated bending inertia is I = π (D⁴ - d⁴) / 64.
    /// </summary>
    /// <param name="id">Unique section identifier.</param>
    /// <param name="outerDiameter">Outer diameter D in meters.</param>
    /// <param name="innerDiameter">Inner diameter d in meters.</param>
    /// <param name="name">Optional display name. When omitted, a generated name is used.</param>
    /// <returns>A structural section with area and second moment of area.</returns>
    public static StructuralSection CircularHollow(
        string id,
        double outerDiameter,
        double innerDiameter,
        string? name = null)
    {
        ValidateIdentifier(id);
        ValidatePositiveFinite(outerDiameter, nameof(outerDiameter));
        ValidatePositiveFinite(innerDiameter, nameof(innerDiameter));

        if (innerDiameter >= outerDiameter)
        {
            throw new ArgumentOutOfRangeException(
                nameof(innerDiameter),
                innerDiameter,
                "The inner diameter must be smaller than the outer diameter.");
        }

        double area = Math.PI * (Math.Pow(outerDiameter, 2.0) - Math.Pow(innerDiameter, 2.0)) / 4.0;
        double momentOfInertia = Math.PI * (Math.Pow(outerDiameter, 4.0) - Math.Pow(innerDiameter, 4.0)) / 64.0;

        return new StructuralSection(
            id,
            name ?? $"Circular hollow Ø {outerDiameter:0.###}/{innerDiameter:0.###} m",
            area,
            momentOfInertia,
            Height: outerDiameter,
            Width: outerDiameter);
    }

    private static void ValidateIdentifier(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("The section identifier must not be empty.", nameof(id));
        }
    }

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (value <= 0.0 || !double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The dimension must be positive and finite.");
        }
    }
}
