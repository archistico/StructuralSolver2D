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

        double area = Product(width, height);
        double momentOfInertia = Product(1.0 / 12.0, width, height, height, height);
        ValidateProperties(area, momentOfInertia);

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

        double area = Product(Math.PI / 4.0, diameter, diameter);
        double momentOfInertia = Product(Math.PI / 64.0, diameter, diameter, diameter, diameter);
        ValidateProperties(area, momentOfInertia);

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

        // Factor differences of powers; the diameter gap remains accurate for thin walls.
        double gap = outerDiameter - innerDiameter;
        double ratio = innerDiameter / outerDiameter;
        double area = Product(Math.PI / 4.0, gap, outerDiameter, 1 + ratio);
        double momentOfInertia = Product(Math.PI / 64.0, gap, outerDiameter, outerDiameter, outerDiameter,
            (1 + ratio) * (1 + ratio * ratio));
        ValidateProperties(area, momentOfInertia);

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

    private static double Product(params double[] factors)
    {
        // Multiply normalized mantissas and apply the combined binary exponent once.
        // All factors here are positive and finite, with at most six bounded mantissas.
        double mantissa = 1;
        int exponent = 0;
        foreach (double factor in factors)
        {
            int factorExponent = Math.ILogB(factor);
            mantissa *= Math.ScaleB(factor, -factorExponent);
            exponent += factorExponent;
        }
        return Math.ScaleB(mantissa, exponent);
    }

    private static void ValidateProperties(double area, double momentOfInertia)
    {
        if (area <= 0 || !double.IsFinite(area))
            throw new ArgumentOutOfRangeException(nameof(area), area, "Calculated section area must be representable as a positive finite value.");
        if (momentOfInertia <= 0 || !double.IsFinite(momentOfInertia))
            throw new ArgumentOutOfRangeException(nameof(momentOfInertia), momentOfInertia, "Calculated section inertia must be representable as a positive finite value.");
    }

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (value <= 0.0 || !double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The dimension must be positive and finite.");
        }
    }
}
