using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Model.Generators;

public sealed class ParametricGeneratorRegressionTests
{
    public static IEnumerable<object[]> InvalidDimensions()
    {
        (string Generator, string Parameter)[] inputs =
        {
            ("Beam", "span"), ("Gerber", "leftSpan"), ("Gerber", "suspendedSpan"), ("Gerber", "rightSpan"),
            ("Portal", "bayWidth"), ("Portal", "height"), ("Pratt", "span"), ("Pratt", "height"),
            ("Triangle", "span"), ("Triangle", "rise"), ("Nielsen", "span"), ("Nielsen", "rise"),
            ("Inverted", "span"), ("Inverted", "rise"), ("Double", "span"), ("Double", "height")
        };
        foreach (var input in inputs)
        foreach (double value in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            yield return new object[] { input.Generator, input.Parameter, value };
    }

    [Theory]
    [MemberData(nameof(InvalidDimensions))]
    public void Generate_ShouldRejectInvalidDimension(string generator, string parameter, double value)
    {
        double Dimension(string name, double fallback) => parameter == name ? value : fallback;
        StructuralModel Generate() => generator switch
        {
            "Beam" => ParametricBeamGenerator.SimplySupportedBeam(Dimension("span", 10), 4),
            "Gerber" => ParametricBeamGenerator.GerberBeamWithAsymmetricLoads(Dimension("leftSpan", 4), Dimension("suspendedSpan", 3), Dimension("rightSpan", 5)),
            "Portal" => ParametricFrameGenerator.PortalFrame(Dimension("bayWidth", 6), Dimension("height", 3)),
            "Pratt" => ParametricTrussGenerator.PrattBridge(Dimension("span", 10), Dimension("height", 2), 4),
            "Triangle" => ParametricTrussGenerator.IsostaticTriangularTruss(Dimension("span", 10), Dimension("rise", 2)),
            "Nielsen" => ParametricTrussGenerator.NielsenParabolicTruss(Dimension("span", 10), Dimension("rise", 2), 4),
            "Inverted" => ParametricTrussGenerator.InvertedParabolicTruss(Dimension("span", 10), Dimension("rise", 2), 4),
            "Double" => ParametricTrussGenerator.DoubleDiagonalTruss(Dimension("span", 10), Dimension("height", 2), 4),
            _ => throw new InvalidOperationException(generator)
        };

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => Generate());
        Assert.Equal(parameter, error.ParamName);
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    [InlineData(false, 6)]
    [InlineData(false, 8)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    [InlineData(true, 5)]
    [InlineData(true, 6)]
    [InlineData(true, 8)]
    public void ParabolicTruss_ShouldShareEndNodesWithoutDuplicateMembers(bool inverted, int panels)
    {
        var model = inverted
            ? ParametricTrussGenerator.InvertedParabolicTruss(10, 2, panels)
            : ParametricTrussGenerator.NielsenParabolicTruss(10, 2, panels);

        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
        Assert.Equal(model.Nodes.Count, model.Nodes.Select(node => (node.X, node.Y)).Distinct().Count());
        var connections = model.Members.Select(member => string.CompareOrdinal(member.StartNodeId, member.EndNodeId) < 0
            ? (member.StartNodeId, member.EndNodeId) : (member.EndNodeId, member.StartNodeId));
        Assert.Equal(model.Members.Count, connections.Distinct().Count());
        Assert.Equal(2 * model.Nodes.Count - 3, model.Members.Count);
        foreach (var support in model.Supports)
            Assert.True(model.Members.Count(member => member.StartNodeId == support.NodeId || member.EndNodeId == support.NodeId) >= 2);
    }
}
