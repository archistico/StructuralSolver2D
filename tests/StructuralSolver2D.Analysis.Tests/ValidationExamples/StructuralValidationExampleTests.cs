using StructuralSolver2D.Analysis.Equilibrium;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Model.Materials;
using StructuralSolver2D.Core.Model.Sections;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Tests.ValidationExamples;

/// <summary>
/// Regression tests for representative educational structural schemes.
/// These tests are intentionally qualitative: they verify that each example is valid, solvable,
/// finite and globally balanced. They are not normative design checks.
/// </summary>
public sealed class StructuralValidationExampleTests
{
    public static IEnumerable<object[]> ValidationExamples()
    {
        yield return new object[] { CreateRigidJointPortalFrame() };
        yield return new object[] { CreateSmallBridgeTruss() };
        yield return new object[] { CreateIsostaticTrussBeam() };
        yield return new object[] { CreateNielsenParabolicTruss() };
        yield return new object[] { CreateInvertedParabolicIsostaticTruss() };
        yield return new object[] { CreateHyperstaticDoubleDiagonalTruss() };
        yield return new object[] { CreateGerberBeamWithAsymmetricLoads() };
    }

    [Theory]
    [MemberData(nameof(ValidationExamples))]
    public void ValidationExample_ShouldBeAcceptedByModelValidator(StructuralValidationExample example)
    {
        StructuralModelValidationResult validation = new StructuralModelValidator().Validate(example.Model);

        Assert.True(
            validation.IsValid,
            $"Example '{example.Name}' should be valid. Issues: {string.Join("; ", validation.Issues.Select(issue => issue.Message))}");
    }

    [Theory]
    [MemberData(nameof(ValidationExamples))]
    public void ValidationExample_ShouldAnalyzeWithPublicApiAndRemainInGlobalEquilibrium(StructuralValidationExample example)
    {
        var service = new StructuralSolver2DService();
        var options = new StructuralAnalysisOptions
        {
            InternalForceSampleCount = 9,
            DisplacementSampleCount = 9,
            IncludeDisplacementDiagrams = true,
        };

        StructuralAnalysisOutput output = service.AnalyzeLoadCase(example.Model, example.LoadCaseId, options);
        GlobalEquilibriumResult equilibrium = new GlobalEquilibriumChecker().Check(example.Model, output.Result);
        int frameMemberCount = example.Model.Members.Count(member => member.Type == MemberType.Frame2D);

        Assert.Equal(example.Model.Nodes.Count, output.Result.Displacements.Count);
        Assert.Equal(example.Model.Supports.Count, output.Result.Reactions.Count);
        Assert.Equal(example.Model.Members.Count, output.Result.MemberEndForces.Count);
        Assert.Equal(example.Model.Members.Count, output.InternalForceDiagrams.Count);
        Assert.Equal(frameMemberCount, output.DisplacementDiagrams.Count);
        Assert.True(equilibrium.IsInEquilibrium(1e-6), $"Example '{example.Name}' is not in global equilibrium: Fx={equilibrium.ResidualFx}, Fy={equilibrium.ResidualFy}, Mz={equilibrium.ResidualMz}.");

        AssertFinite(output.Result.Displacements.SelectMany(displacement => new[] { displacement.Ux, displacement.Uy, displacement.Rz }));
        AssertFinite(output.Result.Reactions.SelectMany(reaction => new[] { reaction.Fx, reaction.Fy, reaction.Mz }));
        AssertFinite(output.Result.MemberEndForces.SelectMany(force => new[]
        {
            force.StartAxial,
            force.StartShear,
            force.StartMoment,
            force.EndAxial,
            force.EndShear,
            force.EndMoment,
        }));
    }

    private static void AssertFinite(IEnumerable<double> values)
    {
        foreach (double value in values)
        {
            Assert.True(double.IsFinite(value), $"Expected a finite numerical result, but found {value}.");
        }
    }

    private static StructuralValidationExample CreateRigidJointPortalFrame()
    {
        StructuralModel model = CreateBaseFrameModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0, "Left fixed base"))
            .AddNode(new StructuralNode("B", 6.0, 0.0, "Right fixed base"))
            .AddNode(new StructuralNode("C", 0.0, 4.0, "Left rigid knee"))
            .AddNode(new StructuralNode("D", 6.0, 4.0, "Right rigid knee"))
            .AddMember(Frame("COL-L", "A", "C"))
            .AddMember(Frame("BEAM", "C", "D"))
            .AddMember(Frame("COL-R", "B", "D"))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.Fixed("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q-BEAM", "LC1", "BEAM", StructuralLoadDirection.GlobalY, -18.0, "Gravity load on rigid beam"))
            .AddLoad(StructuralLoad.NodalForce("H-D", "LC1", "D", StructuralLoadDirection.GlobalX, 12.0, "Horizontal frame action"));

        return new StructuralValidationExample(
            "Rigid-joint portal frame",
            "Telaio piano a nodi rigidi con basi incastrate, carico verticale sulla trave e azione orizzontale.",
            model,
            "LC1");
    }

    private static StructuralValidationExample CreateSmallBridgeTruss()
    {
        StructuralModel model = CreateBaseTrussModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 3.0, 0.0))
            .AddNode(new StructuralNode("C", 6.0, 0.0))
            .AddNode(new StructuralNode("D", 9.0, 0.0))
            .AddNode(new StructuralNode("E", 12.0, 0.0))
            .AddNode(new StructuralNode("T1", 3.0, 2.4))
            .AddNode(new StructuralNode("T2", 6.0, 2.8))
            .AddNode(new StructuralNode("T3", 9.0, 2.4))
            .AddMember(Truss("AB", "A", "B"))
            .AddMember(Truss("BC", "B", "C"))
            .AddMember(Truss("CD", "C", "D"))
            .AddMember(Truss("DE", "D", "E"))
            .AddMember(Truss("A-T1", "A", "T1"))
            .AddMember(Truss("T1-T2", "T1", "T2"))
            .AddMember(Truss("T2-T3", "T2", "T3"))
            .AddMember(Truss("T3-E", "T3", "E"))
            .AddMember(Truss("B-T1", "B", "T1"))
            .AddMember(Truss("C-T2", "C", "T2"))
            .AddMember(Truss("D-T3", "D", "T3"))
            .AddMember(Truss("T1-C", "T1", "C"))
            .AddMember(Truss("T2-D", "T2", "D"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SE", "E"))
            .AddLoad(StructuralLoad.NodalForce("P-B", "LC1", "B", StructuralLoadDirection.GlobalY, -20.0))
            .AddLoad(StructuralLoad.NodalForce("P-C", "LC1", "C", StructuralLoadDirection.GlobalY, -30.0))
            .AddLoad(StructuralLoad.NodalForce("P-D", "LC1", "D", StructuralLoadDirection.GlobalY, -20.0));

        return new StructuralValidationExample(
            "Small bridge truss",
            "Piccolo ponte reticolare con montanti e diagonali, appoggiato a cerniera e carrello.",
            model,
            "LC1");
    }

    private static StructuralValidationExample CreateIsostaticTrussBeam()
    {
        StructuralModel model = CreateBaseTrussModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 4.0, 0.0))
            .AddNode(new StructuralNode("C", 8.0, 0.0))
            .AddNode(new StructuralNode("T", 4.0, 2.6))
            .AddMember(Truss("AB", "A", "B"))
            .AddMember(Truss("BC", "B", "C"))
            .AddMember(Truss("AT", "A", "T"))
            .AddMember(Truss("BT", "B", "T"))
            .AddMember(Truss("TC", "T", "C"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SC", "C"))
            .AddLoad(StructuralLoad.NodalForce("P-T", "LC1", "T", StructuralLoadDirection.GlobalY, -35.0));

        return new StructuralValidationExample(
            "Isostatic triangular truss beam",
            "Trave reticolare isostatica triangolare con carico verticale sul nodo superiore.",
            model,
            "LC1");
    }

    private static StructuralValidationExample CreateNielsenParabolicTruss()
    {
        StructuralModel model = CreateBaseTrussModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 3.0, 0.0))
            .AddNode(new StructuralNode("C", 6.0, 0.0))
            .AddNode(new StructuralNode("D", 9.0, 0.0))
            .AddNode(new StructuralNode("E", 12.0, 0.0))
            .AddNode(new StructuralNode("T1", 3.0, 2.0))
            .AddNode(new StructuralNode("T2", 6.0, 3.0))
            .AddNode(new StructuralNode("T3", 9.0, 2.0))
            .AddMember(Truss("AB", "A", "B"))
            .AddMember(Truss("BC", "B", "C"))
            .AddMember(Truss("CD", "C", "D"))
            .AddMember(Truss("DE", "D", "E"))
            .AddMember(Truss("A-T1", "A", "T1"))
            .AddMember(Truss("T1-T2", "T1", "T2"))
            .AddMember(Truss("T2-T3", "T2", "T3"))
            .AddMember(Truss("T3-E", "T3", "E"))
            .AddMember(Truss("T1-C", "T1", "C"))
            .AddMember(Truss("T2-B", "T2", "B"))
            .AddMember(Truss("T2-D", "T2", "D"))
            .AddMember(Truss("T3-C", "T3", "C"))
            .AddMember(Truss("T2-C", "T2", "C"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SE", "E"))
            .AddLoad(StructuralLoad.NodalForce("P-B", "LC1", "B", StructuralLoadDirection.GlobalY, -12.0))
            .AddLoad(StructuralLoad.NodalForce("P-C", "LC1", "C", StructuralLoadDirection.GlobalY, -18.0))
            .AddLoad(StructuralLoad.NodalForce("P-D", "LC1", "D", StructuralLoadDirection.GlobalY, -12.0));

        return new StructuralValidationExample(
            "Nielsen-style parabolic truss",
            "Schema reticolare didattico con corrente superiore parabolico e pendini inclinati tipo Nielsen.",
            model,
            "LC1");
    }

    private static StructuralValidationExample CreateInvertedParabolicIsostaticTruss()
    {
        StructuralModel model = CreateBaseTrussModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 3.0, 0.0))
            .AddNode(new StructuralNode("C", 6.0, 0.0))
            .AddNode(new StructuralNode("D", 9.0, 0.0))
            .AddNode(new StructuralNode("E", 12.0, 0.0))
            .AddNode(new StructuralNode("P1", 3.0, -2.0))
            .AddNode(new StructuralNode("P2", 6.0, -3.0))
            .AddNode(new StructuralNode("P3", 9.0, -2.0))
            .AddMember(Truss("AB", "A", "B"))
            .AddMember(Truss("BC", "B", "C"))
            .AddMember(Truss("CD", "C", "D"))
            .AddMember(Truss("DE", "D", "E"))
            .AddMember(Truss("A-P1", "A", "P1"))
            .AddMember(Truss("P1-P2", "P1", "P2"))
            .AddMember(Truss("P2-P3", "P2", "P3"))
            .AddMember(Truss("P3-E", "P3", "E"))
            .AddMember(Truss("P1-B", "P1", "B"))
            .AddMember(Truss("P2-C", "P2", "C"))
            .AddMember(Truss("P3-D", "P3", "D"))
            .AddMember(Truss("P1-C", "P1", "C"))
            .AddMember(Truss("P2-D", "P2", "D"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SE", "E"))
            .AddLoad(StructuralLoad.NodalForce("P-B", "LC1", "B", StructuralLoadDirection.GlobalY, -10.0))
            .AddLoad(StructuralLoad.NodalForce("P-C", "LC1", "C", StructuralLoadDirection.GlobalY, -20.0))
            .AddLoad(StructuralLoad.NodalForce("P-D", "LC1", "D", StructuralLoadDirection.GlobalY, -10.0));

        return new StructuralValidationExample(
            "Inverted parabolic isostatic truss",
            "Trave reticolare isostatica con corrente parabolico rovescio e carichi sui nodi superiori.",
            model,
            "LC1");
    }

    private static StructuralValidationExample CreateHyperstaticDoubleDiagonalTruss()
    {
        StructuralModel model = CreateBaseTrussModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 3.0, 0.0))
            .AddNode(new StructuralNode("C", 6.0, 0.0))
            .AddNode(new StructuralNode("D", 9.0, 0.0))
            .AddNode(new StructuralNode("E", 12.0, 0.0))
            .AddNode(new StructuralNode("T1", 3.0, 2.5))
            .AddNode(new StructuralNode("T2", 6.0, 2.5))
            .AddNode(new StructuralNode("T3", 9.0, 2.5))
            .AddMember(Truss("AB", "A", "B"))
            .AddMember(Truss("BC", "B", "C"))
            .AddMember(Truss("CD", "C", "D"))
            .AddMember(Truss("DE", "D", "E"))
            .AddMember(Truss("A-T1", "A", "T1"))
            .AddMember(Truss("T1-T2", "T1", "T2"))
            .AddMember(Truss("T2-T3", "T2", "T3"))
            .AddMember(Truss("T3-E", "T3", "E"))
            .AddMember(Truss("B-T1", "B", "T1"))
            .AddMember(Truss("C-T2", "C", "T2"))
            .AddMember(Truss("D-T3", "D", "T3"))
            .AddMember(Truss("A-T2", "A", "T2"))
            .AddMember(Truss("T1-C", "T1", "C"))
            .AddMember(Truss("B-T2", "B", "T2"))
            .AddMember(Truss("T2-D", "T2", "D"))
            .AddMember(Truss("C-T3", "C", "T3"))
            .AddMember(Truss("T1-D", "T1", "D"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SE", "E"))
            .AddLoad(StructuralLoad.NodalForce("P-B", "LC1", "B", StructuralLoadDirection.GlobalY, -15.0))
            .AddLoad(StructuralLoad.NodalForce("P-C", "LC1", "C", StructuralLoadDirection.GlobalY, -25.0))
            .AddLoad(StructuralLoad.NodalForce("P-D", "LC1", "D", StructuralLoadDirection.GlobalY, -15.0));

        return new StructuralValidationExample(
            "Hyperstatic double-diagonal truss",
            "Trave reticolare iperstatica con campi a doppia diagonale per testare ridondanza e stabilità numerica.",
            model,
            "LC1");
    }

    private static StructuralValidationExample CreateGerberBeamWithAsymmetricLoads()
    {
        StructuralModel model = CreateBaseFrameModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", 4.0, 0.0))
            .AddNode(new StructuralNode("C", 8.0, 0.0))
            .AddNode(new StructuralNode("D", 12.0, 0.0))
            .AddMember(Frame("AB", "A", "B", releaseEndMoment: true))
            .AddMember(Frame("BC", "B", "C", releaseStartMoment: true, releaseEndMoment: true))
            .AddMember(Frame("CD", "C", "D", releaseStartMoment: true))
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SC", "C"))
            .AddSupport(StructuralSupport.SimpleSupport("SD", "D"))
            .AddLoad(StructuralLoad.PointLoadOnMember("P-AB", "LC1", "AB", StructuralLoadDirection.GlobalY, -18.0, 0.35, "Asymmetric point load on left span"))
            .AddLoad(StructuralLoad.PointLoadOnMember("P-CD", "LC1", "CD", StructuralLoadDirection.GlobalY, -28.0, 0.70, "Asymmetric point load on right span"))
            .AddLoad(StructuralLoad.NodalForce("H-B", "LC1", "B", StructuralLoadDirection.GlobalX, 5.0, "Small horizontal perturbation"));

        return new StructuralValidationExample(
            "Gerber beam with asymmetric loads",
            "Trave Gerber didattica modellata con rilasci di momento e carichi asimmetrici sui campi.",
            model,
            "LC1");
    }

    private static StructuralModel CreateBaseFrameModel() =>
        new StructuralModel()
            .AddMaterial(StructuralMaterialLibrary.SteelS355("STEEL"))
            .AddSection(StructuralSectionFactory.Rectangular("FRAME", 0.30, 0.50, "Frame member section"))
            .AddLoadCase(new StructuralLoadCase("LC1", "Validation load case"));

    private static StructuralModel CreateBaseTrussModel() =>
        new StructuralModel()
            .AddMaterial(StructuralMaterialLibrary.SteelS355("STEEL"))
            .AddSection(StructuralSectionFactory.CircularHollow("TRUSS", 0.12, 0.10, "Truss bar section"))
            .AddLoadCase(new StructuralLoadCase("LC1", "Validation load case"));

    private static StructuralMember Frame(
        string id,
        string startNodeId,
        string endNodeId,
        bool releaseStartMoment = false,
        bool releaseEndMoment = false) =>
        new(id, startNodeId, endNodeId, "STEEL", "FRAME", MemberType.Frame2D, ReleaseStartMoment: releaseStartMoment, ReleaseEndMoment: releaseEndMoment);

    private static StructuralMember Truss(string id, string startNodeId, string endNodeId) =>
        new(id, startNodeId, endNodeId, "STEEL", "TRUSS", MemberType.Truss2D);

    public sealed record StructuralValidationExample(
        string Name,
        string Description,
        StructuralModel Model,
        string LoadCaseId)
    {
        public override string ToString() => Name;
    }
}
