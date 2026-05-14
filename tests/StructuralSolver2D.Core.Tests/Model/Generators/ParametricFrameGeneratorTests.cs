using StructuralSolver2D.Core.Model.Generators;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Model.Generators;

public sealed class ParametricFrameGeneratorTests
{
    [Fact]
    public void PortalFrame_ShouldCreateRigidJointedFrameWithFixedBases()
    {
        var model = ParametricFrameGenerator.PortalFrame(6.0, 3.5);

        Assert.Equal(4, model.Nodes.Count);
        Assert.Equal(3, model.Members.Count);
        Assert.Equal(2, model.Supports.Count);
        Assert.All(model.Members, member => Assert.False(member.ReleaseStartMoment || member.ReleaseEndMoment));

        var leftBase = Assert.Single(model.Supports, support => support.NodeId == "N1");
        Assert.True(leftBase.RestrainedUx);
        Assert.True(leftBase.RestrainedUy);
        Assert.True(leftBase.RestrainedRz);

        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }
}
