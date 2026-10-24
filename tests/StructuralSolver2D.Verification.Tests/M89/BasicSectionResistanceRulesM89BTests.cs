using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Integration;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M89;

public sealed class BasicSectionResistanceRulesM89BTests
{
    private static readonly DesignCodeEdition Code = new("EN 1993-1-1", "2005+A1:2014", "IT-USER-DECLARED-TEST");
    private static readonly CommercialRolledSteelCatalogM88B2 Sections =
        CommercialRolledSteelCatalogM88B2.Load(Path.Combine(AppContext.BaseDirectory, "steel-oppo-ipe-hea-heb-inp.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory, "characteristic-strength-classes.json"));

    // For IPE 200, OPPO: A=28.48 cm2, b=100 mm, tf=8.5 mm, tw=5.6 mm, r=12 mm.
    // Independent hand calculation, 6.2.6(3)(a) without a lower-limit override:
    // Av=[2848 - 2*100*8.5 + (5.6 + 2*12)*8.5] / 100 = 13.996 cm2.
    private const double AvExampleCm2 = 13.996;
    private static readonly SteelBasicSectionAssignmentM89B Assignment = new(
        "M1", "IPE-200", "S235", 3,
        "Test declaration: section class independently justified",
        NoRelevantHolesConfirmed: true,
        ShearAreaCm2: AvExampleCm2,
        ShearAreaEvidence: "Hand calculation from OPPO dimensions; EN 1993-1-1:2005 6.2.6(3)(a); eta lower bound independently checked",
        InPlaneShearAxisMappingConfirmed: true,
        ShearBucklingExcludedConfirmed: true,
        NoTorsionConfirmed: true);

    private static NormativeProfile Profile(double gamma = 1, DesignCodeEdition? code = null) => new(
        "USER-DEFINED-GAMMA-NOT-COMPLIANCE", code ?? Code,
        new[] { new NormativeCoefficient("gamma_M0", gamma, "TEST: designer provided, annex not automatically interpreted", "Explicit input") });

    private static DesignCheckRequest Request(double n = 0, double v = 0, double m = 0,
        string section = "IPE-200", string member = "M1", string? ruleId = null,
        NormativeProfile? profile = null) => new(
            new DesignMemberContext(member, DesignMaterial.Steel, section, 4),
            DesignLimitState.Ultimate, profile ?? Profile(),
            new DesignEffectSnapshot("FEM:linear-static:first-order", "ULS-1",
                DesignEffectOrigin.FemAnalysis, 2, n, v, m)
            { MemberId = member, TargetKind = DesignEffectTargetKind.LoadCombination,
                SectionSide = DesignEffectSectionSide.Continuous },
            new[] { ruleId ?? SteelGrossTensionYieldRuleM89B.Id });

    private static DesignCheckResult Check(string rule, DesignCheckRequest request,
        SteelBasicSectionAssignmentM89B? assignment = null, DesignCodeEdition? code = null)
    {
        var input = new[] { assignment ?? Assignment };
        SteelBasicSectionRuleM89B item = rule switch
        {
            SteelGrossTensionYieldRuleM89B.Id => new SteelGrossTensionYieldRuleM89B(code ?? Code, Sections, Materials, input),
            SteelGrossCompressionRuleM89B.Id => new SteelGrossCompressionRuleM89B(code ?? Code, Sections, Materials, input),
            SteelPureShearRuleM89B.Id => new SteelPureShearRuleM89B(code ?? Code, Sections, Materials, input),
            _ => throw new ArgumentOutOfRangeException(nameof(rule))
        };
        return Assert.Single(new DesignCheckEngine(new[] { item }).Evaluate(request with { RuleIds = new[] { rule } }).Results);
    }

    [Fact]
    public void PureTension_GrossYield_IndependentUnitsAndValue()
    {
        var result = Check(SteelGrossTensionYieldRuleM89B.Id, Request(n: 200));
        const double resistance = 28.48 * 235 * 0.1; // 669.28 kN, NOT net fracture strength
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(resistance, result.Resistance!.Value, 10);
        Assert.Equal(200 / resistance, result.Utilization!.Value, 12);
        Assert.Equal("kN", result.Unit);
        Assert.Contains("NET RUPTURE NOT checked", result.CalculationTrace!);
        Assert.Contains("OPPO", result.CalculationTrace!);
        Assert.Contains("ULS-1", result.CalculationTrace!);
        Assert.Contains("source=TEST: designer provided", result.CalculationTrace!);
    }

    [Fact]
    public void GrossTension_GammaIsNotDefaulted()
    {
        var p = Profile(1.25);
        var result = Check(SteelGrossTensionYieldRuleM89B.Id, Request(n: 100, profile: p));
        Assert.Equal(28.48 * 235 * 0.1 / 1.25, result.Resistance!.Value, 9);
        Assert.Equal(DesignCheckStatus.Passed, result.Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossTensionYieldRuleM89B.Id,
            Request(n: 100, profile: p with { Coefficients = Array.Empty<NormativeCoefficient>() })).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossTensionYieldRuleM89B.Id,
            Request(n: 100, profile: Profile(0))).Status);
    }

    [Theory]
    [InlineData(669.28, DesignCheckStatus.Passed)]
    [InlineData(669.281, DesignCheckStatus.Failed)]
    public void PureTension_AtAndAboveLimit(double n, DesignCheckStatus expected)
    {
        Assert.Equal(expected, Check(SteelGrossTensionYieldRuleM89B.Id, Request(n: n)).Status);
    }

    [Theory]
    [InlineData(-10, 0, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(10, 0.001, 0)]
    [InlineData(10, 0, -0.01)]
    public void Tension_DoesNotAcceptCompressionOrMixedActions(double n, double v, double m)
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(SteelGrossTensionYieldRuleM89B.Id, Request(n, v, m)).Status);
    }

    [Fact]
    public void TensionYieldAloneDoesNotBecomeNetSectionOrMemberApproval()
    {
        var rule = new SteelGrossTensionYieldRuleM89B(Code, Sections, Materials, new[] { Assignment });
        Assert.False(rule.EstablishesCompleteMemberCompliance);
        var r = Request(n: 25) with { RuleIds = new[] { rule.RuleId, "EC3-NET-FRACTURE-NOT-IMPLEMENTED" } };
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(r);
        Assert.Equal(DesignCheckStatus.Passed, report.Results[0].Status);
        Assert.Equal(DesignCheckStatus.Unsupported, report.Results[1].Status);
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
    }

    [Fact]
    public void PureCompression_Class3_IndependentGrossResistance()
    {
        var check = Check(SteelGrossCompressionRuleM89B.Id, Request(n: -150));
        Assert.Equal(DesignCheckStatus.Passed, check.Status);
        Assert.Equal(28.48 * 235 * 0.1, check.Resistance!.Value, 9);
        Assert.Equal(150, check.Demand!.Value);
        Assert.Contains("NO member buckling check", check.CalculationTrace!);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CompressionClasses1To3RequireIndependentEvidence(int classNumber)
    {
        var a = Assignment with { SectionClass = classNumber };
        Assert.Equal(DesignCheckStatus.Passed,
            Check(SteelGrossCompressionRuleM89B.Id, Request(n:-100), a).Status);
    }

    [Fact]
    public void Compression_Class4OrWrongSignIsNotApplicable()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:-100), Assignment with { SectionClass = 4 }).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:100)).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:0)).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable, Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:-100,m:0.01)).Status);
    }

    [Fact]
    public void CompressionMissingClassificationOrHoleEvidenceCannotPass()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:-100), Assignment with { SectionClass = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:-100), Assignment with { ClassificationEvidence = "" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:-100), Assignment with { NoRelevantHolesConfirmed = null }).Status);
    }

    [Fact]
    public void Shear_IndependentHandAreaAndVonMisesResistance()
    {
        // 13.996 cm2, fy=235 MPa, gamma=1; sqrt(3) yield condition.
        const double avIndependent = (2848 - 2 * 100 * 8.5 + (5.6 + 24) * 8.5) / 100;
        Assert.Equal(AvExampleCm2, avIndependent, 10);
        double expected = avIndependent * 235 / (10 * Math.Sqrt(3));
        var check = Check(SteelPureShearRuleM89B.Id, Request(v:-80));
        Assert.Equal(DesignCheckStatus.Passed, check.Status);
        Assert.Equal(expected, check.Resistance!.Value, 10);
        Assert.Equal(80, check.Demand!.Value);
        Assert.Equal(80 / expected, check.Utilization!.Value, 12);
        Assert.Contains("AvEvidence", check.CalculationTrace!);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void Shear_SignPreservedInTraceMagnitudeUsedForDemand(int sign)
    {
        var check = Check(SteelPureShearRuleM89B.Id, Request(v:sign*250));
        Assert.Equal(DesignCheckStatus.Failed, check.Status);
        Assert.Equal(250, check.Demand!.Value);
        Assert.Contains($"V={(sign*250)} kN", check.CalculationTrace!);
    }

    [Fact]
    public void ShearRequiredEvidenceCannotBeImplicitlyAssumed()
    {
        var r = Request(v:40);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { ShearAreaCm2 = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { ShearAreaEvidence = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { ShearAreaCm2 = 35 }).Status); // > gross area
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { ShearAreaCm2 = -4 }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { ShearBucklingExcludedConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { NoTorsionConfirmed = null }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { InPlaneShearAxisMappingConfirmed = false }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelPureShearRuleM89B.Id,
            r, Assignment with { NoRelevantHolesConfirmed = false }).Status);
    }

    [Fact]
    public void Shear_IsolatedRuleIsNotAMomentOrAxialInteractionCheck()
    {
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(SteelPureShearRuleM89B.Id, Request(v:100,n:-0.0001)).Status);
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Check(SteelPureShearRuleM89B.Id, Request(v:100,m:0.0001)).Status);
    }

    [Fact]
    public void DifferentSteelGradeAndThicknessUseCorrectYieldBand()
    {
        var s = Sections.Find("HEB-240").Definition;
        var a = Assignment with { SectionId = "HEB-240", SteelGrade = "S355" };
        double fy = Materials.GetSteelYieldMpa("S355", Math.Max(s.FlangeThicknessMm, s.WebThicknessMm));
        Assert.True(fy < 355);
        var check = Check(SteelGrossCompressionRuleM89B.Id,
            Request(n:-500,section:"HEB-240"), a);
        Assert.Equal(s.AreaCm2 * fy * 0.1, check.Resistance!.Value, 9);
    }

    [Fact]
    public void MissingOrIncorrectIdentificationFailsClosed()
    {
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossTensionYieldRuleM89B.Id,
            Request(n:100, section:"HEA-200")).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossTensionYieldRuleM89B.Id,
            Request(n:100), Assignment with { SteelGrade = "MISSING" }).Status);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossTensionYieldRuleM89B.Id,
            Request(n:100, section:"IPE-999"), Assignment with { SectionId = "IPE-999" }).Status);
        var r = Request(n:100);
        Assert.Equal(DesignCheckStatus.InvalidInput, Check(SteelGrossTensionYieldRuleM89B.Id,
            r with { Effects = r.Effects with { MemberId = "OTHER" } }).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput, Check(SteelGrossTensionYieldRuleM89B.Id,
            r with { Effects = r.Effects with { ShearForceYKn = double.NaN } }).Status);
    }

    [Fact]
    public void MissingActionComponentAndWrongNormativeEditionAreNotPasses()
    {
        var r = Request(n:100);
        Assert.Equal(DesignCheckStatus.MissingData, Check(SteelGrossTensionYieldRuleM89B.Id,
            r with { Effects = r.Effects with { ShearForceYKn = null } }).Status);
        Assert.Equal(DesignCheckStatus.Unsupported, Check(SteelGrossTensionYieldRuleM89B.Id,
            r with { Profile = Profile(code:new DesignCodeEdition("EN 1993-1-1","2022","IT")) }).Status);
        Assert.Throws<NotSupportedException>(() => new SteelPureShearRuleM89B(
            new DesignCodeEdition("EN 1993-1-1","2022","IT"), Sections, Materials, new[] { Assignment }));
    }

    [Fact]
    public void DuplicateMemberAssignmentsAndNoRulesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => new SteelGrossCompressionRuleM89B(Code, Sections, Materials,
            new[] { Assignment, Assignment }));
        var rule = new SteelGrossCompressionRuleM89B(Code, Sections, Materials, new[] { Assignment });
        var report = new DesignCheckEngine(new[] { rule }).Evaluate(Request(n:-100) with { RuleIds = Array.Empty<string>() });
        Assert.Equal(DesignOverallStatus.Incomplete, report.OverallStatus);
    }

    [Fact]
    public void FirstOrderFemCompressionRetainsItsSignAndAnalysisIdentity()
    {
        var profileSection = Sections.Find("IPE-200").ToSolverSection();
        var model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 4, 0))
            .AddMaterial(new StructuralMaterial("MAT", "Elastic steel", 210_000_000))
            .AddSection(profileSection)
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "IPE-200", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase("G", "Permanent"))
            .AddSupport(StructuralSupport.Fixed("S", "A"))
            .AddLoad(StructuralLoad.NodalForce("N", "G", "B", StructuralLoadDirection.GlobalX, -150));
        var rule = new SteelGrossCompressionRuleM89B(Code, Sections, Materials, new[] { Assignment });
        var report = new FemStationVerificationBridgeM87B().Evaluate(
            model, new DesignMemberContext("M1", DesignMaterial.Steel, "IPE-200", 4),
            DesignLimitState.Ultimate, Profile(), new[] { rule.RuleId },
            new FemStationSelectionM87B(StructuralAnalysisTargetKind.LoadCase, "G", 2),
            new DesignCheckEngine(new[] { rule }));
        var check = Assert.Single(report.Results);
        Assert.Equal(DesignCheckStatus.Passed, check.Status);
        Assert.Equal(150, check.Demand!.Value, 7);
        Assert.Contains("target=G", check.CalculationTrace!);
        Assert.Contains("analysis=FEM:linear-static:first-order", check.CalculationTrace!);
    }
}
