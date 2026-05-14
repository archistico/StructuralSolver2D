using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Tests.Model;

public sealed class StructuralLoadTests
{
    [Fact]
    public void NodalForce_ShouldCreateGlobalNodalForce()
    {
        StructuralLoad load = StructuralLoad.NodalForce(
            "L1",
            "LC1",
            "A",
            StructuralLoadDirection.GlobalY,
            -10.0);

        Assert.Equal("L1", load.Id);
        Assert.Equal("LC1", load.LoadCaseId);
        Assert.Equal(StructuralLoadType.NodalForce, load.Type);
        Assert.Equal(StructuralLoadTargetType.Node, load.TargetType);
        Assert.Equal("A", load.TargetId);
        Assert.Equal(StructuralLoadDirection.GlobalY, load.Direction);
        Assert.Equal(-10.0, load.Value);
        Assert.Null(load.Position);
    }

    [Fact]
    public void NodalMoment_ShouldCreateMomentZLoad()
    {
        StructuralLoad load = StructuralLoad.NodalMoment("MZ1", "LC1", "A", 12.5);

        Assert.Equal(StructuralLoadType.NodalMoment, load.Type);
        Assert.Equal(StructuralLoadTargetType.Node, load.TargetType);
        Assert.Equal(StructuralLoadDirection.MomentZ, load.Direction);
        Assert.Equal(12.5, load.Value);
    }

    [Fact]
    public void UniformDistributedLoad_ShouldCreateMemberLoadWithoutPosition()
    {
        StructuralLoad load = StructuralLoad.UniformDistributedLoad(
            "Q1",
            "LC1",
            "M1",
            StructuralLoadDirection.GlobalY,
            -5.0);

        Assert.Equal(StructuralLoadType.UniformDistributedLoad, load.Type);
        Assert.Equal(StructuralLoadTargetType.Member, load.TargetType);
        Assert.Equal("M1", load.TargetId);
        Assert.Equal(StructuralLoadDirection.GlobalY, load.Direction);
        Assert.Equal(-5.0, load.Value);
        Assert.Null(load.Position);
    }

    [Fact]
    public void PointLoadOnMember_ShouldCreateMemberLoadWithNormalizedPosition()
    {
        StructuralLoad load = StructuralLoad.PointLoadOnMember(
            "P1",
            "LC1",
            "M1",
            StructuralLoadDirection.LocalY,
            -20.0,
            0.5);

        Assert.Equal(StructuralLoadType.PointLoadOnMember, load.Type);
        Assert.Equal(StructuralLoadTargetType.Member, load.TargetType);
        Assert.Equal("M1", load.TargetId);
        Assert.Equal(StructuralLoadDirection.LocalY, load.Direction);
        Assert.Equal(-20.0, load.Value);
        Assert.Equal(0.5, load.Position);
    }

    [Fact]
    public void LinearDistributedLoad_ShouldCreateMemberLoadWithStartAndEndValues()
    {
        StructuralLoad load = StructuralLoad.LinearDistributedLoad(
            "T1",
            "LC1",
            "M1",
            StructuralLoadDirection.GlobalY,
            0.0,
            -10.0);

        Assert.Equal(StructuralLoadType.LinearDistributedLoad, load.Type);
        Assert.Equal(StructuralLoadTargetType.Member, load.TargetType);
        Assert.Equal("M1", load.TargetId);
        Assert.Equal(StructuralLoadDirection.GlobalY, load.Direction);
        Assert.Equal(0.0, load.Value);
        Assert.Equal(-10.0, load.EndValue);
        Assert.Null(load.Position);
    }
}
