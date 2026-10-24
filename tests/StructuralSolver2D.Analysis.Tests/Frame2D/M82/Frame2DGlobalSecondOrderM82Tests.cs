using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D.SecondOrder;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D.M82;

/// <summary>M82 first-order-prestress global tangent benchmarks; not design checks.</summary>
public sealed class Frame2DGlobalSecondOrderM82Tests
{
    private const double E = 210_000_000d;
    private const double A = 0.006d;
    private const double I = 0.00002d;
    private const double L = 4d;
    private const double H = 2d;

    [Fact]
    public void SingleColumn_GlobalTangentAgreesWithIndependentM81TwoDofReference()
    {
        const double compression = 180d;
        var result = new Frame2DGlobalSecondOrderM82().Analyze(Column(1, compression, H), "LC");
        var isolated = Frame2DInitialStressM81.SolveCantileverTipLoad(E, A, I, L, -compression, -H);
        // Vertical column local transverse axis = negative global X.
        Close(-isolated.TipTransverseDisplacement, result.GetDisplacement("N1").Ux, 1e-10);
        Close(isolated.TipRotation, result.GetDisplacement("N1").Rz, 1e-10);
        Close(-compression, result.MemberAxialReferences.Single().AxialForce, 1e-10);
        Assert.True(result.GetDisplacement("N1").Ux > result.FirstOrder.GetDisplacement("N1").Ux);
        Assert.NotNull(result.CriticalLoadMultiplier);
        Assert.True(result.CriticalLoadMultiplier > 1d);
    }

    [Fact]
    public void ZeroAxialReference_ExactlyRecoversFirstOrderAcrossTwoMembers()
    {
        var model = Column(2, 0d, H);
        var result = new Frame2DGlobalSecondOrderM82().Analyze(model, "LC");
        Assert.Null(result.CriticalLoadMultiplier);
        foreach (var node in model.Nodes)
        {
            var baseline = result.FirstOrder.GetDisplacement(node.Id);
            var secondOrder = result.GetDisplacement(node.Id);
            Close(baseline.Ux, secondOrder.Ux, 1e-10);
            Close(baseline.Uy, secondOrder.Uy, 1e-10);
            Close(baseline.Rz, secondOrder.Rz, 1e-10);
        }
        Assert.All(result.MemberAxialReferences, item => Close(0d, item.AxialForce, 1e-9));
    }

    [Fact]
    public void TwoElementColumn_RefinesBucklingLoadTowardEulerAndPreservesReactionEquilibrium()
    {
        const double compression = 200d;
        var one = new Frame2DGlobalSecondOrderM82().Analyze(Column(1, compression, H), "LC");
        var two = new Frame2DGlobalSecondOrderM82().Analyze(Column(2, compression, H), "LC");
        double euler = Math.PI * Math.PI * E * I / (4d * L * L);
        double oneCritical = compression * one.CriticalLoadMultiplier!.Value;
        double twoCritical = compression * two.CriticalLoadMultiplier!.Value;
        Assert.True(Math.Abs(twoCritical - euler) < Math.Abs(oneCritical - euler));
        Assert.InRange(twoCritical / euler, 0.999d, 1.001d);
        Assert.All(two.MemberAxialReferences, item => Close(-compression, item.AxialForce, 1e-8));
        Assert.True(two.GetDisplacement("N2").Ux > two.FirstOrder.GetDisplacement("N2").Ux);
        // Global force equilibrium is preserved by the tangent residual reactions.
        Close(-H, two.Reactions.Single().Fx, 1e-8);
        Close(compression, two.Reactions.Single().Fy, 1e-8);
    }

    [Fact]
    public void NearAndBeyondCriticalCompression_FailsClosedBeforeMisleadingDisplacement()
    {
        double EI = E * I;
        double x = (5.2d - Math.Sqrt(5.2d * 5.2d - 4d * 0.15d * 12d)) / (2d * 0.15d);
        double discreteCritical = x * EI / (L * L);
        var near = new Frame2DGlobalSecondOrderM82().Analyze(Column(1, discreteCritical * 0.95d, H), "LC");
        Assert.True(near.CriticalLoadMultiplier!.Value > 1d);
        var unstable = Assert.Throws<StructuralAnalysisException>(() =>
            new Frame2DGlobalSecondOrderM82().Analyze(Column(1, discreteCritical * 1.01d, H), "LC"));
        Assert.Contains("M82_LINEARIZED_INSTABILITY", unstable.Message);
    }

    [Fact]
    public void Tension_StiffensLateralResponseAndReportsNoCompressiveBuckling()
    {
        var neutral = new Frame2DGlobalSecondOrderM82().Analyze(Column(1, 0d, H), "LC");
        var tension = new Frame2DGlobalSecondOrderM82().Analyze(Column(1, -180d, H), "LC");
        Assert.True(tension.GetDisplacement("N1").Ux < neutral.GetDisplacement("N1").Ux);
        Close(+180d, tension.MemberAxialReferences.Single().AxialForce, 1e-10);
        Assert.Null(tension.CriticalLoadMultiplier);
    }

    [Fact]
    public void RepeatedCalls_AreDeterministicAndDoNotModifyTheModel()
    {
        var model = Column(2, 120d, H);
        int originalLoads = model.Loads.Count;
        var solver = new Frame2DGlobalSecondOrderM82();
        var first = solver.Analyze(model, "LC");
        var second = solver.Analyze(model, "LC");
        Close(first.GetDisplacement("N2").Ux, second.GetDisplacement("N2").Ux, 1e-12);
        Close(first.CriticalLoadMultiplier!.Value, second.CriticalLoadMultiplier!.Value, 1e-12);
        Assert.Equal(originalLoads, model.Loads.Count);
    }

    [Fact]
    public void UnsupportedScope_IsRejectedExplicitlyInsteadOfSilentlyIgnoringActions()
    {
        var baseModel = Column(1, 50d, H);
        Assert.Contains("M82_UNSUPPORTED_LOAD", Assert.Throws<StructuralAnalysisException>(() =>
            new Frame2DGlobalSecondOrderM82().Analyze(baseModel.AddLoad(
                StructuralLoad.UniformDistributedLoad("UDL", "LC", "M1", StructuralLoadDirection.GlobalY, -1d)), "LC")).Message);
        Assert.Contains("M82_UNSUPPORTED_MEMBER", Assert.Throws<StructuralAnalysisException>(() =>
            new Frame2DGlobalSecondOrderM82().Analyze(Column(1, 50d, H, released: true), "LC")).Message);
        Assert.Contains("M82_UNSUPPORTED_SUPPORT", Assert.Throws<StructuralAnalysisException>(() =>
            new Frame2DGlobalSecondOrderM82().Analyze(Column(1, 50d, H, spring: true), "LC")).Message);
    }

    [Fact]
    public void PortalFrame_UsesAllElementAxialForcesAndExposesPublicOptInFacade()
    {
        const double p = 100d;
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("STEEL", "Steel", E))
            .AddSection(new StructuralSection("S", "Section", A, I))
            .AddLoadCase(new StructuralLoadCase("LC", "Portal"))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 5, 0))
            .AddNode(new StructuralNode("C", 0, 4))
            .AddNode(new StructuralNode("D", 5, 4))
            .AddMember(new StructuralMember("LEFT", "A", "C", "STEEL", "S"))
            .AddMember(new StructuralMember("BEAM", "C", "D", "STEEL", "S"))
            .AddMember(new StructuralMember("RIGHT", "B", "D", "STEEL", "S"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"))
            .AddLoad(StructuralLoad.NodalForce("P1", "LC", "C", StructuralLoadDirection.GlobalY, -p))
            .AddLoad(StructuralLoad.NodalForce("P2", "LC", "D", StructuralLoadDirection.GlobalY, -p))
            .AddLoad(StructuralLoad.NodalForce("H", "LC", "C", StructuralLoadDirection.GlobalX, 2));
        var result = new StructuralSolver2DService().AnalyzeFrameSecondOrder(model, "LC");
        Assert.Equal(3, result.MemberAxialReferences.Count);
        Assert.Contains(result.MemberAxialReferences, item => item.AxialForce < -1);
        Assert.True(result.CriticalLoadMultiplier > 1d);
        Assert.True(result.GetDisplacement("C").Ux > result.FirstOrder.GetDisplacement("C").Ux);
        Close(-2d, result.Reactions.Sum(item => item.Fx), 1e-7);
        Close(2d * p, result.Reactions.Sum(item => item.Fy), 1e-7);
    }

    private static StructuralModel Column(int elements, double compressiveForce, double transverseForce,
        bool released = false, bool spring = false)
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("STEEL", "Steel", E))
            .AddSection(new StructuralSection("S", "Section", A, I))
            .AddLoadCase(new StructuralLoadCase("LC", "Reference"));
        for (int i = 0; i <= elements; i++)
            model.AddNode(new StructuralNode($"N{i}", 0d, L * i / elements));
        for (int i = 0; i < elements; i++)
            model.AddMember(new StructuralMember($"M{i + 1}", $"N{i}", $"N{i + 1}",
                "STEEL", "S", MemberType.Frame2D, ReleaseEndMoment: released));
        model.AddSupport(spring
            ? new StructuralSupport("FIX", "N0", true, true, true, StiffnessUx: 10d)
            : StructuralSupport.Fixed("FIX", "N0"));
        if (compressiveForce != 0d)
            model.AddLoad(StructuralLoad.NodalForce("P", "LC", $"N{elements}", StructuralLoadDirection.GlobalY, -compressiveForce));
        if (transverseForce != 0d)
            model.AddLoad(StructuralLoad.NodalForce("H", "LC", $"N{elements}", StructuralLoadDirection.GlobalX, transverseForce));
        return model;
    }

    private static void Close(double expected, double actual, double tolerance)
    {
        Assert.True(double.IsFinite(actual) && Math.Abs(expected - actual) <= tolerance * Math.Max(1d, Math.Abs(expected)),
            $"Expected {expected:G17}, actual {actual:G17} (tolerance {tolerance:G17}).");
    }
}
