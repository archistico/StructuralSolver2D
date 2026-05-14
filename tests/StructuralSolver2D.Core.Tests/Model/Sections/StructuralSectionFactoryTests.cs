using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Core.Tests.Model.Sections;

public sealed class StructuralSectionFactoryTests
{
    [Fact]
    public void Rectangular_ShouldComputeAreaAndMomentOfInertia()
    {
        var section = StructuralSectionFactory.Rectangular("RECT_100x200", 0.10, 0.20);

        Assert.Equal("RECT_100x200", section.Id);
        Assert.Equal(0.020, section.Area, precision: 12);
        Assert.Equal(0.00006666666666666668, section.MomentOfInertia, precision: 15);
        Assert.Equal(0.20, section.Height);
        Assert.Equal(0.10, section.Width);
    }

    [Fact]
    public void TimberRectangular_ShouldComputeAreaAndMomentOfInertiaLikeRectangular()
    {
        var section = StructuralSectionFactory.TimberRectangular("C24_80x240", 0.08, 0.24);

        Assert.Equal(0.0192, section.Area, precision: 12);
        Assert.Equal(0.00009216, section.MomentOfInertia, precision: 12);
        Assert.Contains("Timber", section.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CircularSolid_ShouldComputeAreaAndMomentOfInertia()
    {
        var section = StructuralSectionFactory.CircularSolid("CIRC_100", 0.10);

        double expectedArea = Math.PI * Math.Pow(0.10, 2.0) / 4.0;
        double expectedInertia = Math.PI * Math.Pow(0.10, 4.0) / 64.0;

        Assert.Equal(expectedArea, section.Area, precision: 15);
        Assert.Equal(expectedInertia, section.MomentOfInertia, precision: 15);
        Assert.Equal(0.10, section.Height);
        Assert.Equal(0.10, section.Width);
    }

    [Fact]
    public void CircularHollow_ShouldComputeAreaAndMomentOfInertia()
    {
        var section = StructuralSectionFactory.CircularHollow("CHS_100_80", 0.10, 0.08);

        double expectedArea = Math.PI * (Math.Pow(0.10, 2.0) - Math.Pow(0.08, 2.0)) / 4.0;
        double expectedInertia = Math.PI * (Math.Pow(0.10, 4.0) - Math.Pow(0.08, 4.0)) / 64.0;

        Assert.Equal(expectedArea, section.Area, precision: 15);
        Assert.Equal(expectedInertia, section.MomentOfInertia, precision: 15);
        Assert.Equal(0.10, section.Height);
        Assert.Equal(0.10, section.Width);
    }

    [Theory]
    [InlineData(0.0, 0.20)]
    [InlineData(0.10, 0.0)]
    [InlineData(double.NaN, 0.20)]
    [InlineData(0.10, double.PositiveInfinity)]
    public void Rectangular_ShouldRejectInvalidDimensions(double width, double height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StructuralSectionFactory.Rectangular("INVALID", width, height));
    }

    [Fact]
    public void CircularHollow_ShouldRejectInnerDiameterGreaterThanOrEqualToOuterDiameter()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StructuralSectionFactory.CircularHollow("INVALID", 0.10, 0.10));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            StructuralSectionFactory.CircularHollow("INVALID", 0.10, 0.12));
    }

    [Fact]
    public void FactoryMethods_ShouldRejectEmptyIdentifier()
    {
        Assert.Throws<ArgumentException>(() =>
            StructuralSectionFactory.Rectangular(" ", 0.10, 0.20));
    }
}
