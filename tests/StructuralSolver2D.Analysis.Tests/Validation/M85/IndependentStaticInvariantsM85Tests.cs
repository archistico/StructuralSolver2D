using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Validation.M85;

/// <summary>
/// M85-A independent checks: closed-form beam/bar theory and solution invariants.
/// These assertions do not call the production stiffness-matrix builders to obtain expected values.
/// All actions are first-order elastic and in kN/m/rad units; no code-design checks.
/// </summary>
public sealed class IndependentStaticInvariantsM85Tests
{
    private const double E = 210_000_000d; // kN/m^2
    private const double A = 0.003d;       // m^2
    private const double I = 0.00002d;     // m^4

    [Fact]
    public void InclinedCantilever_AxialShearAndTipMoment_MatchClosedFormBeamTheory()
    {
        // L=5 m, local x=(3/5,4/5), local y=(-4/5,3/5).
        // Independent Euler-Bernoulli superposition of axial load, shear and end moment.
        const double length = 5d;
        const double fx = 11d;
        const double fy = -7d;
        const double mz = 5d;
        const double c = 3d / 5d;
        const double s = 4d / 5d;
        var model = Basics()
            .AddNode(new StructuralNode("A", 0d, 0d))
            .AddNode(new StructuralNode("B", 3d, 4d))
            .AddMember(new StructuralMember("F", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("FIX", "A"))
            .AddLoad(StructuralLoad.NodalForce("PX", "LC", "B", StructuralLoadDirection.GlobalX, fx))
            .AddLoad(StructuralLoad.NodalForce("PY", "LC", "B", StructuralLoadDirection.GlobalY, fy))
            .AddLoad(StructuralLoad.NodalMoment("M", "LC", "B", mz));

        StructuralAnalysisResult result = new Frame2DAnalyzer().Analyze(model, "LC");
        double axial = c * fx + s * fy;
        double shear = -s * fx + c * fy;
        double uLocal = axial * length / (E * A);
        double vLocal = shear * Math.Pow(length, 3) / (3d * E * I)
            + mz * length * length / (2d * E * I);
        double theta = shear * length * length / (2d * E * I)
            + mz * length / (E * I);

        NodalDisplacementResult tip = result.GetDisplacement("B");
        Close(c * uLocal - s * vLocal, tip.Ux, 1e-10);
        Close(s * uLocal + c * vLocal, tip.Uy, 1e-10);
        Close(theta, tip.Rz, 1e-10);

        // Independent whole-structure force/moment balance about A.
        SupportReactionResult reaction = result.GetReaction("FIX");
        Close(-fx, reaction.Fx, 1e-8);
        Close(-fy, reaction.Fy, 1e-8);
        Close(-(3d * fy - 4d * fx + mz), reaction.Mz, 1e-8);
    }

    [Fact]
    public void TwoNonIdenticalTrussBarsInSeries_MatchElementaryAxialCompliance()
    {
        // Each intermediate node is vertically supported: this eliminates transverse mechanisms.
        const double force = 20d;
        const double a1 = 0.003d;
        const double a2 = 0.006d;
        var model = Basics()
            .AddSection(new StructuralSection("SEC2", "Section 2", a2, I))
            .AddNode(new StructuralNode("A", 0d, 0d))
            .AddNode(new StructuralNode("B", 2d, 0d))
            .AddNode(new StructuralNode("C", 5d, 0d))
            .AddMember(new StructuralMember("T1", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddMember(new StructuralMember("T2", "B", "C", "MAT", "SEC2", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("S1", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("S2", "B"))
            .AddSupport(StructuralSupport.SimpleSupport("S3", "C"))
            .AddLoad(StructuralLoad.NodalForce("FX", "LC", "C", StructuralLoadDirection.GlobalX, force));
        var truss = new Truss2DAnalyzer().Analyze(model, "LC");
        var generic = new PlaneStructureAnalyzer().Analyze(model, "LC");

        double expectedB = force * 2d / (E * a1);
        double expectedC = expectedB + force * 3d / (E * a2);
        Close(expectedB, truss.GetDisplacement("B").Ux, 1e-11);
        Close(expectedC, truss.GetDisplacement("C").Ux, 1e-11);
        Close(-force, truss.GetReaction("S1").Fx, 1e-8);
        Close(0d, truss.GetReaction("S2").Fy, 1e-8);
        Close(0d, truss.GetReaction("S3").Fy, 1e-8);
        foreach (var member in truss.MemberEndForces)
        {
            Close(-force, member.StartAxial, 1e-8);
            Close(force, member.EndAxial, 1e-8);
        }
        foreach (var node in model.Nodes)
        {
            Close(truss.GetDisplacement(node.Id).Ux, generic.GetDisplacement(node.Id).Ux, 1e-11);
            Close(truss.GetDisplacement(node.Id).Uy, generic.GetDisplacement(node.Id).Uy, 1e-11);
        }
    }

    [Fact]
    public void IndeterminatePortal_RespectsMaxwellBettiAndSignedLoadSuperposition()
    {
        var model = Portal(rotated: false);
        var solver = new Frame2DAnalyzer();
        var p = solver.Analyze(model, "P");
        var q = solver.Analyze(model, "Q");
        var r = solver.Analyze(model, "R");
        var combination = solver.AnalyzeCombination(model, "COMB");

        // f_Q^T u_P = f_P^T u_Q. Case P: +18 kN at Cx; Q: -7 kN at Dy.
        Close(-7d * p.GetDisplacement("D").Uy,
              18d * q.GetDisplacement("C").Ux, 1e-10);

        // This also verifies that the public combination pipeline respects a negative factor.
        foreach (var node in model.Nodes)
        {
            var a = p.GetDisplacement(node.Id);
            var b = q.GetDisplacement(node.Id);
            var c = r.GetDisplacement(node.Id);
            var sum = combination.GetDisplacement(node.Id);
            Close(1.25d * a.Ux - 0.4d * b.Ux + 0.3d * c.Ux, sum.Ux, 1e-10);
            Close(1.25d * a.Uy - 0.4d * b.Uy + 0.3d * c.Uy, sum.Uy, 1e-10);
            Close(1.25d * a.Rz - 0.4d * b.Rz + 0.3d * c.Rz, sum.Rz, 1e-10);
        }
        foreach (var support in model.Supports)
        {
            var a = p.GetReaction(support.Id);
            var b = q.GetReaction(support.Id);
            var c = r.GetReaction(support.Id);
            var sum = combination.GetReaction(support.Id);
            Close(1.25d * a.Fx - 0.4d * b.Fx + 0.3d * c.Fx, sum.Fx, 1e-8);
            Close(1.25d * a.Fy - 0.4d * b.Fy + 0.3d * c.Fy, sum.Fy, 1e-8);
            Close(1.25d * a.Mz - 0.4d * b.Mz + 0.3d * c.Mz, sum.Mz, 1e-8);
        }
        foreach (var member in model.Members)
        {
            var a = p.MemberEndForces.Single(x => x.MemberId == member.Id);
            var b = q.MemberEndForces.Single(x => x.MemberId == member.Id);
            var c = r.MemberEndForces.Single(x => x.MemberId == member.Id);
            var sum = combination.MemberEndForces.Single(x => x.MemberId == member.Id);
            Close(1.25d * a.StartAxial - 0.4d * b.StartAxial + 0.3d * c.StartAxial, sum.StartAxial, 1e-8);
            Close(1.25d * a.StartShear - 0.4d * b.StartShear + 0.3d * c.StartShear, sum.StartShear, 1e-8);
            Close(1.25d * a.StartMoment - 0.4d * b.StartMoment + 0.3d * c.StartMoment, sum.StartMoment, 1e-8);
            Close(1.25d * a.EndAxial - 0.4d * b.EndAxial + 0.3d * c.EndAxial, sum.EndAxial, 1e-8);
            Close(1.25d * a.EndShear - 0.4d * b.EndShear + 0.3d * c.EndShear, sum.EndShear, 1e-8);
            Close(1.25d * a.EndMoment - 0.4d * b.EndMoment + 0.3d * c.EndMoment, sum.EndMoment, 1e-8);
        }
    }

    [Theory]
    [InlineData("P")]
    [InlineData("Q")]
    [InlineData("R")]
    [InlineData("COMB")]
    public void GlobalRigidRotationOfPortalAndLoads_PreservesLocalEndForces(string scenario)
    {
        // Proper 90-degree rotation: (x,y)->(-y,x). Rz and local member forces are invariant.
        StructuralModel original = Portal(rotated: false);
        StructuralModel rotated = Portal(rotated: true);
        var solver = new Frame2DAnalyzer();
        var first = scenario == "COMB"
            ? solver.AnalyzeCombination(original, scenario) : solver.Analyze(original, scenario);
        var second = scenario == "COMB"
            ? solver.AnalyzeCombination(rotated, scenario) : solver.Analyze(rotated, scenario);

        foreach (var node in original.Nodes)
        {
            NodalDisplacementResult a = first.GetDisplacement(node.Id);
            NodalDisplacementResult b = second.GetDisplacement(node.Id);
            Close(-a.Uy, b.Ux, 1e-10);
            Close(a.Ux, b.Uy, 1e-10);
            Close(a.Rz, b.Rz, 1e-10);
        }
        foreach (var support in original.Supports)
        {
            SupportReactionResult a = first.GetReaction(support.Id);
            SupportReactionResult b = second.GetReaction(support.Id);
            Close(-a.Fy, b.Fx, 1e-8);
            Close(a.Fx, b.Fy, 1e-8);
            Close(a.Mz, b.Mz, 1e-8);
        }
        foreach (var member in original.Members)
        {
            MemberEndForceResult a = first.MemberEndForces.Single(x => x.MemberId == member.Id);
            MemberEndForceResult b = second.MemberEndForces.Single(x => x.MemberId == member.Id);
            Close(a.StartAxial, b.StartAxial, 1e-8);
            Close(a.StartShear, b.StartShear, 1e-8);
            Close(a.StartMoment, b.StartMoment, 1e-8);
            Close(a.EndAxial, b.EndAxial, 1e-8);
            Close(a.EndShear, b.EndShear, 1e-8);
            Close(a.EndMoment, b.EndMoment, 1e-8);
        }
    }

    private static StructuralModel Basics() => new StructuralModel()
        .AddMaterial(new StructuralMaterial("MAT", "Material", E))
        .AddSection(new StructuralSection("SEC", "Section", A, I))
        .AddLoadCase(new StructuralLoadCase("LC", "Independent validation"));

    private static StructuralModel Portal(bool rotated)
    {
        // Original: A(0,0), B(4,0), C(0,3), D(4,3).
        // Rotated: A(0,0), B(0,4), C(-3,0), D(-3,4).
        StructuralModel model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("MAT", "Material", E))
            .AddSection(new StructuralSection("SEC", "Section", A, I))
            .AddLoadCase(new StructuralLoadCase("P", "Horizontal C"))
            .AddLoadCase(new StructuralLoadCase("Q", "Vertical D"))
            .AddLoadCase(new StructuralLoadCase("R", "Moment D"))
            .AddNode(new StructuralNode("A", 0d, 0d))
            .AddNode(new StructuralNode("B", rotated ? 0d : 4d, rotated ? 4d : 0d))
            .AddNode(new StructuralNode("C", rotated ? -3d : 0d, rotated ? 0d : 3d))
            .AddNode(new StructuralNode("D", rotated ? -3d : 4d, rotated ? 4d : 3d))
            .AddMember(new StructuralMember("AC", "A", "C", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("CD", "C", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddMember(new StructuralMember("BD", "B", "D", "MAT", "SEC", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"))
            .AddLoad(StructuralLoad.NodalForce("LP", "P", "C",
                rotated ? StructuralLoadDirection.GlobalY : StructuralLoadDirection.GlobalX, 18d))
            .AddLoad(StructuralLoad.NodalForce("LQ", "Q", "D",
                rotated ? StructuralLoadDirection.GlobalX : StructuralLoadDirection.GlobalY,
                rotated ? 7d : -7d))
            .AddLoad(StructuralLoad.NodalMoment("LR", "R", "D", 4d))
            .AddLoadCombination(new StructuralLoadCombination("COMB", "Signed combined actions", new[]
            {
                new StructuralLoadCombinationTerm("P", 1.25d),
                new StructuralLoadCombinationTerm("Q", -0.4d),
                new StructuralLoadCombinationTerm("R", 0.3d)
            }));
        return model;
    }

    private static void Close(double expected, double actual, double absoluteTolerance)
    {
        Assert.True(double.IsFinite(actual), $"Nonfinite result. Expected={expected}, actual={actual}");
        double tolerance = Math.Max(absoluteTolerance, 1e-8d * Math.Abs(expected));
        Assert.True(Math.Abs(expected - actual) <= tolerance,
            $"M85 validation discrepancy: expected={expected:R}, actual={actual:R}, tolerance={tolerance:R}");
    }
}
