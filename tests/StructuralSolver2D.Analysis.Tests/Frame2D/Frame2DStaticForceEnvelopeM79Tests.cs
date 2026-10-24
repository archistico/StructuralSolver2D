using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

public sealed class Frame2DStaticForceEnvelopeM79Tests
{
    private static StructuralModel Beam(double length = 5) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", length, 0))
            .AddMaterial(new StructuralMaterial("S", "steel", 210_000_000))
            .AddSection(new StructuralSection("R", "section", .004, .00002))
            .AddMember(new StructuralMember("BEAM", "A", "B", "S", "R", MemberType.Frame2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("G", "Permanent"))
            .AddLoadCase(new StructuralLoadCase("Q", "Variable"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("DEAD", "G", "BEAM", StructuralLoadDirection.GlobalY, -10))
            .AddLoad(StructuralLoad.UniformDistributedLoad("LIVE", "Q", "BEAM", StructuralLoadDirection.GlobalY, -5));

    private static StaticForceEnvelopeScenarioM79 C(string id) =>
        new(StructuralAnalysisTargetKind.LoadCase, id);

    private static StaticForceEnvelopeScenarioM79 K(string id) =>
        new(StructuralAnalysisTargetKind.LoadCombination, id);

    [Fact]
    public void Combinations_SelectSignedMinMaxAndAbsoluteWithGoverningIdentity()
    {
        StructuralModel model = Beam()
            .AddLoadCombination(new StructuralLoadCombination("STRONG", "strong", new[]
            {
                new StructuralLoadCombinationTerm("G", 1.35),
                new StructuralLoadCombinationTerm("Q", 1.5),
            }))
            .AddLoadCombination(new StructuralLoadCombination("REV", "reverse", new[]
            {
                new StructuralLoadCombinationTerm("G", -1),
                // An omitted term is equivalent to a zero contribution; the
                // model validator intentionally rejects explicit zero factors.
            }));
        StructuralStaticForceEnvelopeM79 envelope = new StructuralSolver2DService()
            .AnalyzeStaticForceEnvelope(model, new[] { K("REV"), K("STRONG"), C("G") });

        Assert.Equal(3, envelope.Scenarios.Count);
        FrameMemberForceEnvelopeM79 beam = envelope.GetMember("beam");
        GoverningForceExtremeM79 max = beam.Maximum(InternalForceQuantity.BendingMoment);
        GoverningForceExtremeM79 min = beam.Minimum(InternalForceQuantity.BendingMoment);
        GoverningForceExtremeM79 abs = beam.MaximumAbsolute(InternalForceQuantity.BendingMoment);
        Assert.Equal(65.625, max.Value, 7); // (1.35*10 + 1.5*5)*5^2/8
        Assert.Equal(2.5, max.Distance, 8);
        Assert.Equal("STRONG", max.ResultId);
        Assert.Equal(StructuralAnalysisTargetKind.LoadCombination, max.TargetKind);
        Assert.Equal(-31.25, min.Value, 7);
        Assert.Equal("REV", min.ResultId);
        Assert.Equal("STRONG", abs.ResultId);
        Assert.Equal(65.625, abs.Value, 7);
        Assert.Equal(.5, beam.Maximum(InternalForceQuantity.BendingMoment).Position, 7);
    }

    [Fact]
    public void IndependentM78Reference_EqualsEnvelopeAcrossMixedCasesAndCombinations()
    {
        StructuralModel model = Beam(6)
            .AddLoadCombination(new StructuralLoadCombination("C1", "sum", new[]
            {
                new StructuralLoadCombinationTerm("G", .5),
                new StructuralLoadCombinationTerm("Q", 2),
            }));
        var selection = new[] { C("G"), K("C1"), C("Q") };
        StructuralStaticForceEnvelopeM79 envelope = new StructuralSolver2DService()
            .AnalyzeStaticForceEnvelope(model, selection);
        var solver = new Frame2DAnalyzer();
        var finder = new Frame2DExactExtremaFinderM78();
        ExactMemberInternalForceExtremaM78[] exact =
        {
            finder.Find(model, solver.Analyze(model, "G"), "BEAM"),
            finder.Find(model, solver.AnalyzeCombination(model, "C1"), "BEAM"),
            finder.Find(model, solver.Analyze(model, "Q"), "BEAM"),
        };
        FrameMemberForceEnvelopeM79 member = envelope.GetMember("BEAM");
        foreach (InternalForceQuantity quantity in new[]
        {
            InternalForceQuantity.NormalForce,
            InternalForceQuantity.ShearForce,
            InternalForceQuantity.BendingMoment,
        })
        {
            Assert.Equal(exact.Min(x => x.Minimum(quantity).Value), member.Minimum(quantity).Value, 8);
            Assert.Equal(exact.Max(x => x.Maximum(quantity).Value), member.Maximum(quantity).Value, 8);
            Assert.Equal(exact.Max(x => Math.Abs(x.MaximumAbsolute(quantity).Value)),
                Math.Abs(member.MaximumAbsolute(quantity).Value), 8);
        }
    }

    [Fact]
    public void ConcentratedLoad_OneSidedShearAndProvenanceArePreserved()
    {
        StructuralModel model = Beam(8)
            .AddLoad(StructuralLoad.PointLoadOnMember("P1", "Q", "BEAM", StructuralLoadDirection.GlobalY, -100, .25));
        StructuralStaticForceEnvelopeM79 envelope = new StructuralSolver2DService()
            .AnalyzeStaticForceEnvelope(model, new[] { C("Q") });
        FrameMemberForceEnvelopeM79 member = envelope.GetMember("BEAM");
        // Q also has a small uniform load; the concentrated load creates a real shear jump.
        var finder = new Frame2DExactExtremaFinderM78();
        var standalone = finder.Find(model, new Frame2DAnalyzer().Analyze(model, "Q"), "BEAM");
        Assert.Equal(standalone.Maximum(InternalForceQuantity.ShearForce).Value,
            member.Maximum(InternalForceQuantity.ShearForce).Value, 8);
        Assert.Equal(standalone.Minimum(InternalForceQuantity.ShearForce).Side,
            member.Minimum(InternalForceQuantity.ShearForce).Side);
        Assert.Equal(2, member.Maximum(InternalForceQuantity.BendingMoment).Distance, 8);
    }

    [Fact]
    public void TiesRemainDeterministicInUserSpecifiedOrder()
    {
        StructuralModel model = Beam()
            .AddLoadCombination(new StructuralLoadCombination("FIRST", "first", new[]
            {
                new StructuralLoadCombinationTerm("G", 1),
            }))
            .AddLoadCombination(new StructuralLoadCombination("SECOND", "second", new[]
            {
                new StructuralLoadCombinationTerm("G", 1),
            }));
        var result = new StructuralSolver2DService().AnalyzeStaticForceEnvelope(model,
            new[] { K("SECOND"), K("FIRST") });
        Assert.Equal("SECOND", result.GetMember("BEAM")
            .MaximumAbsolute(InternalForceQuantity.BendingMoment).ResultId);
    }

    [Fact]
    public void MultipleMembers_OneResultPerFrameMemberInModelOrder()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddNode(new StructuralNode("C", 4, 0))
            .AddMaterial(new StructuralMaterial("S", "steel", 210_000_000))
            .AddSection(new StructuralSection("R", "section", .004, .00002))
            .AddMember(new StructuralMember("E1", "A", "B", "S", "R", MemberType.Frame2D))
            .AddMember(new StructuralMember("E2", "B", "C", "S", "R", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("G", "permanent"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("L", "G", "E2", StructuralLoadDirection.GlobalY, -10));
        var envelope = new StructuralSolver2DService().AnalyzeStaticForceEnvelope(model, new[] { C("G") });
        Assert.Equal(new[] { "E1", "E2" }, envelope.Members.Select(item => item.MemberId));
        Assert.Equal(-60, envelope.GetMember("E1").Minimum(InternalForceQuantity.BendingMoment).Value, 6);
    }

    [Fact]
    public void InvalidSelections_AreRejectedWithStableDiagnostics()
    {
        StructuralModel model = Beam();
        StructuralSolver2DService service = new();
        Assert.Contains("M79_EMPTY_TARGETS", Assert.Throws<StructuralAnalysisException>(() =>
            service.AnalyzeStaticForceEnvelope(model, Array.Empty<StaticForceEnvelopeScenarioM79>())).Message);
        Assert.Contains("M79_DUPLICATE_TARGET", Assert.Throws<StructuralAnalysisException>(() =>
            service.AnalyzeStaticForceEnvelope(model, new[] { C("G"), C("g") })).Message);
        Assert.Contains("M79_UNKNOWN_TARGET", Assert.Throws<StructuralAnalysisException>(() =>
            service.AnalyzeStaticForceEnvelope(model, new[] { K("NOPE") })).Message);
        Assert.Contains("M79_INVALID_TARGET", Assert.Throws<StructuralAnalysisException>(() =>
            service.AnalyzeStaticForceEnvelope(model, new[] { new StaticForceEnvelopeScenarioM79((StructuralAnalysisTargetKind)99, "G") })).Message);
        Assert.Contains("M79_INVALID_TARGET", Assert.Throws<StructuralAnalysisException>(() =>
            service.AnalyzeStaticForceEnvelope(model, new[] { C(" ") })).Message);
    }

    [Fact]
    public void CollisionBetweenCaseAndCombinationId_IsRejectedBeforePostProcessing()
    {
        StructuralModel model = Beam().AddLoadCombination(
            new StructuralLoadCombination("G", "collision", new[] { new StructuralLoadCombinationTerm("G", 1) }));
        var exception = Assert.Throws<StructuralAnalysisException>(() =>
            new StructuralSolver2DService().AnalyzeStaticForceEnvelope(model, new[] { C("G") }));
        Assert.Contains("M79_AMBIGUOUS_ID", exception.Message);
    }

    [Fact]
    public void TrussOnlyModel_IsExplicitlyUnsupportedByFrameMomentEnvelope()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 2, 0))
            .AddMember(new StructuralMember("T", "A", "B", "S", "R", MemberType.Truss2D));
        var exception = Assert.Throws<StructuralAnalysisException>(() =>
            new StructuralSolver2DService().AnalyzeStaticForceEnvelope(model, new[] { C("G") }));
        Assert.Contains("M79_NO_FRAME_MEMBERS", exception.Message);
    }
}
