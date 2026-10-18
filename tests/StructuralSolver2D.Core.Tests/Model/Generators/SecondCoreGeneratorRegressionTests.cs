using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Model.Generators;

public sealed class SecondCoreGeneratorRegressionTests
{
    [Theory]
    [InlineData("beam")]
    [InlineData("pratt")]
    [InlineData("double")]
    [InlineData("nielsen")]
    [InlineData("inverted")]
    public void Generator_ShouldKeepFiniteLargeCoordinates(string type)
    {
        StructuralModel model = type switch
        {
            "beam" => ParametricBeamGenerator.SimplySupportedBeam(1e308, 2),
            "pratt" => ParametricTrussGenerator.PrattBridge(1e308, 2, 2),
            "double" => ParametricTrussGenerator.DoubleDiagonalTruss(1e308, 2, 2),
            "nielsen" => ParametricTrussGenerator.NielsenParabolicTruss(10, 5e307, 2),
            _ => ParametricTrussGenerator.InvertedParabolicTruss(10, 5e307, 2)
        };
        Assert.All(model.Nodes, node => Assert.True(double.IsFinite(node.X) && double.IsFinite(node.Y)));
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
        if (type is "nielsen" or "inverted") Assert.Equal(type == "nielsen" ? 5e307 : -5e307, model.Nodes.Single(node => node.Id == "T1").Y);
        else Assert.Equal(1e308, model.Nodes.Max(node => node.X));
    }

    [Theory]
    [InlineData(1e308, 1e308, 1)]
    [InlineData(1e16, 1, 5)]
    public void Gerber_ShouldRejectUnrepresentableSpanAccumulation(double left, double middle, double right)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ParametricBeamGenerator.GerberBeamWithAsymmetricLoads(left, middle, right));
    }

    [Theory]
    [InlineData("beam")]
    [InlineData("pratt")]
    [InlineData("parabola")]
    public void Generator_ShouldRejectRoundedCoincidentNodes(string type)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => type switch
        {
            "beam" => ParametricBeamGenerator.SimplySupportedBeam(double.Epsilon, 2),
            "pratt" => ParametricTrussGenerator.PrattBridge(double.Epsilon, 2, 2),
            _ => ParametricTrussGenerator.NielsenParabolicTruss(10, double.Epsilon, 10)
        });
    }
}
