using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Dynamics;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Dynamics.M67;

public sealed class DynamicForceRecoveryM67Tests
{
    private static StructuralModel Frame(double length = 2.0)
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("M", "Steel", 210_000_000, 77))
            .AddSection(new StructuralSection("S", "Beam", .01, .0001))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", length, 0))
            .AddMember(new StructuralMember("AB", "A", "B", "M", "S"));
        model.AddSupport(StructuralSupport.Fixed("SUP", "A"));
        return model;
    }

    private static StructuralTimeHistoryAnalysisRequest Request(
        StructuralModel model, StructuralHarmonicDamping damping, double fy = -10,
        IReadOnlyList<StructuralTimeInitialNodeState>? initial = null,
        int steps = 15, double dt = .002)
    {
        return new(model, new StructuralDynamicsDefinition(), dt, steps, damping,
            new[] { new StructuralTimeLoadPattern(
                new[] { new StructuralHarmonicNodalForce("B", Fy:fy) },
                new[] { new StructuralTimeLoadKnot(0,1), new StructuralTimeLoadKnot(1,1) }) }, initial);
    }
    private static readonly StructuralHarmonicDamping NoDamping = new() { UniformModalRatio = 0 };

    [Fact]
    public void ExactInitialStaticCantilever_ReactionsAndElasticEndForcesMatchHandSolution()
    {
        const double e = 210_000_000, inertia = .0001, length = 2, force = -10;
        double displacement = force * length*length*length / (3*e*inertia);
        double rotation = force*length*length / (2*e*inertia);
        var request = Request(Frame(), NoDamping, force,
            new[] { new StructuralTimeInitialNodeState("B", Uy:displacement, Rz:rotation) }, steps:2);
        StructuralDynamicForceRecoveryOutput output = new StructuralDynamicForceRecoveryService().Analyze(request);
        var reaction = Assert.Single(output.Steps[0].SupportNodeReactions);
        Assert.Equal("A", reaction.NodeId);
        Assert.InRange(Math.Abs(reaction.Fx),0,1e-7);
        Assert.InRange(Math.Abs(reaction.Fy + force),0,1e-6);
        Assert.InRange(Math.Abs(reaction.Mz + force * length),0,1e-6);
        var member = Assert.Single(output.Steps[0].MemberElasticEndForces);
        Assert.InRange(Math.Abs(member.StartShear + force),0,1e-6);
        Assert.InRange(Math.Abs(member.EndShear - force),0,1e-6);
        Assert.InRange(Math.Abs(member.StartMoment + force*length),0,1e-6);
        Assert.InRange(Math.Abs(member.EndMoment),0,1e-6);
        Assert.InRange(output.MaximumAdmissibleEquilibriumResidual,0,1e-6);
    }

    [Theory]
    [InlineData(0,0)]
    [InlineData(.04,.00001)]
    public void Transient_ReactionsIncludeConsistentInertia_AndSatisfyFreeDofEquilibrium(
        double alpha, double beta)
    {
        var output = new StructuralDynamicForceRecoveryService().Analyze(Request(Frame(),
            new StructuralHarmonicDamping { RayleighAlphaInverseSeconds=alpha, RayleighBetaSeconds=beta }));
        Assert.Equal(16, output.Steps.Count);
        Assert.Single(output.ReactionEnvelopes);
        Assert.Single(output.MemberEnvelopes);
        Assert.All(output.Steps, step =>
        {
            Assert.True(double.IsFinite(step.MaxAdmissibleEquilibriumResidual));
            Assert.InRange(step.MaxAdmissibleEquilibriumResidual,0,1e-6);
            Assert.All(step.SupportNodeReactions, r =>
                Assert.True(double.IsFinite(r.Fx) && double.IsFinite(r.Fy) && double.IsFinite(r.Mz)));
        });
        Assert.True(output.ReactionEnvelopes[0].Fy.MaxAbsolute > 0);
        Assert.InRange(output.MemberEnvelopes[0].StartMoment.TimeSeconds, 0, .03);
    }

    [Fact]
    public void UniformModalDamping_IsExplicitlyWarnedButEquilibrated()
    {
        var output = new StructuralSolver2DService().AnalyzeTimeHistoryForces(Request(Frame(),
            new StructuralHarmonicDamping { UniformModalRatio=.05 }, fy:-1));
        Assert.Contains(output.Warnings, w => w.Contains("M67_MODAL_DAMPING"));
        Assert.InRange(output.MaximumAdmissibleEquilibriumResidual,0,1e-6);
    }

    [Fact]
    public void AxialTruss_ReturnsOppositeEndForcesAndGlobalNetReaction()
    {
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("M", "Steel", 200_000_000, 77))
            .AddSection(new StructuralSection("S", "Bar", .01, .00001))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddMember(new StructuralMember("AB", "A", "B", "M", "S", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"));
        double ux = 12 * 2.0 / (200_000_000 * .01);
        var request = new StructuralTimeHistoryAnalysisRequest(model,new StructuralDynamicsDefinition(),
            .001,3,NoDamping,
            new[] { new StructuralTimeLoadPattern(
                new[] { new StructuralHarmonicNodalForce("B", Fx:12) },
                new[] { new StructuralTimeLoadKnot(0,1), new StructuralTimeLoadKnot(1,1) }) },
            new[] { new StructuralTimeInitialNodeState("B",Ux:ux) });
        var result = new StructuralDynamicForceRecoveryService().Analyze(request);
        var bar = Assert.Single(result.Steps[0].MemberElasticEndForces);
        Assert.InRange(Math.Abs(bar.StartAxial + 12),0,1e-6);
        Assert.InRange(Math.Abs(bar.EndAxial - 12),0,1e-6);
        var fixedReaction = Assert.Single(result.Steps[0].SupportNodeReactions, x => x.NodeId == "A");
        Assert.InRange(Math.Abs(fixedReaction.Fx+12),0,1e-6);
        Assert.InRange(result.MaximumAdmissibleEquilibriumResidual,0,1e-6);
    }

    [Fact]
    public void InclinedRollerAndRotatedMember_ReactionsAreReportedInGlobalCoordinates()
    {
        double c = 1 / Math.Sqrt(2);
        var model = new StructuralModel()
            .AddMaterial(new StructuralMaterial("M", "Steel", 200_000_000, 77))
            .AddSection(new StructuralSection("S", "Bar", .01, .00001))
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2*c, 2*c))
            .AddMember(new StructuralMember("AB", "A", "B", "M", "S", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B", orientationDegrees:45));
        double extension = 12 * 2 / (200_000_000 * .01);
        var request = new StructuralTimeHistoryAnalysisRequest(model,new StructuralDynamicsDefinition(),
            .001,2,NoDamping,
            new[] {new StructuralTimeLoadPattern(
                new[] {new StructuralHarmonicNodalForce("B",Fx:12*c,Fy:12*c)},
                new[] {new StructuralTimeLoadKnot(0,1),new StructuralTimeLoadKnot(1,1)})},
            new[] {new StructuralTimeInitialNodeState("B",Ux:extension*c,Uy:extension*c)});
        var output = new StructuralDynamicForceRecoveryService().Analyze(request);
        var a = Assert.Single(output.Steps[0].SupportNodeReactions, x => x.NodeId == "A");
        var b = Assert.Single(output.Steps[0].SupportNodeReactions, x => x.NodeId == "B");
        Assert.InRange(Math.Abs(a.Fx+12*c),0,1e-6);
        Assert.InRange(Math.Abs(a.Fy+12*c),0,1e-6);
        Assert.InRange(Math.Abs(b.Fx),0,1e-6);
        Assert.InRange(Math.Abs(b.Fy),0,1e-6);
        Assert.InRange(output.MaximumAdmissibleEquilibriumResidual,0,1e-6);
    }

    [Fact]
    public void DynamicLoadAtSupport_ContributesToReaction_NotToFreeEquilibrium()
    {
        var request = Request(Frame(),NoDamping,0);
        request = request with { Loads = new[] { new StructuralTimeLoadPattern(
            new[] { new StructuralHarmonicNodalForce("A", Fy:5) },
            new[] { new StructuralTimeLoadKnot(0,1),new StructuralTimeLoadKnot(1,1) }) } };
        var result = new StructuralDynamicForceRecoveryService().Analyze(request);
        Assert.All(result.Steps, s =>
        {
            Assert.InRange(Math.Abs(s.SupportNodeReactions[0].Fy+5),0,1e-7);
            Assert.InRange(s.MaxAdmissibleEquilibriumResidual,0,1e-7);
        });
    }

    [Fact]
    public void ExplicitlyRefusesDynamicMemberReleases()
    {
        var model=Frame(); model.Members[0]=model.Members[0] with {ReleaseEndMoment=true};
        var request = Request(model,NoDamping);
        Assert.Throws<StructuralSolver2D.Analysis.StructuralAnalysisException>(() =>
            new StructuralDynamicForceRecoveryService().Analyze(request));
    }
}
