using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Model.Generators;

public sealed class ParametricBeamGeneratorTests
{
    [Fact]
    public void SimplySupportedBeam_ShouldCreateEqualFrameMembersAndEndSupports()
    {
        var model = ParametricBeamGenerator.SimplySupportedBeam(span: 8.0, divisions: 4);

        Assert.Equal(5, model.Nodes.Count);
        Assert.Equal(4, model.Members.Count);
        Assert.Equal(2, model.Supports.Count);
        Assert.All(model.Members, member => Assert.Equal(MemberType.Frame2D, member.Type));
        Assert.Equal(2.0, model.Nodes[1].X, precision: 12);
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void GerberBeamWithAsymmetricLoads_ShouldCreateInternalMomentReleasesAndLoads()
    {
        var model = ParametricBeamGenerator.GerberBeamWithAsymmetricLoads(4.0, 3.0, 5.0);

        Assert.Equal(4, model.Nodes.Count);
        Assert.Equal(3, model.Members.Count);
        Assert.Equal(3, model.Loads.Count);

        var suspendedMember = Assert.Single(model.Members, member => member.Id == "M2");
        Assert.True(suspendedMember.ReleaseStartMoment);
        Assert.True(suspendedMember.ReleaseEndMoment);

        Assert.All(model.Loads, load => Assert.Equal(StructuralLoadDirection.GlobalY, load.Direction));
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }
}
