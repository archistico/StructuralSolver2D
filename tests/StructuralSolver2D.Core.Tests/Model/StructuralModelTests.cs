using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Tests.Model;

public sealed class StructuralModelTests
{
    [Fact]
    public void AddMethods_ShouldBuildSimpleSupportedBeamModel()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 5.0, 0.0))
            .AddMaterial(new StructuralMaterial("S235", "Steel S235", 210_000_000.0))
            .AddSection(new StructuralSection("GENERIC", "Generic beam", 0.003, 0.00002))
            .AddMember(new StructuralMember("M1", "A", "B", "S235", "GENERIC"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));

        Assert.Equal(2, model.Nodes.Count);
        Assert.Single(model.Members);
        Assert.Single(model.Materials);
        Assert.Single(model.Sections);
        Assert.Equal(2, model.Supports.Count);
        Assert.Equal(MemberType.Frame2D, model.Members[0].Type);
    }

    [Fact]
    public void GetLength_ShouldReturnDistanceBetweenMemberNodes()
    {
        StructuralNode startNode = new("A", 0.0, 0.0);
        StructuralNode endNode = new("B", 3.0, 4.0);

        double length = StructuralMember.GetLength(startNode, endNode);

        Assert.Equal(5.0, length, precision: 10);
    }

    [Fact]
    public void SupportFactories_ShouldCreateExpectedRestraints()
    {
        StructuralSupport hinge = StructuralSupport.Hinge("S1", "A");
        StructuralSupport simpleSupport = StructuralSupport.SimpleSupport("S2", "B");
        StructuralSupport fixedSupport = StructuralSupport.Fixed("S3", "C");

        Assert.True(hinge.RestrainedUx);
        Assert.True(hinge.RestrainedUy);
        Assert.False(hinge.RestrainedRz);
        Assert.Equal(SupportType.Hinge, hinge.Type);

        Assert.False(simpleSupport.RestrainedUx);
        Assert.True(simpleSupport.RestrainedUy);
        Assert.False(simpleSupport.RestrainedRz);
        Assert.Equal(SupportType.SimpleSupport, simpleSupport.Type);

        Assert.True(fixedSupport.RestrainedUx);
        Assert.True(fixedSupport.RestrainedUy);
        Assert.True(fixedSupport.RestrainedRz);
        Assert.Equal(SupportType.Fixed, fixedSupport.Type);
    }
}
