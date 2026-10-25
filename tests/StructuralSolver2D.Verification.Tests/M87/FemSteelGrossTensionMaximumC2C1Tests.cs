using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Integration;
using StructuralSolver2D.Verification.Rules;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M87;

public sealed class FemSteelGrossTensionMaximumC2C1Tests
{
    private const double Length = 4;
    private const double IndependentGrossYieldResistanceKn = 28.48 * 235 * 0.1; // 669.28 kN
    private static readonly DesignCodeEdition Code = new(
        "EN 1993-1-1", "2005+A1:2014", "IT-USER-DECLARED-TEST");
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory,
            "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));
    private static readonly DesignMemberContext Member = new("M1", DesignMaterial.Steel, "IPE-200", Length);
    private static readonly NormativeProfile Profile = new("C2C1-EXPLICIT-TEST-COEFFICIENT", Code,
        new[] { new NormativeCoefficient("gamma_M0", 1, "TEST: explicit selected coefficient", "No default") });

    private static IDesignCheckRule Rule(SteelBasicSectionAssignmentM89B? assignment = null) =>
        new SteelGrossTensionYieldRuleM89B(Code, Sections, Materials,
            new[] { assignment ?? new SteelBasicSectionAssignmentM89B("M1", "IPE-200", "S235") });

    private static StructuralModel Model() => new StructuralModel()
        .AddNode(new StructuralNode("A", 0, 0))
        .AddNode(new StructuralNode("B", Length, 0))
        .AddMaterial(new StructuralMaterial("STEEL", "Steel", 210_000_000, 78.5))
        .AddSection(new StructuralSection("IPE-200", "IPE nominal test section", .002848, .00002))
        .AddMember(new StructuralMember("M1", "A", "B", "STEEL", "IPE-200", MemberType.Frame2D))
        .AddSupport(StructuralSupport.Fixed("FIXED", "A"))
        .AddLoadCase(new StructuralLoadCase("G", "G"))
        .AddLoadCase(new StructuralLoadCase("Q", "Q"));

    private static FemSteelGrossTensionMaximumResultC2C1 Check(StructuralModel model,
        string target = "G", StructuralAnalysisTargetKind kind = StructuralAnalysisTargetKind.LoadCase,
        IDesignCheckRule? rule = null, NormativeProfile? profile = null) =>
        FemSteelGrossTensionMaximumC2C1.Evaluate(model, Member, profile ?? Profile,
            kind, target, rule ?? Rule());

    [Fact]
    public void RealEc3GrossYieldAndFirstOrderFem_ConstantPositiveAxial_GivesIndependentGolden()
    {
        StructuralModel model = Model().AddLoad(StructuralLoad.NodalForce("N", "G", "B",
            StructuralLoadDirection.GlobalX, 200));
        var result = Check(model);

        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.RuleSpecificMaximumUnderConstantActionAssumptions,
            result.Status);
        Assert.Equal("C2C1_GROSS_YIELD_RATIO_LOCATED", result.ReasonCode);
        Assert.Equal(200 / IndependentGrossYieldResistanceKn,
            result.ConstantGrossYieldUtilization!.Value, 12);
        Assert.Equal(2, result.StationAudit!.Observations.Count);
        Assert.Equal(0, result.StationAudit.Observations[0].DistanceMetres);
        Assert.Equal(Length, result.StationAudit.Observations[1].DistanceMetres);
        Assert.All(result.StationAudit.Observations,
            observation => Assert.Equal(DesignCheckStatus.Passed, observation.RuleStatus));
        Assert.True(result.SpecificRuleMaximumLocatedWithinIdealizedModel);
        Assert.False(result.MemberAdequacyEstablished);
        Assert.False(result.AllApplicableRulesChecked);
        Assert.False(result.CompleteActionSetEstablished);
        Assert.False(result.WholeStructureVerified);
        Assert.False(result.RuleImplementationIndependentlyAuthenticated);
        Assert.False(result.NumericalIntervalErrorBoundEstablished);
    }

    [Fact]
    public void ActualGrossYieldFailure_RemainsVisible_WithoutWholeMemberClaim()
    {
        var result = Check(Model().AddLoad(StructuralLoad.NodalForce("N", "G", "B",
            StructuralLoadDirection.GlobalX, 700)));

        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ObservedRuleFailure, result.Status);
        Assert.Equal(700 / IndependentGrossYieldResistanceKn,
            result.ConstantGrossYieldUtilization!.Value, 12);
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.ObservedRuleFailure,
            result.StationAudit!.Status);
        Assert.False(result.MemberAdequacyEstablished);
    }

    [Theory]
    [InlineData(StructuralLoadDirection.GlobalY, -10)]
    [InlineData(StructuralLoadDirection.GlobalX, -200)]
    public void NonPureOrNonTensileNodalActions_NeverGetAConstantTensionCertificate(
        StructuralLoadDirection direction, double value)
    {
        var result = Check(Model().AddLoad(StructuralLoad.NodalForce("LOAD", "G", "B",
            direction, value)));

        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete, result.Status);
        Assert.Equal("C2C1_NOT_STRICT_PURE_CONSTANT_TENSION", result.ReasonCode);
        Assert.Null(result.ConstantGrossYieldUtilization);
        Assert.Null(result.StationAudit);
    }

    [Fact]
    public void TinyButNonzeroTransverseForce_CannotBeRoundedIntoPureTension()
    {
        StructuralModel model = Model()
            .AddLoad(StructuralLoad.NodalForce("T", "G", "B", StructuralLoadDirection.GlobalX, 200))
            .AddLoad(StructuralLoad.NodalForce("V", "G", "B", StructuralLoadDirection.GlobalY, 1e-7));

        var result = Check(model);
        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete, result.Status);
        Assert.Equal("C2C1_NOT_STRICT_PURE_CONSTANT_TENSION", result.ReasonCode);
    }

    [Fact]
    public void PureNodalEndCouple_AlsoViolatesThePureAxialRule()
    {
        StructuralModel model = Model()
            .AddLoad(StructuralLoad.NodalForce("T", "G", "B", StructuralLoadDirection.GlobalX, 200))
            .AddLoad(StructuralLoad.NodalMoment("M", "G", "B", 15));

        var result = Check(model);
        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete, result.Status);
    }

    [Fact]
    public void MemberPointLoadAndDistributedMemberLoad_AreRejectedBeforeAnyFalseConstantClaim()
    {
        StructuralModel point = Model().AddLoad(StructuralLoad.PointLoadOnMember(
            "POINT", "G", "M1", StructuralLoadDirection.GlobalX, 10, .5));
        StructuralModel distributed = Model().AddLoad(StructuralLoad.UniformDistributedLoad(
            "UDL", "G", "M1", StructuralLoadDirection.GlobalX, 5));

        foreach (StructuralModel model in new[] { point, distributed })
        {
            var result = Check(model);
            Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete, result.Status);
            Assert.Equal("C2C1_MEMBER_LOAD_UNSUPPORTED", result.ReasonCode);
            Assert.Null(result.ConstantGrossYieldUtilization);
        }
    }

    [Fact]
    public void ModelSelfWeight_IsExplicitlyOutsideThisPilotEvenWhenOtherLoadsArePureAxial()
    {
        StructuralModel model = Model()
            .AddLoad(StructuralLoad.NodalForce("N", "G", "B", StructuralLoadDirection.GlobalX, 200))
            .AddLoad(StructuralLoad.SelfWeightLoad("SW", "G"));

        var result = Check(model);
        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete, result.Status);
        Assert.Equal("C2C1_MEMBER_LOAD_UNSUPPORTED", result.ReasonCode);
    }

    [Fact]
    public void SignedCombination_UsesActualFemTarget_NotIndependentEnvelope()
    {
        StructuralModel model = Model()
            .AddLoad(StructuralLoad.NodalForce("G-P", "G", "B", StructuralLoadDirection.GlobalX, 300))
            .AddLoad(StructuralLoad.NodalForce("Q-P", "Q", "B", StructuralLoadDirection.GlobalX, -100))
            .AddLoadCombination(new StructuralLoadCombination("ULS-TEST", "Manual example only", new[]
            {
                new StructuralLoadCombinationTerm("G", 1.5),
                new StructuralLoadCombinationTerm("Q", .5)
            }));

        var result = Check(model, "ULS-TEST", StructuralAnalysisTargetKind.LoadCombination);
        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.RuleSpecificMaximumUnderConstantActionAssumptions,
            result.Status);
        Assert.Equal(400 / IndependentGrossYieldResistanceKn, result.ConstantGrossYieldUtilization!.Value, 12);
        Assert.All(result.StationAudit!.Observations, o => Assert.Equal("ULS-TEST", o.TargetId));
    }

    [Fact]
    public void MissingRealNormativeData_RemainsIncomplete()
    {
        StructuralModel model = Model().AddLoad(StructuralLoad.NodalForce("N", "G", "B",
            StructuralLoadDirection.GlobalX, 200));
        var missingGrade = Check(model, rule: Rule(new SteelBasicSectionAssignmentM89B("M1", "IPE-200", "UNKNOWN-GRADE")));
        var missingGamma = Check(model, profile: Profile with { Coefficients = Array.Empty<NormativeCoefficient>() });

        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete, missingGrade.Status);
        Assert.Equal("C2C1_RULE_NONUNIFORM_OR_INCOMPLETE", missingGrade.ReasonCode);
        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.ScreeningIncomplete, missingGamma.Status);
        Assert.Equal("C2C1_RULE_NONUNIFORM_OR_INCOMPLETE", missingGamma.ReasonCode);
        Assert.Null(missingGrade.ConstantGrossYieldUtilization);
        Assert.Null(missingGamma.ConstantGrossYieldUtilization);
    }

    [Fact]
    public void RuleOrTargetMismatch_IsInvalid_NotPromotedToAnyResult()
    {
        StructuralModel model = Model().AddLoad(StructuralLoad.NodalForce("N", "G", "B",
            StructuralLoadDirection.GlobalX, 200));
        var unsupportedRule = new SteelGrossCompressionRuleM89B(Code, Sections, Materials,
            new[] { new SteelBasicSectionAssignmentM89B("M1", "IPE-200", "S235") });

        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
            Check(model, rule: unsupportedRule).Status);
        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
            Check(model, target: "MISSING").Status);
        Assert.Equal(FemSteelGrossTensionMaximumStatusC2C1.InvalidInput,
            Check(model, kind: StructuralAnalysisTargetKind.LoadCombination).Status);
    }
}
