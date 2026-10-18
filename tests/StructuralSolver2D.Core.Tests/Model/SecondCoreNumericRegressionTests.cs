using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Model.Sections;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Model;

public sealed class SecondCoreNumericRegressionTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(1e155)]
    [InlineData(1e-170)]
    public void Length_ShouldPreserveRepresentableNormAtDifferentScales(double scale)
    {
        var a = new StructuralNode("A", 0, 0); var b = new StructuralNode("B", 3 * scale, 4 * scale);
        Relative(5 * scale, StructuralMember.GetLength(a, b));
    }

    [Fact]
    public void Validation_ShouldHonorTinyCustomTolerance()
    {
        var model = ParametricBeamGenerator.SimplySupportedBeam(5, 1);
        model.Nodes[1] = model.Nodes[1] with { X = 1e-170 };
        Assert.True(new StructuralModelValidator(1e-180).Validate(model).IsValid);
    }

    [Fact]
    public void Validation_ShouldRejectUnrepresentableLength()
    {
        var model = ParametricBeamGenerator.SimplySupportedBeam(5, 1);
        model.Nodes[0] = model.Nodes[0] with { X = -double.MaxValue };
        model.Nodes[1] = model.Nodes[1] with { X = double.MaxValue };
        Assert.Contains(new StructuralModelValidator().Validate(model).Errors, issue => issue.Code == "MEMBER_INVALID_LENGTH");
    }

    [Theory]
    [InlineData("rectangle")]
    [InlineData("timber")]
    [InlineData("solid")]
    [InlineData("hollow")]
    public void Factory_ShouldAvoidIntermediateOverflow(string kind)
    {
        StructuralSection section = kind switch
        {
            "rectangle" => StructuralSectionFactory.Rectangular("S", 1, 1e103),
            "timber" => StructuralSectionFactory.TimberRectangular("S", 1, 1e103),
            "solid" => StructuralSectionFactory.CircularSolid("S", 1e77),
            _ => StructuralSectionFactory.CircularHollow("S", 1e77, 5e76)
        };
        double expected = kind switch
        {
            "rectangle" or "timber" => (1e103 / 12) * 1e103 * 1e103,
            "solid" => (Math.PI / 64) * 1e77 * 1e77 * 1e77 * 1e77,
            _ => (Math.PI / 64) * 1e77 * 1e77 * 1e77 * 1e77 * .9375
        };
        Relative(expected, section.MomentOfInertia);
        Assert.Single(StructuralSectionCatalog.FromSections(new[] { section }).Sections);
    }

    [Theory]
    [InlineData(1e-200, 1e150, 8.333333333333333e248)]
    [InlineData(1e200, 1e-110, 8.333333333333333e-132)]
    public void RectangularFactory_ShouldHandleMixedDimensionScales(double width, double height, double inertia)
    {
        var section = StructuralSectionFactory.Rectangular("S", width, height);
        Relative(inertia, section.MomentOfInertia);
    }

    [Theory]
    [InlineData("rectangle", 1e200)]
    [InlineData("solid", 1e200)]
    [InlineData("hollow", 1e200)]
    [InlineData("rectangle", 1e-200)]
    [InlineData("solid", 1e-200)]
    [InlineData("hollow", 1e-200)]
    [InlineData("rectangle", 1e-90)]
    [InlineData("solid", 1e-90)]
    [InlineData("hollow", 1e-90)]
    public void Factory_ShouldRejectUnrepresentableProperties(string kind, double dimension)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => kind switch
        {
            "rectangle" => StructuralSectionFactory.Rectangular("S", dimension, dimension),
            "solid" => StructuralSectionFactory.CircularSolid("S", dimension),
            _ => StructuralSectionFactory.CircularHollow("S", dimension, dimension / 2)
        });
    }

    [Theory]
    [InlineData("rectangle")]
    [InlineData("solid")]
    [InlineData("hollow")]
    public void Factory_ShouldPreserveRepresentableSubnormalInertia(string kind)
    {
        var section = kind switch
        {
            "rectangle" => StructuralSectionFactory.Rectangular("S", double.Epsilon, 3),
            "solid" => StructuralSectionFactory.CircularSolid("S", 1e-80),
            _ => StructuralSectionFactory.CircularHollow("S", 1e-80, 5e-81)
        };
        double expected = kind switch
        {
            "rectangle" => 2 * double.Epsilon,
            "solid" => (Math.PI / 64) * 1e-80 * 1e-80 * 1e-80 * 1e-80,
            _ => (Math.PI / 64 * .9375) * 1e-80 * 1e-80 * 1e-80 * 1e-80
        };
        Assert.True(section.MomentOfInertia > 0);
        Assert.InRange(Math.Abs(section.MomentOfInertia - expected), 0, double.Epsilon);
        Assert.Single(StructuralSectionCatalog.FromSections(new[] { section }).Sections);
    }

    [Fact]
    public void HollowFactory_ShouldPreserveARepresentableThinWall()
    {
        double outer = 1e80, inner = double.BitDecrement(outer);
        var section = StructuralSectionFactory.CircularHollow("S", outer, inner);
        // At one ULP wall thickness, the thin-ring limit differs by less than a double's
        // rounding error from the exact annulus formula, without subtracting large powers.
        double expected = (Math.PI / 16) * (outer - inner) * outer * outer * outer;
        Relative(expected, section.MomentOfInertia);
    }

    private static void Relative(double expected, double actual)
    {
        Assert.True(double.IsFinite(actual));
        Assert.InRange(Math.Abs(actual / expected - 1), 0, 2e-14);
    }
}
