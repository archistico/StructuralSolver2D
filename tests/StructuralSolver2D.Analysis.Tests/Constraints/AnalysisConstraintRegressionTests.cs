using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Constraints;

public sealed class AnalysisConstraintRegressionTests
{
    public static IEnumerable<object[]> Mechanisms()
    {
        foreach (string analyzer in new[] { "Frame", "Plane", "Truss" })
        foreach (double angle in new[] { 0, 15, 30, 45, 60, 75 })
        foreach (bool loaded in new[] { false, true })
            yield return new object[] { analyzer, angle, loaded };
    }

    [Theory]
    [MemberData(nameof(Mechanisms))]
    public void Analyze_ShouldRejectMechanismAtEveryAngleEvenWhenUnloaded(string analyzer, double angle, bool loaded)
    {
        var model = Beam(angle).AddSupport(StructuralSupport.Hinge("A", "A"));
        if (analyzer == "Truss") model.Members[0] = model.Members[0] with { Type = MemberType.Truss2D };
        if (loaded) model.AddLoad(StructuralLoad.NodalForce("P", "LC1", "B", StructuralLoadDirection.GlobalY, -10));

        var error = Assert.Throws<StructuralAnalysisException>(() => Analyze(model, analyzer));
        Assert.Contains("singular", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Frame", -4.0, -10.0)]
    [InlineData("Plane", 0.0, -10.0)]
    [InlineData("Truss", 4.0, -10.0)]
    public void IndependentInclinedRollers_ShouldRecoverIndividualReactions(string analyzer, double fx, double fy)
    {
        var model = Beam().AddSupport(StructuralSupport.SimpleSupport("S45", "A", orientationDegrees: 45))
            .AddSupport(StructuralSupport.SimpleSupport("Sm45", "A", orientationDegrees: -45))
            .AddSupport(new StructuralSupport("SR", "A", false, false, true));
        if (analyzer == "Truss")
        {
            model.Members[0] = model.Members[0] with { Type = MemberType.Truss2D };
            model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        }
        if (fx != 0) model.AddLoad(StructuralLoad.NodalForce("PX", "LC1", "B", StructuralLoadDirection.GlobalX, fx));
        model.AddLoad(StructuralLoad.NodalForce("PY", "LC1", analyzer == "Truss" ? "A" : "B", StructuralLoadDirection.GlobalY, fy));

        var result = Analyze(model, analyzer);
        var first = result.GetReaction("S45");
        var second = result.GetReaction("Sm45");
        Assert.Equal((-fx + fy) / 2, first.Fx, precision: 7);
        Assert.Equal((fx - fy) / 2, first.Fy, precision: 7);
        Assert.Equal((-fx - fy) / 2, second.Fx, precision: 7);
        Assert.Equal((-fx - fy) / 2, second.Fy, precision: 7);
        Assert.True(new GlobalEquilibriumChecker().Check(model, result).IsInEquilibrium(1e-7));
    }

    [Theory]
    [InlineData("Frame", 0.0)]
    [InlineData("Frame", 45.0)]
    [InlineData("Plane", 180.0)]
    [InlineData("Plane", 360.0)]
    [InlineData("Truss", -45.0)]
    public void RedundantSupports_ShouldPreserveDisplacementsAndTotalReaction(string analyzer, double angle)
    {
        var model = Beam().AddSupport(StructuralSupport.Fixed("S1", "A"));
        if (analyzer == "Truss")
        {
            model.Members[0] = model.Members[0] with { Type = MemberType.Truss2D };
            model.AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        }
        model.AddLoad(StructuralLoad.NodalForce("PX", "LC1", "B", StructuralLoadDirection.GlobalX, 4));
        model.AddLoad(StructuralLoad.NodalForce("PY", "LC1", "A", StructuralLoadDirection.GlobalY, -10));
        var original = Analyze(model, analyzer);
        model.AddSupport(StructuralSupport.SimpleSupport("EXTRA", "A", orientationDegrees: angle));

        var result = Analyze(model, analyzer);

        foreach (var displacement in original.Displacements)
        {
            var actual = result.GetDisplacement(displacement.NodeId);
            Assert.Equal(displacement.Ux, actual.Ux, precision: 10);
            Assert.Equal(displacement.Uy, actual.Uy, precision: 10);
            Assert.Equal(displacement.Rz, actual.Rz, precision: 10);
        }
        Assert.True(new GlobalEquilibriumChecker().Check(model, result).IsInEquilibrium(1e-7));
        var extra = result.GetReaction("EXTRA");
        double radians = angle * Math.PI / 180;
        Assert.Equal(0, extra.Fx * Math.Cos(radians) + extra.Fy * Math.Sin(radians), precision: 7);
    }

    [Theory]
    [InlineData(1e-8, 0.0)]
    [InlineData(1e-20, 45.0)]
    [InlineData(1.0, 30.0)]
    [InlineData(1e8, 75.0)]
    public void StableCantilever_ShouldMatchAnalyticalDeflectionAcrossStiffnessScales(double scale, double angle)
    {
        var model = Beam(angle).AddSupport(StructuralSupport.Fixed("S1", "A"));
        model.Materials[0] = model.Materials[0] with { ElasticModulus = 210_000_000 * scale };
        double radians = angle * Math.PI / 180;
        // A local transverse tip load, rotated into global coordinates.
        if (angle != 0) model.AddLoad(StructuralLoad.NodalForce("PX", "LC1", "B", StructuralLoadDirection.GlobalX, 10 * Math.Sin(radians)));
        model.AddLoad(StructuralLoad.NodalForce("PY", "LC1", "B", StructuralLoadDirection.GlobalY, -10 * Math.Cos(radians)));

        var result = Analyze(model, "Frame");
        var tip = result.GetDisplacement("B");
        double transverse = -Math.Sin(radians) * tip.Ux + Math.Cos(radians) * tip.Uy;
        double expected = -10 * 125 / (3 * 210_000_000 * scale * .00002);
        Assert.InRange(Math.Abs((transverse - expected) / expected), 0, 1e-7);
        Assert.True(new GlobalEquilibriumChecker().Check(model, result).IsInEquilibrium(1e-6));
    }

    [Theory]
    [InlineData(30.0, -15.0)]
    [InlineData(0.0, 60.0)]
    [InlineData(45.0, 135.0)]
    public void NonOrthogonalRollers_ShouldResolveReactionsInTheirOwnDirections(double firstAngle, double secondAngle)
    {
        var model = Beam().AddSupport(StructuralSupport.SimpleSupport("S1", "A", orientationDegrees: firstAngle))
            .AddSupport(StructuralSupport.SimpleSupport("S2", "A", orientationDegrees: secondAngle))
            .AddSupport(new StructuralSupport("SR", "A", false, false, true))
            .AddLoad(StructuralLoad.NodalForce("PX", "LC1", "B", StructuralLoadDirection.GlobalX, 4))
            .AddLoad(StructuralLoad.NodalForce("PY", "LC1", "B", StructuralLoadDirection.GlobalY, -10));
        double x1 = -Math.Sin(firstAngle * Math.PI / 180), y1 = Math.Cos(firstAngle * Math.PI / 180);
        double x2 = -Math.Sin(secondAngle * Math.PI / 180), y2 = Math.Cos(secondAngle * Math.PI / 180);
        double determinant = x1 * y2 - x2 * y1;
        double first = (-4 * y2 - 10 * x2) / determinant;
        double second = (10 * x1 + 4 * y1) / determinant;

        var result = Analyze(model, "Frame");
        Assert.Equal(first * x1, result.GetReaction("S1").Fx, precision: 7);
        Assert.Equal(first * y1, result.GetReaction("S1").Fy, precision: 7);
        Assert.Equal(second * x2, result.GetReaction("S2").Fx, precision: 7);
        Assert.Equal(second * y2, result.GetReaction("S2").Fy, precision: 7);
        Assert.True(new GlobalEquilibriumChecker().Check(model, result).IsInEquilibrium(1e-7));
    }

    [Fact]
    public void RedundantVerticalRestraint_ShouldSplitReactionByMinimumNormRegardlessOfSupportOrder()
    {
        var model = Beam().AddSupport(StructuralSupport.Fixed("S1", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("S2", "A"))
            .AddLoad(StructuralLoad.NodalForce("PY", "LC1", "B", StructuralLoadDirection.GlobalY, -10));
        var original = Analyze(model, "Frame");
        Assert.Equal(5, original.GetReaction("S1").Fy, precision: 7);
        Assert.Equal(5, original.GetReaction("S2").Fy, precision: 7);
        (model.Supports[0], model.Supports[1]) = (model.Supports[1], model.Supports[0]);
        var reversed = Analyze(model, "Frame");
        Assert.Equal(original.GetReaction("S1").Fy, reversed.GetReaction("S1").Fy, precision: 7);
        Assert.Equal(original.GetReaction("S2").Fy, reversed.GetReaction("S2").Fy, precision: 7);
    }

    private static StructuralModel Beam(double angle = 0) => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0))
        .AddNode(new StructuralNode("B", 5 * Math.Cos(angle * Math.PI / 180), 5 * Math.Sin(angle * Math.PI / 180)))
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
        .AddSection(new StructuralSection("SEC", "Section", .003, .00002))
        .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC"))
        .AddLoadCase(new StructuralLoadCase("LC1", "Load"));

    private static StructuralAnalysisResult Analyze(StructuralModel model, string analyzer) => analyzer switch
    {
        "Frame" => new Frame2DAnalyzer().Analyze(model, "LC1"),
        "Truss" => new Truss2DAnalyzer().Analyze(model, "LC1"),
        _ => new PlaneStructureAnalyzer().Analyze(model, "LC1")
    };
}
