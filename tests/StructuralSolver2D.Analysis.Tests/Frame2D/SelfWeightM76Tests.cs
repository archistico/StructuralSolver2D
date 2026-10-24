using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Model.Loads;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Tests.Frame2D;

/// <summary>Independent closed-form and load-equivalence checks for explicit M76 self-weight.</summary>
public sealed class SelfWeightM76Tests
{
    private const double E = 210_000_000.0; // kN/m²
    private const double Gamma = 78.5;       // kN/m³
    private const double A = 0.005;          // m²
    private const double I = 2.0e-5;         // m⁴
    private const double L = 4.0;           // m
    private const double Q = Gamma * A;     // kN/m, DOWNWARD

    private static StructuralModel Beam() => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0))
        .AddNode(new StructuralNode("B", L, 0))
        .AddMaterial(new StructuralMaterial("MAT", "steel", E, Gamma))
        .AddSection(new StructuralSection("SEC", "generic", A, I))
        .AddMember(new StructuralMember("M", "A", "B", "MAT", "SEC", MemberType.Frame2D))
        .AddSupport(StructuralSupport.Hinge("SA", "A"))
        .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
        .AddLoadCase(new StructuralLoadCase("G", "permanent"))
        .AddLoadCase(new StructuralLoadCase("Q", "variable"));

    [Fact]
    public void SimplySupportedBeam_ReactionsAndMomentMatchClosedFormAndExplicitLoad()
    {
        var self = Beam().AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"));
        var manual = Beam().AddLoad(StructuralLoad.UniformDistributedLoad("EXPLICIT", "G", "M", StructuralLoadDirection.GlobalY, -Q));
        var analysis = new Frame2DAnalyzer().Analyze(self, "G");
        var reference = new Frame2DAnalyzer().Analyze(manual, "G");
        var diagram = new Frame2DInternalForceSampler().SampleMember(self, analysis, "M", 21);
        Assert.Equal(Q * L / 2, analysis.GetReaction("SA").Fy, 8);
        Assert.Equal(Q * L / 2, analysis.GetReaction("SB").Fy, 8);
        Assert.Equal(Q * L * L / 8, diagram.GetClosestSample(0.5).BendingMoment, 8);
        Assert.Equal(reference.GetReaction("SA").Fy, analysis.GetReaction("SA").Fy, 9);
        Assert.Equal(reference.MemberEndForces.Single().StartShear, analysis.MemberEndForces.Single().StartShear, 9);
        var equilibrium = new GlobalEquilibriumChecker().Check(self, analysis);
        Assert.Equal(-Q * L, equilibrium.AppliedFy, 8);
        Assert.Equal(0, equilibrium.ResidualFy, 8);
        Assert.Equal(0, equilibrium.ResidualMz, 8);
    }

    [Fact]
    public void InclinedCantilever_ReleasesAndLocalDirectionsMatchAnExplicitGlobalYLoad()
    {
        foreach (bool releaseEnd in new[] { false, true })
        {
            var self = Beam();
            self.Members[0] = self.Members[0] with { ReleaseEndMoment = releaseEnd };
            self.Nodes[1] = new StructuralNode("B", 3, 4);
            self.Supports.Clear();
            self.AddSupport(StructuralSupport.Fixed("SA", "A"));
            self.AddLoad(StructuralLoad.SelfWeightLoad("G_SW", "G", scale: 1.2));
            var manual = Beam();
            manual.Members[0] = manual.Members[0] with { ReleaseEndMoment = releaseEnd };
            manual.Nodes[1] = new StructuralNode("B", 3, 4);
            manual.Supports.Clear();
            manual.AddSupport(StructuralSupport.Fixed("SA", "A"));
            manual.AddLoad(StructuralLoad.UniformDistributedLoad("MANUAL", "G", "M", StructuralLoadDirection.GlobalY, -Q * 1.2));
            var a = new Frame2DAnalyzer().Analyze(self, "G");
            var b = new Frame2DAnalyzer().Analyze(manual, "G");
            Assert.Equal(b.GetReaction("SA").Fy, a.GetReaction("SA").Fy, 8);
            Assert.Equal(b.GetReaction("SA").Mz, a.GetReaction("SA").Mz, 8);
            Assert.Equal(b.GetDisplacement("B").Uy, a.GetDisplacement("B").Uy, 10);
            var ad = new Frame2DInternalForceSampler().SampleMember(self, a, "M", 11);
            var bd = new Frame2DInternalForceSampler().SampleMember(manual, b, "M", 11);
            Assert.Equal(bd.GetClosestSample(0.5).BendingMoment, ad.GetClosestSample(0.5).BendingMoment, 8);
            var asampled = new Frame2DDisplacementSampler().SampleMember(self, a, "M", 11);
            var bsampled = new Frame2DDisplacementSampler().SampleMember(manual, b, "M", 11);
            Assert.Equal(bsampled.GetClosestSample(0.5).GlobalUy, asampled.GetClosestSample(0.5).GlobalUy, 10);
        }
    }

    [Fact]
    public void VerticalTruss_GivesEqualNodalBodyForcesAndExactTipDisplacement()
    {
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 0, L))
            .AddMaterial(new StructuralMaterial("MAT", "steel", E, Gamma))
            .AddSection(new StructuralSection("SEC", "generic", A, I))
            .AddMember(new StructuralMember("T", "A", "B", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", true, false, false))
            .AddLoadCase(new StructuralLoadCase("G", "weight"))
            .AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"));
        var expanded = SelfWeightStaticLoadExpander.Expand(model);
        Assert.Equal(2, expanded.Loads.Count);
        Assert.All(expanded.Loads, load => Assert.Equal(-Q * L / 2, load.Value, 9));
        var result = new Truss2DAnalyzer().Analyze(model, "G");
        Assert.Equal(Q * L, result.GetReaction("SA").Fy, 8);
        Assert.Equal(-Gamma * L * L / (2 * E), result.GetDisplacement("B").Uy, 10);
        var equilibrium = new GlobalEquilibriumChecker().Check(model, result);
        Assert.Equal(0, equilibrium.ResidualFy, 9);
    }

    [Fact]
    public void MixedPortal_FrameAndTrussLoadsAreBothIncludedWithoutDoubleCounting()
    {
        var self = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 3, 0))
            .AddNode(new StructuralNode("C", 0, 3))
            .AddNode(new StructuralNode("D", 3, 3))
            .AddMaterial(new StructuralMaterial("MAT", "steel", E, Gamma))
            .AddSection(new StructuralSection("SEC", "generic", A, I))
            .AddMember(new StructuralMember("F1", "A", "C", "MAT", "SEC"))
            .AddMember(new StructuralMember("F2", "C", "D", "MAT", "SEC"))
            .AddMember(new StructuralMember("F3", "B", "D", "MAT", "SEC"))
            .AddMember(new StructuralMember("T", "A", "D", "MAT", "SEC", MemberType.Truss2D))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("G", "weight"))
            .AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"));
        var expanded = SelfWeightStaticLoadExpander.Expand(self);
        Assert.Equal(5, expanded.Loads.Count);
        var a = new PlaneStructureAnalyzer().Analyze(self, "G");
        var b = new PlaneStructureAnalyzer().Analyze(expanded, "G");
        Assert.Equal(b.GetDisplacement("D").Uy, a.GetDisplacement("D").Uy, 10);
        Assert.Equal(b.GetReaction("SA").Fy, a.GetReaction("SA").Fy, 8);
        double weight = -Gamma * A * (3 + 3 + 3 + Math.Sqrt(18));
        Assert.Equal(weight, new GlobalEquilibriumChecker().Check(self, a).AppliedFy, 8);
        Assert.Equal(0, new GlobalEquilibriumChecker().Check(self, a).ResidualFy, 8);
        Assert.Single(self.Loads); // original never mutated
    }

    [Fact]
    public void LoadCombinations_WeightScalesOnce_AndRepeatedAnalysisNeverDuplicates()
    {
        var model = Beam()
            .AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", "Q", "M", StructuralLoadDirection.GlobalY, -2))
            .AddLoadCombination(new StructuralLoadCombination("COMBO", "test",
                new[] { new StructuralLoadCombinationTerm("G", -0.5), new StructuralLoadCombinationTerm("Q", 1.25) }));
        var analyzer = new Frame2DAnalyzer();
        var first = analyzer.AnalyzeCombination(model, "COMBO");
        var second = analyzer.AnalyzeCombination(model, "COMBO");
        double combinedDownward = -0.5 * Q + 1.25 * 2;
        Assert.Equal(combinedDownward * L / 2, first.GetReaction("SA").Fy, 8);
        Assert.Equal(first.GetReaction("SA").Fy, second.GetReaction("SA").Fy, 12);
        Assert.Equal(2, model.Loads.Count);
    }

    [Fact]
    public void InputValidation_RejectsMissingDensityDuplicateActionAndInvalidDirections()
    {
        var missing = Beam();
        missing.Materials[0] = missing.Materials[0] with { UnitWeight = null };
        missing.AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"));
        Assert.Contains(new StructuralModelValidator().Validate(missing).Errors,
            issue => issue.Code == "LOAD_SELF_WEIGHT_MISSING_UNIT_WEIGHT");

        var duplicate = Beam().AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"))
            .AddLoad(StructuralLoad.SelfWeightLoad("SW2", "G"));
        Assert.Contains(new StructuralModelValidator().Validate(duplicate).Errors,
            issue => issue.Code == "LOAD_SELF_WEIGHT_DUPLICATE_CASE");

        var incorrect = Beam().AddLoad(StructuralLoad.SelfWeightLoad("SW", "G") with { Direction = StructuralLoadDirection.LocalY });
        Assert.Contains(new StructuralModelValidator().Validate(incorrect).Errors,
            issue => issue.Code == "LOAD_INVALID_DIRECTION_FOR_TYPE");
    }
}
