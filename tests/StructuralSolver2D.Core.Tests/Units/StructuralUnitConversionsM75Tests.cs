using StructuralSolver2D.Core.Units;

namespace StructuralSolver2D.Core.Tests.Units;

public sealed class StructuralUnitConversionsM75Tests
{
    [Fact]
    public void CatalogueUnitConversions_AreDimensionallyConsistent()
    {
        Assert.Equal(3.5, StructuralUnitConversions.MillimetersToMeters(3500), 12);
        Assert.Equal(0.03, StructuralUnitConversions.CentimetersToMeters(3), 12);
        Assert.Equal(-18, StructuralUnitConversions.NewtonsToKilonewtons(-18000), 12);
        Assert.Equal(0.009, StructuralUnitConversions.SquareMillimetersToSquareMeters(9000), 12);
        Assert.Equal(8.34e-6, StructuralUnitConversions.FourthPowerCentimetersToFourthPowerMeters(834), 15);
        Assert.Equal(8.34e-6, StructuralUnitConversions.FourthPowerMillimetersToFourthPowerMeters(8_340_000), 15);
        Assert.Equal(210_000_000, StructuralUnitConversions.MegaPascalsToKilonewtonsPerSquareMeter(210_000), 8);
        Assert.Equal(11_000_000, StructuralUnitConversions.GigaPascalsToKilonewtonsPerSquareMeter(11), 8);
    }

    [Fact]
    public void MassDensity_RequiresExplicitGravityToDeriveUnitWeight()
    {
        double gamma = StructuralUnitConversions.MassDensityToUnitWeight(480, 9.80665);
        Assert.Equal(4.707192, gamma, 9);
    }

    [Fact]
    public void NaNInfinityAndInvalidDensityAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralUnitConversions.MillimetersToMeters(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralUnitConversions.NewtonsToKilonewtons(double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralUnitConversions.MassDensityToUnitWeight(0, 9.81));
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralUnitConversions.MassDensityToUnitWeight(500, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuralUnitConversions.MassDensityToUnitWeight(500, double.NaN));
    }
}
