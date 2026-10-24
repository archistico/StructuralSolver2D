namespace StructuralSolver2D.Core.Dynamics;

/// <summary>
/// Explicit conversions for StructuralSolver2D's kN–m–s unit convention.
/// Translational mass: kN*s²/m (1 unit = 1000 kg).
/// Area mass density: kN*s²/m³. Volumetric mass density: kN*s²/m⁴.
/// Line mass: kN*s²/m². Rotational inertia: kN*s²*m.
/// The standard gravitational acceleration converts weight into inertial mass;
/// it does NOT apply a load to the structure.
/// </summary>
public static class DynamicMassUnits
{
    public const double StandardGravityMetersPerSecondSquared = 9.80665;

    public static double TranslationalMassFromKilograms(double kilograms) =>
        PositiveFiniteResult(kilograms / 1000.0, kilograms, nameof(kilograms));

    public static double RotationalInertiaFromKilogramSquareMeters(double kilogramSquareMeters) =>
        PositiveFiniteResult(kilogramSquareMeters / 1000.0, kilogramSquareMeters, nameof(kilogramSquareMeters));

    public static double TranslationalMassFromWeight(double weightKilonewtons) =>
        PositiveFiniteResult(weightKilonewtons / StandardGravityMetersPerSecondSquared, weightKilonewtons, nameof(weightKilonewtons));

    public static double VolumetricMassDensityFromUnitWeight(double unitWeightKilonewtonsPerCubicMeter) =>
        PositiveFiniteResult(unitWeightKilonewtonsPerCubicMeter / StandardGravityMetersPerSecondSquared,
            unitWeightKilonewtonsPerCubicMeter, nameof(unitWeightKilonewtonsPerCubicMeter));

    public static double LineMassFromUnitWeight(double unitWeightKilonewtonsPerCubicMeter, double areaSquareMeters)
    {
        double volumetricMassDensity = VolumetricMassDensityFromUnitWeight(unitWeightKilonewtonsPerCubicMeter);
        return PositiveFiniteResult(volumetricMassDensity * RequirePositiveFinite(areaSquareMeters, nameof(areaSquareMeters)),
            unitWeightKilonewtonsPerCubicMeter, nameof(unitWeightKilonewtonsPerCubicMeter));
    }

    public static double MemberMassFromUnitWeight(double unitWeightKilonewtonsPerCubicMeter, double areaSquareMeters, double lengthMeters) =>
        PositiveFiniteResult(LineMassFromUnitWeight(unitWeightKilonewtonsPerCubicMeter, areaSquareMeters) *
            RequirePositiveFinite(lengthMeters, nameof(lengthMeters)), lengthMeters, nameof(lengthMeters));

    private static double PositiveFiniteResult(double result, double argument, string argumentName)
    {
        RequirePositiveFinite(argument, argumentName);
        if (result <= 0 || !double.IsFinite(result))
        {
            throw new ArgumentOutOfRangeException(argumentName, "Converted mass must be finite and strictly positive; check scale and units.");
        }
        return result;
    }

    private static double RequirePositiveFinite(double value, string argumentName)
    {
        if (!double.IsFinite(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(argumentName, "Value must be finite and strictly positive.");
        }
        return value;
    }
}
