using StructuralSolver2D.Core.Dynamics;

namespace StructuralSolver2D.Core.Tests.Dynamics;

public sealed class DynamicMassUnitsTests
{
    [Fact]
    public void KilogramsAndKilogramSquareMeters_ConvertToKiloNewtonSecondUnits()
    {
        Assert.Equal(1.0, DynamicMassUnits.TranslationalMassFromKilograms(1000.0), 12);
        Assert.Equal(2.0, DynamicMassUnits.RotationalInertiaFromKilogramSquareMeters(2000.0), 12);
    }

    [Fact]
    public void ReferenceWeight_EqualsOneInternalMassUnit()
    {
        Assert.Equal(1.0, DynamicMassUnits.TranslationalMassFromWeight(9.80665), 12);
    }

    [Fact]
    public void UnitWeight_Area_Length_ProduceCoherentMass()
    {
        const double gamma = 78.4532; // 8000 kg/m³ times g0
        const double area = 0.005;
        const double length = 5.0;
        Assert.Equal(8.0, DynamicMassUnits.VolumetricMassDensityFromUnitWeight(gamma), 10);
        Assert.Equal(0.04, DynamicMassUnits.LineMassFromUnitWeight(gamma, area), 12);
        Assert.Equal(0.2, DynamicMassUnits.MemberMassFromUnitWeight(gamma, area, length), 12);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void NonPositiveOrNonFiniteInputs_AreRejected(double invalid)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.TranslationalMassFromKilograms(invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.RotationalInertiaFromKilogramSquareMeters(invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.TranslationalMassFromWeight(invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.VolumetricMassDensityFromUnitWeight(invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.LineMassFromUnitWeight(25.0, invalid));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.MemberMassFromUnitWeight(25.0, 0.01, invalid));
    }

    [Fact]
    public void OverflowAndUnderflow_AreRejectedRatherThanSilentlyClamped()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.MemberMassFromUnitWeight(1e300, 1e100, 1e100));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.LineMassFromUnitWeight(1e-300, 1e-300));
        Assert.Throws<ArgumentOutOfRangeException>(() => DynamicMassUnits.TranslationalMassFromKilograms(double.Epsilon));
    }
}
