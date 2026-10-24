namespace StructuralSolver2D.Core.Units;

/// <summary>
/// Explicit conversions from common catalogue/engineering units to the model's
/// kN-m-s base system. The JSON reader does NOT infer or silently convert units.
/// </summary>
public static class StructuralUnitConversions
{
    public static double MillimetersToMeters(double value) => ScaleFinite(value, 1e-3);
    public static double CentimetersToMeters(double value) => ScaleFinite(value, 1e-2);
    public static double NewtonsToKilonewtons(double value) => ScaleFinite(value, 1e-3);
    public static double SquareMillimetersToSquareMeters(double value) => ScaleFinite(value, 1e-6);
    public static double FourthPowerMillimetersToFourthPowerMeters(double value) => ScaleFinite(value, 1e-12);
    public static double FourthPowerCentimetersToFourthPowerMeters(double value) => ScaleFinite(value, 1e-8);
    public static double MegaPascalsToKilonewtonsPerSquareMeter(double value) => ScaleFinite(value, 1e3);
    public static double GigaPascalsToKilonewtonsPerSquareMeter(double value) => ScaleFinite(value, 1e6);

    /// <summary>
    /// Converts mass density [kg/m³] into SPECIFIC WEIGHT [kN/m³].
    /// Gravity [m/s²] must be supplied explicitly: unit weight is NOT density.
    /// </summary>
    public static double MassDensityToUnitWeight(double densityKgPerCubicMeter, double gravityMetersPerSecondSquared)
    {
        if (!double.IsFinite(densityKgPerCubicMeter) || densityKgPerCubicMeter <= 0)
            throw new ArgumentOutOfRangeException(nameof(densityKgPerCubicMeter), "Mass density must be finite and positive.");
        if (!double.IsFinite(gravityMetersPerSecondSquared) || gravityMetersPerSecondSquared <= 0)
            throw new ArgumentOutOfRangeException(nameof(gravityMetersPerSecondSquared), "Gravity must be finite and positive.");
        return ScaleFinite(densityKgPerCubicMeter, gravityMetersPerSecondSquared / 1000);
    }

    private static double ScaleFinite(double value, double factor)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), "Input must be finite.");
        double converted = value * factor;
        if (!double.IsFinite(converted))
            throw new ArgumentOutOfRangeException(nameof(value), "Unit conversion overflowed the representable range.");
        return converted;
    }
}
