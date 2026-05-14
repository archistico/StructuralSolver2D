using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Model.Generators;

public sealed class ParametricTrussGeneratorTests
{
    [Fact]
    public void PrattBridge_ShouldCreateTwoChordTrussWithAlternatingDiagonals()
    {
        var model = ParametricTrussGenerator.PrattBridge(span: 12.0, height: 2.5, panels: 4);

        Assert.Equal(10, model.Nodes.Count);
        Assert.Equal(17, model.Members.Count);
        Assert.All(model.Members, member => Assert.Equal(MemberType.Truss2D, member.Type));
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void IsostaticTriangularTruss_ShouldCreateThreeMembersAndTwoSupports()
    {
        var model = ParametricTrussGenerator.IsostaticTriangularTruss(span: 6.0, rise: 2.0);

        Assert.Equal(3, model.Nodes.Count);
        Assert.Equal(3, model.Members.Count);
        Assert.Equal(2, model.Supports.Count);
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void NielsenParabolicTruss_ShouldPlaceMiddleChordNodeAtRise()
    {
        var model = ParametricTrussGenerator.NielsenParabolicTruss(span: 10.0, rise: 2.0, panels: 4);

        var middleTopNode = Assert.Single(model.Nodes, node => node.Id == "T2");
        Assert.Equal(5.0, middleTopNode.X, precision: 12);
        Assert.Equal(2.0, middleTopNode.Y, precision: 12);
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void InvertedParabolicTruss_ShouldPlaceMiddleChordNodeBelowDeck()
    {
        var model = ParametricTrussGenerator.InvertedParabolicTruss(span: 10.0, rise: 2.0, panels: 4);

        var middleTopNode = Assert.Single(model.Nodes, node => node.Id == "T2");
        Assert.Equal(-2.0, middleTopNode.Y, precision: 12);
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void DoubleDiagonalTruss_ShouldCreateTwoDiagonalsPerPanel()
    {
        var model = ParametricTrussGenerator.DoubleDiagonalTruss(span: 9.0, height: 2.0, panels: 3);

        Assert.Equal(8, model.Nodes.Count);
        Assert.Equal(16, model.Members.Count);
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }
}
