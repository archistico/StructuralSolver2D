namespace StructuralSolver2D.Core.Model.Sections;

using StructuralSolver2D.Core.Model;

/// <summary>
/// Exact geometric formulas for idealized symmetric shapes with sharp corners.
/// No fillets, weld details, tolerances, effective widths or normative capacities.
/// All input dimensions are in meters; the local in-plane section depth is height.
/// </summary>
public static class SectionGeometryFactoryM80
{
    public static SectionGeometricPropertiesM80 Rectangular(string id, double width, double height, string? name = null)
    {
        StructuralSection section = StructuralSectionFactory.Rectangular(id, width, height, name);
        double iy = height * width * width * width / 12.0;
        return Build(section, "Rectangular", iy);
    }

    public static SectionGeometricPropertiesM80 TimberRectangular(string id, double width, double height, string? name = null)
    {
        StructuralSection section = StructuralSectionFactory.TimberRectangular(id, width, height, name);
        double iy = height * width * width * width / 12.0;
        return Build(section, "TimberRectangular", iy);
    }

    public static SectionGeometricPropertiesM80 CircularSolid(string id, double diameter, string? name = null)
    {
        StructuralSection section = StructuralSectionFactory.CircularSolid(id, diameter, name);
        return Build(section, "CircularSolid", section.MomentOfInertia);
    }

    public static SectionGeometricPropertiesM80 CircularHollow(string id, double outerDiameter, double innerDiameter, string? name = null)
    {
        StructuralSection section = StructuralSectionFactory.CircularHollow(id, outerDiameter, innerDiameter, name);
        return Build(section, "CircularHollow", section.MomentOfInertia);
    }

    public static SectionGeometricPropertiesM80 RectangularHollow(
        string id, double width, double height, double wallThickness, string? name = null)
    {
        CheckIdentifier(id);
        Positive(width, nameof(width));
        Positive(height, nameof(height));
        Positive(wallThickness, nameof(wallThickness));
        if (wallThickness >= width / 2.0 || wallThickness >= height / 2.0)
            throw new ArgumentOutOfRangeException(nameof(wallThickness), "The rectangular hollow section needs a positive interior width and height.");

        double clearHeight = height - 2.0 * wallThickness;
        // Split into two full-width flanges and two clear-height side walls.
        // This avoids cancellation of two nearly equal fourth powers for thin walls.
        double area = 2.0 * wallThickness * (width + clearHeight);
        double flangeDistance = (height - wallThickness) / 2.0;
        double sideDistance = (width - wallThickness) / 2.0;
        double iz = 2.0 * (width * Math.Pow(wallThickness, 3) / 12.0
                    + width * wallThickness * flangeDistance * flangeDistance)
                    + wallThickness * Math.Pow(clearHeight, 3) / 6.0;
        double iy = wallThickness * Math.Pow(width, 3) / 6.0
                    + 2.0 * (clearHeight * Math.Pow(wallThickness, 3) / 12.0
                    + clearHeight * wallThickness * sideDistance * sideDistance);
        var section = new StructuralSection(id, name ?? $"Idealized RHS {width:0.###} x {height:0.###} m", area, iz, height, width);
        return Build(section, "RectangularHollow", iy);
    }

    public static SectionGeometricPropertiesM80 DoublySymmetricI(
        string id, double width, double height, double webThickness, double flangeThickness, string? name = null)
    {
        CheckIdentifier(id);
        Positive(width, nameof(width));
        Positive(height, nameof(height));
        Positive(webThickness, nameof(webThickness));
        Positive(flangeThickness, nameof(flangeThickness));
        if (webThickness > width)
            throw new ArgumentOutOfRangeException(nameof(webThickness), "Web thickness cannot exceed flange width.");
        if (flangeThickness >= height / 2.0)
            throw new ArgumentOutOfRangeException(nameof(flangeThickness), "The I section needs a positive clear web height.");

        double clearHeight = height - 2.0 * flangeThickness;
        double area = 2.0 * width * flangeThickness + webThickness * clearHeight;
        double flangeDistance = (height - flangeThickness) / 2.0;
        double iz = 2.0 * (width * Math.Pow(flangeThickness, 3) / 12.0
                   + width * flangeThickness * flangeDistance * flangeDistance)
                   + webThickness * Math.Pow(clearHeight, 3) / 12.0;
        double iy = flangeThickness * Math.Pow(width, 3) / 6.0
                   + clearHeight * Math.Pow(webThickness, 3) / 12.0;
        var section = new StructuralSection(id, name ?? $"Idealized I {width:0.###} x {height:0.###} m", area, iz, height, width);
        return Build(section, "DoublySymmetricI", iy);
    }

    private static SectionGeometricPropertiesM80 Build(StructuralSection section, string shape, double otherInertia)
    {
        double? h = section.Height;
        double? b = section.Width;
        if (h is null || b is null)
            throw new InvalidOperationException("Geometry dimensions are required to derive elastic section moduli.");

        Positive(section.Area, nameof(section.Area));
        Positive(section.MomentOfInertia, nameof(section.MomentOfInertia));
        Positive(otherInertia, nameof(otherInertia));
        double wz = 2.0 * section.MomentOfInertia / h.Value;
        double wy = 2.0 * otherInertia / b.Value;
        double rz = Math.Sqrt(section.MomentOfInertia / section.Area);
        double ry = Math.Sqrt(otherInertia / section.Area);
        Positive(wz, nameof(wz));
        Positive(wy, nameof(wy));
        Positive(rz, nameof(rz));
        Positive(ry, nameof(ry));
        return new SectionGeometricPropertiesM80(section, shape, otherInertia, wz, wy, rz, ry);
    }

    private static void CheckIdentifier(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Section identifier is required.", nameof(id));
    }

    private static void Positive(double value, string name)
    {
        if (value <= 0.0 || !double.IsFinite(value))
            throw new ArgumentOutOfRangeException(name, value, "Section dimensions and derived properties must be positive and finite.");
    }
}
