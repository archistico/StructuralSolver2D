using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Integration;

namespace StructuralSolver2D.Verification.Tests.M87;

public sealed class FemAffineUtilizationExtremaC2ATests
{
    [Fact]
    public void CentralPointLoadAndConstantAxial_ExactEndpointEnvelopeForSyntheticConvexMetric()
    {
        // L=4m, P=12 kN at 2m, RA=RB=6 kN, Mmax=12 kNm.
        // Concurrent axial N=-9 kN. At x=2±: eta=9/9+6/6+12/12=3.
        // No distributed load: within each half-member N/V constant and M affine.
        var result = FemAffineUtilizationExtremaC2A.Locate(Beam(), Request());

        Assert.Equal(AffineUtilizationExtremaStatusC2A.LocatedWithinAffineAssumptions, result.Status);
        Assert.Equal("C2A_AFFINE_DIAGNOSTIC_LOCATED", result.ReasonCode);
        Assert.NotNull(result.Maximum);
        Assert.Equal(3.0, result.Maximum!.MetricValue, 8);
        Assert.Equal(2.0, result.Maximum.DistanceMetres, 8);
        Assert.Contains(result.Candidates, c => c.DistanceMetres == 2 &&
            c.SectionSide == SectionLimitM78.BeforePointLoad && Math.Abs(c.MetricValue - 3) < 1e-9);
        Assert.Contains(result.Candidates, c => c.DistanceMetres == 2 &&
            c.SectionSide == SectionLimitM78.AfterPointLoad && Math.Abs(c.MetricValue - 3) < 1e-9);
        Assert.False(result.NormativeRuleEvaluated);
        Assert.False(result.MemberAdequacyEstablished);
        Assert.False(result.WholeStructureVerified);
        Assert.False(result.CompleteActionSetEstablished);
    }

    [Fact]
    public void NegativeCombinationFactor_PreservesSignsAndFindsOpposingJumpLimits()
    {
        StructuralModel model = Beam()
            .AddLoadCase(new StructuralLoadCase("Q", "Upward"))
            .AddLoad(StructuralLoad.PointLoadOnMember("up", "Q", "M", StructuralLoadDirection.GlobalY, 4, .5))
            .AddLoadCombination(new StructuralLoadCombination("NEG", "G minus Q", new[]
            {
                new StructuralLoadCombinationTerm("G", 1),
                new StructuralLoadCombinationTerm("Q", -1)
            }));
        var request = Request() with
        {
            TargetKind = StructuralAnalysisTargetKind.LoadCombination,
            TargetId = "NEG",
            ShearScaleKn = 8,
            BendingScaleKnm = 16
        };
        var result = FemAffineUtilizationExtremaC2A.Locate(model, request);

        Assert.Equal(AffineUtilizationExtremaStatusC2A.LocatedWithinAffineAssumptions, result.Status);
        Assert.Equal(3.0, result.Maximum!.MetricValue, 8);
        Assert.Equal(2.0, result.Maximum.DistanceMetres, 8);
    }

    [Fact]
    public void MultipleMemberPointLoads_KeepBothSidesOfEveryJump()
    {
        StructuralModel model = Beam()
            .AddLoad(StructuralLoad.PointLoadOnMember("at_one", "G", "M",
                StructuralLoadDirection.GlobalY, -4, .25))
            .AddLoad(StructuralLoad.PointLoadOnMember("at_three", "G", "M",
                StructuralLoadDirection.GlobalY, 4, .75));
        var result = FemAffineUtilizationExtremaC2A.Locate(model, Request());

        Assert.Equal(AffineUtilizationExtremaStatusC2A.LocatedWithinAffineAssumptions, result.Status);
        foreach (double x in new[] { 1d, 2d, 3d })
        {
            Assert.Contains(result.Candidates, c => c.DistanceMetres == x &&
                c.SectionSide == SectionLimitM78.BeforePointLoad);
            Assert.Contains(result.Candidates, c => c.DistanceMetres == x &&
                c.SectionSide == SectionLimitM78.AfterPointLoad);
        }
        Assert.NotNull(result.Maximum);
        Assert.False(result.NormativeRuleEvaluated);
    }

    [Theory]
    [InlineData(StructuralLoadType.UniformDistributedLoad)]
    [InlineData(StructuralLoadType.LinearDistributedLoad)]
    public void DistributedLoadOnAuditedMember_RequiresScreeningIncomplete(StructuralLoadType type)
    {
        StructuralModel model = Beam();
        model.AddLoad(type == StructuralLoadType.UniformDistributedLoad
            ? StructuralLoad.UniformDistributedLoad("dist", "G", "M", StructuralLoadDirection.GlobalY, -2)
            : StructuralLoad.LinearDistributedLoad("dist", "G", "M", StructuralLoadDirection.GlobalY, -1, -3));

        var result = FemAffineUtilizationExtremaC2A.Locate(model, Request());
        Assert.Equal(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete, result.Status);
        Assert.Equal("C2A_DISTRIBUTED_LOAD_UNSUPPORTED", result.ReasonCode);
        Assert.Null(result.Maximum);
        Assert.Empty(result.Candidates);
    }

    [Fact]
    public void ModelWideSelfWeight_RequiresScreeningIncompleteEvenIfPointLoadOtherwiseSupported()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.SelfWeightLoad("sw", "G"));
        var result = FemAffineUtilizationExtremaC2A.Locate(model, Request());
        Assert.Equal(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete, result.Status);
        Assert.Equal("C2A_DISTRIBUTED_LOAD_UNSUPPORTED", result.ReasonCode);
        Assert.Null(result.Maximum);
    }

    [Fact]
    public void WrongTargetKind_AndInvalidCapacity_AreRejected()
    {
        Assert.Equal(AffineUtilizationExtremaStatusC2A.InvalidInput,
            FemAffineUtilizationExtremaC2A.Locate(Beam(), Request() with
            {
                TargetKind = StructuralAnalysisTargetKind.LoadCombination
            }).Status);
        Assert.Equal(AffineUtilizationExtremaStatusC2A.InvalidInput,
            FemAffineUtilizationExtremaC2A.Locate(Beam(), Request() with
            {
                AxialScaleKn = 0
            }).Status);
        Assert.Equal(AffineUtilizationExtremaStatusC2A.InvalidInput,
            FemAffineUtilizationExtremaC2A.Locate(Beam(), Request() with
            {
                BendingScaleKnm = double.NaN
            }).Status);
    }

    [Fact]
    public void TrussMemberCannotBePassedThroughFrameMetric()
    {
        StructuralModel truss = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
            .AddSection(new StructuralSection("SEC", "Axial", .003, 0))
            .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("G", "Force"))
            .AddLoad(StructuralLoad.NodalForce("n", "G", "B", StructuralLoadDirection.GlobalX, 5));
        var result = FemAffineUtilizationExtremaC2A.Locate(truss, Request());
        Assert.Equal(AffineUtilizationExtremaStatusC2A.ScreeningIncomplete, result.Status);
        Assert.Equal("C2A_NONFRAME_UNSUPPORTED", result.ReasonCode);
        Assert.Null(result.Maximum);
    }

    [Fact]
    public void AffineAbsoluteProduct_AnalyticInternalMaximumBeatsBothEndValues()
    {
        // Independent analytic golden: a(t)=t, b(t)=1-t;
        // |a*b| max = 1/4 at t=1/2, while BOTH endpoints are zero.
        (double x, double max) = AffineUtilizationMathC2A.MaximumAbsoluteProduct(0, 1, 1, 0);
        Assert.Equal(.5, x, 12);
        Assert.Equal(.25, max, 12);
        Assert.Equal(0, Math.Abs(0d * 1d));
        Assert.Equal(0, Math.Abs(1d * 0d));
    }

    [Theory]
    [InlineData(3d, 3d, -2d, -2d, 6d)]
    [InlineData(0d, 0d, 0d, 0d, 0d)]
    [InlineData(-1d, 1d, 1d, -1d, 1d)]
    public void AffineAbsoluteProduct_ExactIndependentBoundaryAndDegenerateGoldens(
        double a0, double a1, double b0, double b1, double expected)
    {
        var actual = AffineUtilizationMathC2A.MaximumAbsoluteProduct(a0, a1, b0, b1);
        Assert.Equal(expected, actual.AbsoluteMaximum, 12);
    }

    [Fact]
    public void AffineAbsoluteProduct_NonfiniteInputsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AffineUtilizationMathC2A.MaximumAbsoluteProduct(0, double.NaN, 1, 0));
        Assert.Throws<OverflowException>(() =>
            AffineUtilizationMathC2A.MaximumAbsoluteProduct(1e308, -1e308, 1, 1));
    }

    [Fact]
    public void ProductMetricOnPointLoadedBeam_RemainsOnlyADiagnostic()
    {
        var result = FemAffineUtilizationExtremaC2A.Locate(Beam(), Request() with
        {
            Metric = AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct
        });
        Assert.Equal(AffineUtilizationExtremaStatusC2A.LocatedWithinAffineAssumptions, result.Status);
        Assert.Equal(1, result.Maximum!.MetricValue, 8);
        Assert.Equal(2, result.Maximum.DistanceMetres, 8);
        Assert.False(result.NormativeRuleEvaluated);
        Assert.False(result.MemberAdequacyEstablished);
    }

    private static FemAffineUtilizationRequestC2A Request() =>
        new("M", StructuralAnalysisTargetKind.LoadCase, "G",
            AffineDiagnosticMetricC2A.AbsoluteNormalizedForceSum, 9, 6, 12);

    private static StructuralModel Beam() => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0))
        .AddNode(new StructuralNode("B", 4, 0))
        .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000, 78.5))
        .AddSection(new StructuralSection("SEC", "Beam", .003, .00002))
        .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Frame2D))
        .AddSupport(StructuralSupport.Hinge("SA", "A"))
        .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
        .AddLoadCase(new StructuralLoadCase("G", "Permanent"))
        .AddLoad(StructuralLoad.PointLoadOnMember("p", "G", "M", StructuralLoadDirection.GlobalY, -12, .5))
        .AddLoad(StructuralLoad.NodalForce("n", "G", "B", StructuralLoadDirection.GlobalX, -9));
}
