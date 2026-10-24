using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Validation.M85;

/// <summary>
/// M85-C independent static reference solutions and input invariance checks.
/// All expected results are obtained from equilibrium, Euler-Bernoulli beam theory
/// or the unit-load/virtual-work theorem, not from production FE matrices.
/// Units: kN, m, kNm, radians. This is not code compliance certification.
/// </summary>
public sealed class FinalReferenceAndRobustnessM85CTests
{
    private const double E = 210_000_000d; // kN/m²
    private const double A = 0.003d;       // m²
    private const double I = 0.00002d;     // m⁴

    [Fact]
    public void NonuniformTriangleTruss_MatchesMethodOfJointsAndVirtualWork()
    {
        // A(0,0), B(6,0), C(3,4): diagonal length 5, sin(theta)=4/5.
        // For P=40 kN down: R_Ay=R_By=20, diagonal N=-25, base N=+15.
        // Unequal areas ensure a nontrivial virtual-work displacement reference.
        const double p = 40d;
        const double diagonalForce = -25d;
        const double tieForce = 15d;
        const double leftArea = 0.003d;
        const double rightArea = 0.006d;
        const double tieArea = 0.0009d;

        StructuralModel model = Triangle(leftArea, rightArea, tieArea);
        StructuralAnalysisResult truss = new Truss2DAnalyzer().Analyze(model, "LC");
        StructuralAnalysisResult plane = new PlaneStructureAnalyzer().Analyze(model, "LC");

        Close(p / 2d, truss.GetReaction("SA").Fy, 1e-8);
        Close(p / 2d, truss.GetReaction("SB").Fy, 1e-8);
        Close(0d, truss.GetReaction("SA").Fx, 1e-8);
        Close(-diagonalForce, truss.MemberEndForces.Single(x => x.MemberId == "AC").StartAxial, 1e-8);
        Close(-diagonalForce, truss.MemberEndForces.Single(x => x.MemberId == "BC").StartAxial, 1e-8);
        Close(-tieForce, truss.MemberEndForces.Single(x => x.MemberId == "AB").StartAxial, 1e-8);

        // Virtual work: delta_Cy = sum(N_i*n_i*L_i/(E*A_i)); n_i=N_i/(-P)
        // for a unit downward load at C. The expected vertical displacement is negative.
        double expectedUy = -(diagonalForce * diagonalForce * 5d / (E * leftArea)
            + diagonalForce * diagonalForce * 5d / (E * rightArea)
            + tieForce * tieForce * 6d / (E * tieArea)) / p;
        Close(expectedUy, truss.GetDisplacement("C").Uy, 1e-10);
        Close(expectedUy, plane.GetDisplacement("C").Uy, 1e-10);

        foreach (StructuralNode node in model.Nodes)
        {
            Close(truss.GetDisplacement(node.Id).Ux, plane.GetDisplacement(node.Id).Ux, 1e-10);
            Close(truss.GetDisplacement(node.Id).Uy, plane.GetDisplacement(node.Id).Uy, 1e-10);
        }
    }

    [Theory]
    [InlineData(4d, 8d)]
    [InlineData(3d, 5d)]
    public void TwoEqualContinuousSpans_MatchThreeMomentReference(double span, double load)
    {
        // A-B-C: two equal spans L, constant EI, q down on both spans.
        // Clapeyron three-moment theorem: M_B=-qL²/8.
        // R_A=R_C=3qL/8; R_B=5qL/4.
        // Euler-Bernoulli integration: theta_A=-qL³/(48EI), theta_B=0, theta_C=+qL³/(48EI).
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(ContinuousBeam(span, load, E), "LC");
        double endReaction = 3d * load * span / 8d;
        double middleReaction = 5d * load * span / 4d;
        double endRotation = load * Math.Pow(span, 3) / (48d * E * I);

        Close(endReaction, result.GetReaction("SA").Fy, 1e-8);
        Close(middleReaction, result.GetReaction("SB").Fy, 1e-8);
        Close(endReaction, result.GetReaction("SC").Fy, 1e-8);
        Close(0d, result.GetReaction("SA").Fx, 1e-8);
        Close(-endRotation, result.GetDisplacement("A").Rz, 1e-10);
        Close(0d, result.GetDisplacement("B").Rz, 1e-10);
        Close(endRotation, result.GetDisplacement("C").Rz, 1e-10);
        Close(2d * load * span,
            result.GetReaction("SA").Fy + result.GetReaction("SB").Fy + result.GetReaction("SC").Fy, 1e-8);
    }

    [Fact]
    public void PublishedWorkedExample_TwoSpans4mAnd3kNperM_MatchesIndependentNumbers()
    {
        // Independently published worked values, accessed 2026-10-24:
        // https://www.toolfoundry.app/engineering/continuous-beam-calculator
        // Two equal 4 m spans under 3 kN/m on each: outer reactions 4.50 kN,
        // central reaction 15.00 kN, hogging support moment magnitude 6.00 kNm.
        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(ContinuousBeam(4d, 3d, E), "LC");
        Close(4.5d, result.GetReaction("SA").Fy, 1e-8);
        Close(15d, result.GetReaction("SB").Fy, 1e-8);
        Close(4.5d, result.GetReaction("SC").Fy, 1e-8);
        Close(6d, Math.Abs(result.MemberEndForces.Single(x => x.MemberId == "AB").EndMoment), 1e-8);
        Close(6d, Math.Abs(result.MemberEndForces.Single(x => x.MemberId == "BC").StartMoment), 1e-8);
    }

    [Theory]
    [InlineData(0.1d)]
    [InlineData(10d)]
    public void ScalingAllElasticStiffness_LeavesReactionsInvariantAndScalesRotations(double scale)
    {
        // In a homogeneous elastic problem without imposed settlements,
        // K'=s*K implies u'=u/s while support reactions remain unchanged.
        var solver = new Frame2DAnalyzer();
        StructuralAnalysisResult baseline = solver.Analyze(ContinuousBeam(4d, 8d, E), "LC");
        StructuralAnalysisResult scaled = solver.Analyze(ContinuousBeam(4d, 8d, E * scale), "LC");

        foreach (string nodeId in new[] { "A", "B", "C" })
            Close(baseline.GetDisplacement(nodeId).Rz / scale,
                scaled.GetDisplacement(nodeId).Rz, 1e-10);
        foreach (string supportId in new[] { "SA", "SB", "SC" })
            Close(baseline.GetReaction(supportId).Fy,
                scaled.GetReaction(supportId).Fy, 1e-8);
        foreach (string memberId in new[] { "AB", "BC" })
        {
            MemberEndForceResult first = baseline.MemberEndForces.Single(x => x.MemberId == memberId);
            MemberEndForceResult second = scaled.MemberEndForces.Single(x => x.MemberId == memberId);
            Close(first.StartMoment, second.StartMoment, 1e-8);
            Close(first.EndMoment, second.EndMoment, 1e-8);
        }
    }

    [Fact]
    public void ReversingNodeMemberSupportAndLoadOrder_DoesNotChangeTrussPhysics()
    {
        StructuralAnalysisResult reference = new Truss2DAnalyzer().Analyze(
            Triangle(0.003d, 0.006d, 0.0009d), "LC");
        StructuralAnalysisResult rearranged = new Truss2DAnalyzer().Analyze(
            Triangle(0.003d, 0.006d, 0.0009d, reverseInputOrder: true), "LC");
        CompareTriangleResults(reference, rearranged);
    }

    [Fact]
    public void LargeRigidCoordinateTranslation_DoesNotChangeTrussPhysics()
    {
        StructuralAnalysisResult reference = new Truss2DAnalyzer().Analyze(
            Triangle(0.003d, 0.006d, 0.0009d), "LC");
        StructuralAnalysisResult shifted = new Truss2DAnalyzer().Analyze(
            Triangle(0.003d, 0.006d, 0.0009d, offsetX: 100_000_000d, offsetY: -100_000_000d), "LC");
        CompareTriangleResults(reference, shifted);
    }

    private static StructuralModel ContinuousBeam(double span, double load, double elasticModulus) =>
        new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Elastic", elasticModulus))
            .AddSection(new StructuralSection("SEC", "Section", A, I))
            .AddLoadCase(new StructuralLoadCase("LC", "Two equal spans"))
            .AddNode(new StructuralNode("A", 0d, 0d))
            .AddNode(new StructuralNode("B", span, 0d))
            .AddNode(new StructuralNode("C", 2d * span, 0d))
            .AddMember(new StructuralMember("AB", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("BC", "B", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddSupport(StructuralSupport.SimpleSupport("SC", "C"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("QAB", "LC", "AB", StructuralLoadDirection.GlobalY, -load))
            .AddLoad(StructuralLoad.UniformDistributedLoad("QBC", "LC", "BC", StructuralLoadDirection.GlobalY, -load));

    private static StructuralModel Triangle(double leftArea, double rightArea, double tieArea,
        bool reverseInputOrder = false, double offsetX = 0d, double offsetY = 0d)
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Elastic", E))
            .AddSection(new StructuralSection("LEFT", "Left", leftArea, I))
            .AddSection(new StructuralSection("RIGHT", "Right", rightArea, I))
            .AddSection(new StructuralSection("TIE", "Tie", tieArea, I))
            .AddLoadCase(new StructuralLoadCase("LC", "Top joint load"));
        StructuralNode[] nodes =
        {
            new("A", offsetX, offsetY),
            new("B", offsetX + 6d, offsetY),
            new("C", offsetX + 3d, offsetY + 4d)
        };
        StructuralMember[] members =
        {
            new("AC", "A", "C", "MAT", "LEFT", MemberType.Truss2D),
            new("BC", "B", "C", "MAT", "RIGHT", MemberType.Truss2D),
            new("AB", "A", "B", "MAT", "TIE", MemberType.Truss2D)
        };
        StructuralSupport[] supports =
        {
            StructuralSupport.Hinge("SA", "A"),
            StructuralSupport.SimpleSupport("SB", "B")
        };
        StructuralLoad[] loads =
        {
            StructuralLoad.NodalForce("P", "LC", "C", StructuralLoadDirection.GlobalY, -40d)
        };
        foreach (StructuralNode node in reverseInputOrder ? nodes.Reverse() : nodes) model.AddNode(node);
        foreach (StructuralMember member in reverseInputOrder ? members.Reverse() : members) model.AddMember(member);
        foreach (StructuralSupport support in reverseInputOrder ? supports.Reverse() : supports) model.AddSupport(support);
        foreach (StructuralLoad load in loads) model.AddLoad(load);
        return model;
    }

    private static void CompareTriangleResults(StructuralAnalysisResult expected, StructuralAnalysisResult actual)
    {
        foreach (string nodeId in new[] { "A", "B", "C" })
        {
            Close(expected.GetDisplacement(nodeId).Ux, actual.GetDisplacement(nodeId).Ux, 1e-10);
            Close(expected.GetDisplacement(nodeId).Uy, actual.GetDisplacement(nodeId).Uy, 1e-10);
        }
        foreach (string supportId in new[] { "SA", "SB" })
        {
            Close(expected.GetReaction(supportId).Fx, actual.GetReaction(supportId).Fx, 1e-8);
            Close(expected.GetReaction(supportId).Fy, actual.GetReaction(supportId).Fy, 1e-8);
        }
        foreach (string memberId in new[] { "AC", "BC", "AB" })
        {
            MemberEndForceResult first = expected.MemberEndForces.Single(x => x.MemberId == memberId);
            MemberEndForceResult second = actual.MemberEndForces.Single(x => x.MemberId == memberId);
            Close(first.StartAxial, second.StartAxial, 1e-8);
            Close(first.EndAxial, second.EndAxial, 1e-8);
        }
    }

    private static void Close(double expected, double actual, double absoluteTolerance)
    {
        Assert.True(double.IsFinite(actual), $"M85-C non-finite output (expected {expected:R}).");
        double tolerance = Math.Max(absoluteTolerance, 1e-8d * Math.Abs(expected));
        Assert.True(Math.Abs(expected - actual) <= tolerance,
            $"M85-C expected {expected:R}, actual {actual:R}, tolerance {tolerance:R}.");
    }
}
