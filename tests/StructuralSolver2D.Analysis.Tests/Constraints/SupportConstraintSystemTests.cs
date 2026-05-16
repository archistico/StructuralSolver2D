using StructuralSolver2D.Analysis.Constraints;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Analysis.Tests.Constraints;

/// <summary>
/// Regression tests for support reaction extraction from global residual components.
/// </summary>
public sealed class SupportConstraintSystemTests
{
    [Fact]
    public void BuildSupportReactionResults_VerticalRoller_ShouldSuppressUnrestrainedHorizontalReaction()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddSupport(StructuralSupport.SimpleSupport("SA", "A"));

        Dictionary<string, int> nodeIndexById = new(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 0,
        };
        double[] globalResidual = { 12.5, -20.0, 3.0 };

        var reactions = SupportConstraintSystem.BuildSupportReactionResults(
            model,
            nodeIndexById,
            dofsPerNode: 3,
            globalResidual,
            includeRotationalDof: true);

        SupportReactionResult reaction = Assert.Single(reactions);
        Assert.Equal(0.0, reaction.Fx, precision: 12);
        Assert.Equal(-20.0, reaction.Fy, precision: 12);
        Assert.Equal(0.0, reaction.Mz, precision: 12);
    }

    [Fact]
    public void BuildSupportReactionResults_HorizontalRoller_ShouldSuppressUnrestrainedVerticalReaction()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddSupport(new StructuralSupport("SA", "A", true, false, false));

        Dictionary<string, int> nodeIndexById = new(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 0,
        };
        double[] globalResidual = { 15.0, -7.5, 2.0 };

        var reactions = SupportConstraintSystem.BuildSupportReactionResults(
            model,
            nodeIndexById,
            dofsPerNode: 3,
            globalResidual,
            includeRotationalDof: true);

        SupportReactionResult reaction = Assert.Single(reactions);
        Assert.Equal(15.0, reaction.Fx, precision: 12);
        Assert.Equal(0.0, reaction.Fy, precision: 12);
        Assert.Equal(0.0, reaction.Mz, precision: 12);
    }

    [Fact]
    public void BuildSupportReactionResults_InclinedVerticalRoller_ShouldKeepBothGlobalComponents()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddSupport(new StructuralSupport("SA", "A", false, true, false, OrientationDegrees: 45.0));

        Dictionary<string, int> nodeIndexById = new(StringComparer.OrdinalIgnoreCase)
        {
            ["A"] = 0,
        };
        double[] globalResidual = { 8.0, -8.0, 0.0 };

        var reactions = SupportConstraintSystem.BuildSupportReactionResults(
            model,
            nodeIndexById,
            dofsPerNode: 3,
            globalResidual,
            includeRotationalDof: true);

        SupportReactionResult reaction = Assert.Single(reactions);
        Assert.Equal(8.0, reaction.Fx, precision: 12);
        Assert.Equal(-8.0, reaction.Fy, precision: 12);
        Assert.Equal(0.0, reaction.Mz, precision: 12);
    }
}
