using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Model.Generators;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

public sealed class CoreGeneratorAnalysisRegressionTests
{
    [Theory]
    [InlineData(4.0, 3.0, 5.0)]
    [InlineData(2.0, 4.0, 3.0)]
    public void Gerber_ShouldMatchAnalyticalReactionsAndReleaseMoments(double left, double suspended, double right)
    {
        var model = ParametricBeamGenerator.GerberBeamWithAsymmetricLoads(left, suspended, right);
        var result = new Frame2DAnalyzer().Analyze(model, "LC1");

        // The suspended span transfers 7 * (1 - 0.35) and 7 * 0.35 kN to its two cantilevers.
        Assert.Equal(12 + 7 * 0.65, result.GetReaction("S1").Fy, precision: 7);
        Assert.Equal(18 + 7 * 0.35, result.GetReaction("S2").Fy, precision: 7);
        Assert.Equal((12 * 0.65 + 7 * 0.65) * left, result.GetReaction("S1").Mz, precision: 7);
        Assert.Equal(-(18 * 0.30 + 7 * 0.35) * right, result.GetReaction("S2").Mz, precision: 7);
        Assert.Equal(0, result.GetReaction("S2").Fx, precision: 9);

        foreach (var member in model.Members)
        {
            var forces = result.MemberEndForces.Single(force => force.MemberId == member.Id);
            if (member.ReleaseStartMoment) Assert.Equal(0, forces.StartMoment, precision: 9);
            if (member.ReleaseEndMoment) Assert.Equal(0, forces.EndMoment, precision: 9);
        }
        Assert.All(result.Displacements, displacement => Assert.True(double.IsFinite(displacement.Uy)));
    }

    [Theory]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    [InlineData(false, 6)]
    [InlineData(false, 8)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    [InlineData(true, 5)]
    [InlineData(true, 6)]
    [InlineData(true, 8)]
    public void ParabolicTruss_ShouldSatisfyNodalEquilibriumUnderDeckLoads(bool inverted, int panels)
    {
        var model = Parabolic(inverted, panels);
        for (int i = 1; i < panels; i++)
            model.AddLoad(StructuralLoad.NodalForce($"P{i}", "LC1", $"B{i}", StructuralLoadDirection.GlobalY, -10 - i));

        var result = new Truss2DAnalyzer().Analyze(model, "LC1");
        double totalLoad = -model.Loads.Sum(load => load.Value);
        double rightReaction = -model.Loads.Sum(load => load.Value * model.Nodes.Single(n => n.Id == load.TargetId).X) / 10;
        Assert.Equal(rightReaction, result.GetReaction("S2").Fy, precision: 6);
        Assert.Equal(totalLoad - rightReaction, result.GetReaction("S1").Fy, precision: 6);
        Assert.Equal(0, result.GetDisplacement("B0").Ux, precision: 9);
        Assert.Equal(0, result.GetDisplacement("B0").Uy, precision: 9);
        Assert.Equal(0, result.GetDisplacement($"B{panels}").Uy, precision: 9);
        Assert.All(result.Displacements, d => Assert.True(double.IsFinite(d.Ux) && double.IsFinite(d.Uy)));
        Assert.InRange(result.Displacements.Max(d => Math.Abs(d.Uy)), 1e-8, 0.01);

        foreach (var node in model.Nodes)
        {
            double internalX = 0, internalY = 0;
            foreach (var member in model.Members.Where(m => m.StartNodeId == node.Id || m.EndNodeId == node.Id))
            {
                var start = model.Nodes.Single(n => n.Id == member.StartNodeId);
                var end = model.Nodes.Single(n => n.Id == member.EndNodeId);
                double length = StructuralMember.GetLength(start, end);
                var forces = result.MemberEndForces.Single(f => f.MemberId == member.Id);
                double force = member.StartNodeId == node.Id ? forces.StartAxial : forces.EndAxial;
                internalX += force * (end.X - start.X) / length;
                internalY += force * (end.Y - start.Y) / length;
            }
            Assert.Equal(result.Reactions.Where(r => r.NodeId == node.Id).Sum(r => r.Fx), internalX, precision: 6);
            double appliedY = model.Loads.Where(load => load.TargetId == node.Id).Sum(load => load.Value);
            Assert.Equal(appliedY + result.Reactions.Where(r => r.NodeId == node.Id).Sum(r => r.Fy), internalY, precision: 6);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TwoPanelParabolicTruss_ShouldMatchVirtualWorkDeflection(bool inverted)
    {
        var model = Parabolic(inverted, 2);
        model.AddLoad(StructuralLoad.NodalForce("P", "LC1", "B1", StructuralLoadDirection.GlobalY, -10));
        var result = new Truss2DAnalyzer().Analyze(model, "LC1");

        // Unit-load method: two 5 m deck bars, two sqrt(29) m inclined bars and a 2 m vertical.
        double diagonal = Math.Sqrt(29);
        double expected = -10 / (210_000_000.0 * 0.01) * (125.0 / 8 + Math.Pow(diagonal, 3) / 8 + 2);
        Assert.Equal(expected, result.GetDisplacement("B1").Uy, precision: 10);
    }

    [Theory]
    [InlineData("Frame")]
    [InlineData("Truss")]
    [InlineData("Plane")]
    public void Analyze_ShouldRejectAmbiguousLoadTargetBeforeProducingResults(string analyzer)
    {
        var model = analyzer == "Truss"
            ? ParametricTrussGenerator.IsostaticTriangularTruss(5, 2)
            : ParametricBeamGenerator.SimplySupportedBeam(5, 1);
        model.AddLoadCombination(new StructuralLoadCombination("lc1", "Collision",
            new[] { new StructuralLoadCombinationTerm("LC1", 2) }));

        void Analyze(bool combination)
        {
            _ = analyzer switch
            {
                "Truss" => combination ? new Truss2DAnalyzer().AnalyzeCombination(model, "lc1") : new Truss2DAnalyzer().Analyze(model, "LC1"),
                "Frame" => combination ? new Frame2DAnalyzer().AnalyzeCombination(model, "lc1") : new Frame2DAnalyzer().Analyze(model, "LC1"),
                _ => combination ? new PlaneStructureAnalyzer().AnalyzeCombination(model, "lc1") : new PlaneStructureAnalyzer().Analyze(model, "LC1")
            };
        }

        var caseError = Assert.Throws<StructuralAnalysisException>(() => Analyze(false));
        var combinationError = Assert.Throws<StructuralAnalysisException>(() => Analyze(true));
        Assert.Contains(caseError.ValidationIssues, issue => issue.Code == "LOAD_COMBINATION_ID_CONFLICT");
        Assert.Contains(combinationError.ValidationIssues, issue => issue.Code == "LOAD_COMBINATION_ID_CONFLICT");
    }

    [Theory]
    [InlineData(false, 25.0, 31.25)]
    [InlineData(true, 50.0, 62.5)]
    public void DistinctLoadTargets_ShouldProduceConsistentReactionsAndDiagrams(bool combination, double reaction, double midMoment)
    {
        var model = ParametricBeamGenerator.SimplySupportedBeam(5, 1)
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q", "LC1", "M1", StructuralLoadDirection.GlobalY, -10))
            .AddLoadCombination(new StructuralLoadCombination("COMB", "Double load",
                new[] { new StructuralLoadCombinationTerm("LC1", 2) }));
        var analyzer = new Frame2DAnalyzer();
        var result = combination ? analyzer.AnalyzeCombination(model, "COMB") : analyzer.Analyze(model, "LC1");
        var diagram = new Frame2DInternalForceSampler().SampleMember(model, result, "M1", 5);

        Assert.Equal(reaction, result.GetReaction("S1").Fy, precision: 7);
        Assert.Equal(reaction, result.GetReaction("S2").Fy, precision: 7);
        Assert.Equal(midMoment, diagram.Samples[2].BendingMoment, precision: 7);
        Assert.Equal(0, diagram.Samples[0].BendingMoment, precision: 7);
        Assert.Equal(0, diagram.Samples[^1].BendingMoment, precision: 7);
    }

    private static StructuralModel Parabolic(bool inverted, int panels) => inverted
        ? ParametricTrussGenerator.InvertedParabolicTruss(10, 2, panels)
        : ParametricTrussGenerator.NielsenParabolicTruss(10, 2, panels);
}
