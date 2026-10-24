using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

/// <summary>
/// G7: real G1 selector -> real G2/G3 design rule -> real G6 auditor.
/// Source values come from independent closed-form stresses and NTC coefficients,
/// NOT from G2/G3 output. External statements in this synthetic fixture are TEST-ONLY.
/// The audit must never imply whole-member or construction compliance.
/// </summary>
public sealed class NtcTimberValidationPipelineG7Tests
{
    private static readonly Ntc2018TimberCoefficientTablesG1 Tables =
        Ntc2018TimberCoefficientTablesG1.Load(Path.Combine(AppContext.BaseDirectory,
            "ntc2018-timber-coefficients-g1.json"));
    private static readonly CharacteristicMaterialCatalogM88C Materials =
        CharacteristicMaterialCatalogM88C.Load(Path.Combine(AppContext.BaseDirectory,
            "characteristic-strength-classes.json"));
    private static readonly Ntc2018TimberCoefficientSelectorG1 Selector = new(Tables, Materials);

    private const string MemberId = "G7-W1";
    private const string SectionId = "RECT-100x200";
    private const string AnalysisId = "G7-FEM-01";
    private const string CombinationId = "G7-ULS-01";

    private static Ntc2018TimberSelectedCoefficientsG1 Selected()
    {
        var a = new TimberRectangularAssignmentM92A(
            MemberId, SectionId, "C24", 100, 200, "G7 TEST: measured 100x200 mm",
            1, null, "G7 TEST: service class 1 proof", null, AnalysisId, CombinationId,
            DesignEffectTargetKind.LoadCombination, true, true, true, true, true);
        var actions = new[]
        {
            new Ntc2018CombinationActionG1("G", Ntc2018ActionCategoryG1.G1StructuralPermanent,
                TimberLoadDurationM92A.Permanent, 1.3, "G7 TEST permanent", "G7 TEST 1.3"),
            new Ntc2018CombinationActionG1("Q", Ntc2018ActionCategoryG1.QVariable,
                TimberLoadDurationM92A.MediumTerm, 1.5, "G7 TEST medium", "G7 TEST 1.5")
        };
        var input = new Ntc2018TimberSelectionG1(
            Ntc2018TimberCoefficientSelectorG1.Edition, a,
            Ntc2018TimberSituationG1.FundamentalStr, "G7 TEST STR A1",
            Ntc2018GammaColumnG1.A, null, actions, true,
            "G7 TEST exhaustive combination action inventory",
            Ntc2018TimberCoefficientSelectorG1.Fingerprint(AnalysisId, CombinationId, actions),
            "G7 TEST same FEM action manifest", "G7 TEST independently assigned duration",
            "G7 TEST C24 from catalog", true, "G7 TEST Circolare kcr from fv,k");
        return Selector.Select(input);
    }

    private static Ntc2018TimberSectionEvidenceG2 Section(Ntc2018TimberSelectedCoefficientsG1 selection) =>
        new(selection, "G7 TEST: intact rectangular timber section C24",
            true, true, true, true, true, 20000,
            "G7 TEST: independently determined net section=20000 mm2",
            true, "G7 TEST: in-plane shear direction verified");

    private static Ntc2018TimberCombinedEvidenceG3 Combined(Ntc2018TimberSectionEvidenceG2 section) =>
        new(section, true, "G7 TEST: signed concomitant FEM N/V/M",
            true, "G7 TEST: no weak-axis moment", true,
            "G7 TEST: linear elastic local stress model", 2,
            DesignEffectSectionSide.Continuous, "G7 TEST: x=2 m continuous side");

    private static DesignCheckRequest Request(Ntc2018TimberSelectedCoefficientsG1 selected,
        string rule, double n, double v, double m) =>
        new(new DesignMemberContext(MemberId, DesignMaterial.Timber, SectionId, 4),
            DesignLimitState.Ultimate, selected.Profile,
            new DesignEffectSnapshot(AnalysisId, CombinationId,
                DesignEffectOrigin.FemAnalysis, 2, n, v, m)
            { MemberId=MemberId, TargetKind=DesignEffectTargetKind.LoadCombination,
                SectionSide=DesignEffectSectionSide.Continuous },
            new[] {rule});

    private static NtcTimberCoveragePlanG6 Plan(Ntc2018TimberSelectedCoefficientsG1 selection,
        NtcTimberCoverageAreaG6 area, string ruleId, double x=2) =>
        new("G7-SYNTHETIC-PLAN",new DesignMemberContext(MemberId,DesignMaterial.Timber,SectionId,4),
            Ntc2018TimberCoefficientSelectorG1.Edition,
            new[] {new NtcTimberCoverageObligationG6("G7-LOCAL-ONLY",area,ruleId,
                DesignLimitState.Ultimate, selection.Profile.ProfileId,
                AnalysisId,CombinationId,x,DesignEffectSectionSide.Continuous)},
            Enum.GetValues<NtcTimberCoverageAreaG6>().Where(a=>a!=area)
                .Select(a=>new NtcTimberCoverageExclusionG6(a,
                    NtcTimberCoverageExclusionKindG6.UnsupportedOrNotEvaluated,
                    "G7 TEST: deliberately NOT certified and not executed in this fixture",
                    "G7 TEST: explicit gap — not a physical exclusion")).ToArray(),
            true,"G7 TEST: synthetic combination inventory",true,
            "G7 TEST: only synthetic station x=2m",true,
            "G7 TEST: physical assumptions NOT independently certified");

    [Fact]
    public void G1_G3_G6_RealConcomitantTensionMomentShear_PartialPositiveRemainsIncomplete()
    {
        var selected=Selected();
        Assert.Equal(0.8, selected.Kmod, 12);
        Assert.Equal(1.5, selected.GammaM, 12);
        var evidence=Combined(Section(selected));
        var rule=new Ntc2018TimberCombinedSectionRuleG3(
            Ntc2018TimberCombinedEffectG3.TensionBending,Materials,Tables,new[]{evidence});
        var request=Request(selected,rule.RuleId,20,10,3);
        var report=new DesignCheckEngine(new[]{rule}).Evaluate(request);
        var result=Assert.Single(report.Results);
        var sigmaT=20_000.0/(100*200); // 1 MPa
        var sigmaM=3_000_000.0/(100*200*200/6.0); // 4.5 MPa
        var sigmaV=1.5*10_000.0/(100*200*0.5); // kcr=2/4=.5, 1.5 MPa
        var ftd=14.5*.8/1.5;
        var fmd=24*.8/1.5;
        var fvd=4*.8/1.5;
        var interactionA=sigmaT/ftd+sigmaM/fmd;
        var interactionB=sigmaT/ftd+.7*sigmaM/fmd;
        var expected=Math.Max(Math.Max(interactionA,interactionB),sigmaV/fvd);
        Assert.Equal(.703125,expected,12);
        Assert.Equal(DesignCheckStatus.Passed,result.Status);
        Assert.Equal(expected,Assert.IsType<double>(result.Utilization),10);
        Assert.Equal(DesignOverallStatus.Passed,report.OverallStatus);
        var audit=NtcTimberCoverageAuditorG6.Audit(Plan(selected,
            NtcTimberCoverageAreaG6.TensionBending,rule.RuleId),new[]{report});
        Assert.Equal(1,audit.PassedCount);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,audit.Outcome);
        Assert.True(audit.UnsupportedExclusions>0);
        Assert.Contains("NOT A MEMBER",NtcTimberCoverageMarkdownG6.Render(audit),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void G1_G2_G6_FailedPureBendingIsRetainedEvenWithCoverageGaps()
    {
        var selected=Selected();
        var rule=new Ntc2018TimberRectangularSectionRuleG2(
            Ntc2018TimberPureEffectG2.StrongAxisBending,Materials,Tables,
            new[]{Section(selected)});
        var report=new DesignCheckEngine(new[]{rule}).Evaluate(Request(selected,rule.RuleId,0,0,20));
        var result=Assert.Single(report.Results);
        Assert.Equal(DesignCheckStatus.Failed,result.Status);
        Assert.Equal(30.0/12.8,Assert.IsType<double>(result.Utilization),10);
        var audit=NtcTimberCoverageAuditorG6.Audit(Plan(selected,
            NtcTimberCoverageAreaG6.StrongAxisBending,rule.RuleId),new[]{report});
        Assert.Equal(1,audit.FailedCount);
        Assert.Equal(NtcTimberCoverageOutcomeG6.FailedWithinDeclaredScope,audit.Outcome);
        Assert.True(audit.UnsupportedExclusions>0);
    }

    [Fact]
    public void RealRuleResultCannotBeReassignedToNearbyCriticalStation()
    {
        var selected=Selected();
        var rule=new Ntc2018TimberCombinedSectionRuleG3(
            Ntc2018TimberCombinedEffectG3.TensionBending,Materials,Tables,
            new[]{Combined(Section(selected))});
        var report=new DesignCheckEngine(new[]{rule}).Evaluate(Request(selected,rule.RuleId,20,10,3));
        var audit=NtcTimberCoverageAuditorG6.Audit(Plan(selected,
            NtcTimberCoverageAreaG6.TensionBending,rule.RuleId,2.000001),new[]{report});
        Assert.Equal(0,audit.PassedCount);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,audit.Outcome);
        Assert.Equal(NtcTimberCoverageFindingStatusG6.MissingEvidence,Assert.Single(audit.Findings).Status);
    }
}
