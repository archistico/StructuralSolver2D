using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Integration;

namespace StructuralSolver2D.Verification.Tests.M87;

public sealed class FemPolynomialDiagnosticExtremaC2BTests
{
    [Fact]
    public void AffineConcentratedLoad_RetainsPreviousC2AGolden()
    {
        StructuralModel model = Beam().AddLoad(StructuralLoad.PointLoadOnMember(
            "P", "G", "M", StructuralLoadDirection.GlobalY, -12, .5));
        var request = Request();
        var previous = FemAffineUtilizationExtremaC2A.Locate(model, request);
        var extended = FemPolynomialDiagnosticExtremaC2B.Locate(model, request);

        Assert.Equal(AffineUtilizationExtremaStatusC2A.LocatedWithinAffineAssumptions, previous.Status);
        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions, extended.Status);
        Assert.Equal(3, extended.Maximum!.MetricValue, 7);
        Assert.Equal(2, extended.Maximum.DistanceMetres, 7);
        Assert.Equal(previous.Maximum!.MetricValue, extended.Maximum.MetricValue, 7);
    }

    [Fact]
    public void UniformLoad_ConvexAbsoluteSum_HasTwoIndependentInteriorGoldens()
    {
        // L=4, q=-2: V(x)=4-2x, M(x)=4x-x², N=-9.
        // eta=1+|V|/4+|M|/4; its two maxima are 2.25 at x=1 and x=3.
        StructuralModel model = Beam().AddLoad(StructuralLoad.UniformDistributedLoad(
            "UDL", "G", "M", StructuralLoadDirection.GlobalY, -2));
        var result = FemPolynomialDiagnosticExtremaC2B.Locate(model, Request() with
        {
            ShearScaleKn = 4,
            BendingScaleKnm = 4
        });

        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions, result.Status);
        Assert.NotNull(result.Maximum);
        Assert.Equal(2.25, result.Maximum!.MetricValue, 7);
        Assert.Contains(result.Candidates, c => Math.Abs(c.DistanceMetres - 1) < 1e-6 &&
            Math.Abs(c.MetricValue - 2.25) < 1e-7);
        Assert.Contains(result.Candidates, c => Math.Abs(c.DistanceMetres - 3) < 1e-6 &&
            Math.Abs(c.MetricValue - 2.25) < 1e-7);
        Assert.False(result.NormativeRuleEvaluated);
        Assert.False(result.MemberAdequacyEstablished);
        Assert.False(result.NumericalIntervalCertificateEstablished);
    }

    [Fact]
    public void TriangularDistributedLoad_CubicMomentFindsIrrationalGoldenStation()
    {
        // q(x)=-x/2 on L=4; RA=4/3; M=4x/3-x³/12.
        // Mmax at x=4/sqrt(3), with Mmax=32/(9sqrt(3)).
        StructuralModel model = Beam().AddLoad(StructuralLoad.LinearDistributedLoad(
            "TRI", "G", "M", StructuralLoadDirection.GlobalY, 0, -2));
        var result = FemPolynomialDiagnosticExtremaC2B.Locate(model, Request() with
        {
            Metric = AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct,
            BendingScaleKnm = 1
        });

        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions, result.Status);
        Assert.NotNull(result.Maximum);
        Assert.Equal(4 / Math.Sqrt(3), result.Maximum!.DistanceMetres, 6);
        Assert.Equal(32 / (9 * Math.Sqrt(3)), result.Maximum.MetricValue, 6);
        Assert.False(result.MemberAdequacyEstablished);
    }

    [Fact]
    public void TwoTriangularDirections_QuinticAxialMomentProductHasQuarticDerivativeGolden()
    {
        // Uniform geometry L=4. qx(x)=qy(x)=-x/2, plus axial -9 at B.
        // N=-13+x²/4, M=4x/3-x³/12. d(N*M)/dx=0 gives
        // 5x^4-204x²+832=0. Physical interior maximizer:
        // x=sqrt((204-sqrt(24976))/10), independent of C# root algorithm.
        StructuralModel model = Beam()
            .AddLoad(StructuralLoad.LinearDistributedLoad("TX", "G", "M",
                StructuralLoadDirection.GlobalX, 0, -2))
            .AddLoad(StructuralLoad.LinearDistributedLoad("TY", "G", "M",
                StructuralLoadDirection.GlobalY, 0, -2));
        double xGolden = Math.Sqrt((204 - Math.Sqrt(24976)) / 10);
        double nGolden = -13 + xGolden * xGolden / 4;
        double mGolden = 4 * xGolden / 3 - xGolden * xGolden * xGolden / 12;

        var result = FemPolynomialDiagnosticExtremaC2B.Locate(model, Request() with
        {
            Metric = AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct,
            AxialScaleKn = 1,
            BendingScaleKnm = 1
        });

        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions, result.Status);
        Assert.Equal(xGolden, result.Maximum!.DistanceMetres, 5);
        Assert.Equal(Math.Abs(nGolden * mGolden), result.Maximum.MetricValue, 5);
        Assert.False(result.NormativeRuleEvaluated);
    }

    [Fact]
    public void ModelSelfWeight_IsExpandedIntoUniformLoadAndProducesManualGolden()
    {
        // w = A*gamma = 0.003*78.5 = 0.2355 kN/m, M(L/2)=w*L²/8=0.471.
        StructuralModel model = Beam().AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"));
        var result = FemPolynomialDiagnosticExtremaC2B.Locate(model, Request() with
        {
            Metric = AffineDiagnosticMetricC2A.AbsoluteNormalizedAxialMomentProduct,
            BendingScaleKnm = .471
        });

        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions, result.Status);
        Assert.Equal(2, result.Maximum!.DistanceMetres, 6);
        Assert.Equal(1, result.Maximum.MetricValue, 7);
        Assert.False(result.CompleteActionSetEstablished);
    }

    [Fact]
    public void MixedUniformAndPointLoad_TracksBothOneSidedLimits()
    {
        StructuralModel model = Beam()
            .AddLoad(StructuralLoad.UniformDistributedLoad("UDL", "G", "M", StructuralLoadDirection.GlobalY, -2))
            .AddLoad(StructuralLoad.PointLoadOnMember("P", "G", "M", StructuralLoadDirection.GlobalY, -12, .5));
        var result = FemPolynomialDiagnosticExtremaC2B.Locate(model, Request());

        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions, result.Status);
        Assert.Contains(result.Candidates, c => c.DistanceMetres == 2 &&
            c.SectionSide == SectionLimitM78.BeforePointLoad);
        Assert.Contains(result.Candidates, c => c.DistanceMetres == 2 &&
            c.SectionSide == SectionLimitM78.AfterPointLoad);
        Assert.NotNull(result.Maximum);
    }

    [Fact]
    public void NegativeCombinationFactors_ProduceExpectedResultantUniformLoad()
    {
        // G=-2, Q=+1 and factor -2 for Q -> q=-4 kN/m.
        // RA=8, Mmax=8; eta=1+|8-4x|/8+|8x-2x²|/8;
        // each interior max at x=1,3 is 2.25.
        StructuralModel model = Beam()
            .AddLoad(StructuralLoad.UniformDistributedLoad("g", "G", "M", StructuralLoadDirection.GlobalY, -2))
            .AddLoadCase(new StructuralLoadCase("Q", "Alternative"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("q", "Q", "M", StructuralLoadDirection.GlobalY, +1))
            .AddLoadCombination(new StructuralLoadCombination("NEG", "Negative factor", new[]
            {
                new StructuralLoadCombinationTerm("G", 1),
                new StructuralLoadCombinationTerm("Q", -2)
            }));
        var result = FemPolynomialDiagnosticExtremaC2B.Locate(model, Request() with
        {
            TargetKind = StructuralAnalysisTargetKind.LoadCombination,
            TargetId = "NEG",
            ShearScaleKn = 8,
            BendingScaleKnm = 8
        });

        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.LocatedWithinPolynomialAssumptions, result.Status);
        Assert.Equal(2.25, result.Maximum!.MetricValue, 7);
    }

    [Fact]
    public void FourthDegreeProduct_RecursivelyIsolatesInteriorDerivativeRoot()
    {
        // p(t)=t(1-t)^3 => pmax=27/256 at t=1/4.
        double[] p = PolynomialDiagnosticMathC2B.Multiply(new[] { 0d, 1d },
            new[] { 1d, -3d, 3d, -1d });
        var roots = PolynomialDiagnosticMathC2B.StationaryPointsInUnitInterval(p);
        Assert.Contains(roots, x => Math.Abs(x - .25) < 1e-9);
        Assert.Equal(27d / 256d, PolynomialDiagnosticMathC2B.Evaluate(p, .25), 10);
    }

    [Fact]
    public void MultipleRealRootWithoutSignChange_IsFoundByRecursiveDerivative()
    {
        // (t-.5)^2(t-.8) has a double root at .5 and a simple root at .8.
        var polynomial = PolynomialDiagnosticMathC2B.Multiply(
            new[] { .25d, -1d, 1d }, new[] { -.8d, 1d });
        var roots = PolynomialDiagnosticMathC2B.RealRootsInUnitInterval(polynomial);
        Assert.Contains(roots, x => Math.Abs(x - .5) < 1e-8);
        Assert.Contains(roots, x => Math.Abs(x - .8) < 1e-8);
    }

    [Fact]
    public void InvalidScalesOrUnknownTargetCannotReturnMaximum()
    {
        var badScale = FemPolynomialDiagnosticExtremaC2B.Locate(Beam(), Request() with
        {
            AxialScaleKn = double.NaN
        });
        var badTarget = FemPolynomialDiagnosticExtremaC2B.Locate(Beam(), Request() with
        {
            TargetId = "DOES_NOT_EXIST"
        });
        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, badScale.Status);
        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.InvalidInput, badTarget.Status);
        Assert.Null(badScale.Maximum);
        Assert.Null(badTarget.Maximum);
    }

    [Fact]
    public void TrussMemberIsNotPromotedToFramePolynomialResult()
    {
        StructuralModel truss = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Steel", 210_000_000))
            .AddSection(new StructuralSection("SEC", "Axial", .003, 0))
            .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("G", "Test"))
            .AddLoad(StructuralLoad.NodalForce("N", "G", "B", StructuralLoadDirection.GlobalX, -9));

        var result = FemPolynomialDiagnosticExtremaC2B.Locate(truss, Request());
        Assert.Equal(PolynomialDiagnosticExtremaStatusC2B.ScreeningIncomplete, result.Status);
        Assert.Null(result.Maximum);
        Assert.False(result.WholeStructureVerified);
    }

    [Fact]
    public void PurePolynomialRejectsNonfiniteCoefficients()
    {
        Assert.Throws<ArgumentException>(() =>
            PolynomialDiagnosticMathC2B.StationaryPointsInUnitInterval(new[] { 1d, double.NaN }));
        Assert.Throws<ArgumentException>(() =>
            PolynomialDiagnosticMathC2B.RealRootsInUnitInterval(new[] { double.PositiveInfinity }));
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
        .AddLoad(StructuralLoad.NodalForce("N", "G", "B", StructuralLoadDirection.GlobalX, -9));
}
