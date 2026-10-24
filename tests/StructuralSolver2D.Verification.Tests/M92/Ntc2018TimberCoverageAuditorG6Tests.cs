using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Timber;

namespace StructuralSolver2D.Verification.Tests.M92;

public sealed class Ntc2018TimberCoverageAuditorG6Tests
{
    private static readonly DesignCodeEdition Code = Ntc2018TimberCoefficientSelectorG1.Edition;
    private static readonly DesignMemberContext Member = new("T1", DesignMaterial.Timber, "C24", 4);
    private const string Profile = "TEST-NTC-G1-COMBINATION-PROFILE";

    private static NtcTimberCoverageObligationG6 Obligation(
        string id="BENDING-1", NtcTimberCoverageAreaG6 area=NtcTimberCoverageAreaG6.StrongAxisBending,
        string? rule=null, string analysis="FEM-1", string combination="ULS-1",
        string profile=Profile, double x=2,
        DesignEffectSectionSide side=DesignEffectSectionSide.Continuous,
        DesignLimitState limit=DesignLimitState.Ultimate) =>
        new(id,area,rule??Ntc2018TimberRectangularSectionRuleG2.BendingId,
            limit,profile,analysis,combination,x,side);

    private static NtcTimberCoveragePlanG6 Plan(
        IReadOnlyList<NtcTimberCoverageObligationG6>? obligations=null,
        bool explicitExclusions=true)
    {
        var checks=obligations??new[] {Obligation()};
        var exclusions=explicitExclusions ? Enum.GetValues<NtcTimberCoverageAreaG6>()
            .Where(area=>!checks.Select(o=>o.Area).Contains(area))
            .Select(area=>new NtcTimberCoverageExclusionG6(area,
                NtcTimberCoverageExclusionKindG6.PhysicallyNotApplicable,
                "TEST-ONLY physical non-applicability declaration, NOT validated physical evidence",
                "Synthetic independent reference — deliberately not usable for practice"))
            .ToArray() : Array.Empty<NtcTimberCoverageExclusionG6>();
        return new("G6-TEST-SCHEDULE",Member,Code,checks,exclusions,
            true,"TEST: independently listed all combination IDs",
            true,"TEST: independently listed all governing stations",
            true,"TEST: acknowledged Euler-Bernoulli, bracing, real supports and joints");
    }

    private static DesignCheckReport Evidence(
        DesignCheckStatus status=DesignCheckStatus.Passed,
        string member="T1", string section="C24", string analysis="FEM-1",
        string combination="ULS-1", string profile=Profile, double x=2,
        DesignEffectSectionSide side=DesignEffectSectionSide.Continuous,
        string rule=Ntc2018TimberRectangularSectionRuleG2.BendingId,
        DesignLimitState limit=DesignLimitState.Ultimate,
        DesignCodeEdition? code=null,
        DesignEffectTargetKind kind=DesignEffectTargetKind.LoadCombination,
        DesignEffectOrigin origin=DesignEffectOrigin.FemAnalysis,
        double demand=5,double resistance=10,double? utilization=null)
    {
        var request=new DesignCheckRequest(
            Member with {MemberId=member,SectionId=section},limit,
            new NormativeProfile(profile,code??Code,
                new[] {new NormativeCoefficient("TEST",1.5,"NTC 2018", "NOT an actual selector")}),
            new DesignEffectSnapshot(analysis,combination,origin,x,0,0,3)
            {
                MemberId=member,TargetKind=kind,SectionSide=side
            },new[] {rule});
        var result=status is DesignCheckStatus.Passed or DesignCheckStatus.Failed
            ? new DesignCheckResult(rule,status,"TEST ONLY","NTC 2018 4.4.8.1.6",
                demand,resistance,utilization??demand/resistance,"ratio","TEST result: not independently authenticated")
            : new DesignCheckResult(rule,status,"No verified calculation","NTC 2018 4.4.8.1.6");
        return new DesignCheckReport(request,new[] {result});
    }

    private static NtcTimberCoverageAuditReportG6 Audit(
        NtcTimberCoveragePlanG6? plan=null,params DesignCheckReport[] reports) =>
        NtcTimberCoverageAuditorG6.Audit(plan??Plan(),reports);

    [Fact]
    public void PassingOneRuleWithPhysicalExclusionsIsOnlyDeclaredScope_NotMemberApproval()
    {
        var report=Audit(null,Evidence());
        Assert.Equal(NtcTimberCoverageOutcomeG6.ChecksPassedWithExplicitPhysicalExclusions,report.Outcome);
        Assert.Equal(1,report.PassedCount);
        Assert.Empty(report.UndeclaredAreas);
        var text=NtcTimberCoverageMarkdownG6.Render(report);
        Assert.Contains("NON costituisce una dichiarazione di conformità",text);
        Assert.Contains("EulerBernoulli",text);
        Assert.Contains("NOT A MEMBER",text);
    }

    [Fact]
    public void MissingRuleIsIncomplete()
    {
        var report=Audit();
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,report.Outcome);
        Assert.Equal(NtcTimberCoverageFindingStatusG6.MissingEvidence,Assert.Single(report.Findings).Status);
    }

    [Fact]
    public void FailingResultDominatesMissingChecksWithoutHidingThem()
    {
        var checks=new[] {Obligation(),Obligation(id:"B2",combination:"ULS-2")};
        var report=Audit(Plan(checks),Evidence(DesignCheckStatus.Failed,demand:11));
        Assert.Equal(NtcTimberCoverageOutcomeG6.FailedWithinDeclaredScope,report.Outcome);
        Assert.Equal(1,report.FailedCount);
        Assert.Equal(1,report.IncompleteCount);
    }

    [Theory]
    [InlineData(DesignCheckStatus.NotApplicable)]
    [InlineData(DesignCheckStatus.MissingData)]
    [InlineData(DesignCheckStatus.Unsupported)]
    [InlineData(DesignCheckStatus.InvalidInput)]
    [InlineData(DesignCheckStatus.CalculationError)]
    public void UncalculatedStatusNeverCountsAsPhysicalExclusion(DesignCheckStatus status)
    {
        var report=Audit(null,Evidence(status));
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,report.Outcome);
        Assert.Equal(NtcTimberCoverageFindingStatusG6.NotEvaluated,Assert.Single(report.Findings).Status);
    }

    [Fact]
    public void ExactAnalysisCombinationProfileStationSideIdentityIsMandatory()
    {
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(null,Evidence(analysis:"FEM-X")).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(null,Evidence(combination:"ULS-2")).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(null,Evidence(profile:"EC5-PROFILE")).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(null,Evidence(x:2.000000000001)).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(null,Evidence(side:DesignEffectSectionSide.BeforePointLoad)).Outcome);
    }

    [Fact]
    public void WrongMemberSectionEditionOriginOrTargetNeverMatches()
    {
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(null,Evidence(member:"T2")).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(null,Evidence(section:"GL24h")).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(null,Evidence(code:Code with {EditionId="2027"})).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(null,Evidence(origin:DesignEffectOrigin.ExternalManual)).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(null,Evidence(kind:DesignEffectTargetKind.LoadCase)).Outcome);
    }

    [Fact]
    public void DuplicateMatchingReportIsNotDoubleEvidence()
    {
        var original=Evidence();
        var report=Audit(null,original,original);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,report.Outcome);
        Assert.Equal(NtcTimberCoverageFindingStatusG6.InvalidOrConflictingEvidence,
            Assert.Single(report.Findings).Status);
    }

    [Fact]
    public void InvalidNumericOrMislabeledPassIsRejected()
    {
        foreach (var fake in new[]
        {
            Evidence(utilization:.2),
            Evidence(demand:20,resistance:10,utilization:2),
            Evidence(resistance:double.PositiveInfinity),
            Evidence(demand:double.NaN),
            Evidence(resistance:0),
            Evidence(DesignCheckStatus.Failed,demand:5,resistance:10)
        })
        {
            var report=Audit(null,fake);
            Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,report.Outcome);
            Assert.Equal(NtcTimberCoverageFindingStatusG6.InvalidOrConflictingEvidence,
                Assert.Single(report.Findings).Status);
        }
    }

    [Fact]
    public void UnscheduledPassingResultIsNeverSilentlyDiscarded()
    {
        var result=Audit(null,Evidence(),Evidence(combination:"ULS-OTHER"));
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,result.Outcome);
        Assert.Equal(1,result.UnmatchedResults);
    }

    [Fact]
    public void UnscheduledValidFailedRuleOnSameMemberStillWarnsOfFailure()
    {
        var result=Audit(null,Evidence(),Evidence(DesignCheckStatus.Failed,
            combination:"ULS-OTHER",demand:20));
        Assert.Equal(NtcTimberCoverageOutcomeG6.FailedWithinDeclaredScope,result.Outcome);
        Assert.Equal(1,result.UnmatchedVerifiedFailures);
    }

    [Fact]
    public void UnscheduledFailureFromDifferentMemberOrEditionCannotGovern()
    {
        var result=Audit(null,Evidence(),Evidence(DesignCheckStatus.Failed,
            member:"OTHER",combination:"ULS-OTHER",demand:20));
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,result.Outcome);
        Assert.Equal(0,result.UnmatchedVerifiedFailures);
    }

    [Fact]
    public void FailedRuleFromDifferentAnalysisCannotBeMisattributedToThisPlan()
    {
        var result=Audit(null,Evidence(),Evidence(DesignCheckStatus.Failed,
            analysis:"DIFFERENT-FEM-RUN",combination:"ULS-OTHER",demand:20));
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,result.Outcome);
        Assert.Equal(0,result.UnmatchedVerifiedFailures);
    }

    [Fact]
    public void UnsupportedExclusionDoesNotBecomePhysicalNonApplicability()
    {
        var p=Plan();
        var excluded=p.Exclusions.ToArray();
        excluded[0]=excluded[0] with {Kind=NtcTimberCoverageExclusionKindG6.UnsupportedOrNotEvaluated};
        var result=Audit(p with {Exclusions=excluded},Evidence());
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,result.Outcome);
        Assert.Equal(1,result.UnsupportedExclusions);
    }

    [Fact]
    public void NoExternalCompletenessReviewIsNeverPassing()
    {
        var p=Plan() with {CombinationInventoryExternallyReviewed=null};
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(p,Evidence()).Outcome);
        p=Plan() with {CriticalStationsEvidence=""};
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(p,Evidence()).Outcome);
        p=Plan() with {PhysicalModelLimitationsExternallyReviewed=false};
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(p,Evidence()).Outcome);
    }

    [Fact]
    public void OmittedAreasDoNotBecomeNotApplicable()
    {
        var report=Audit(Plan(explicitExclusions:false),Evidence());
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,report.Outcome);
        Assert.Contains(NtcTimberCoverageAreaG6.ConnectionsAndEccentricities,report.UndeclaredAreas);
        Assert.Contains(NtcTimberCoverageAreaG6.UnilateralBracingAndTensionOnlyMembers,report.UndeclaredAreas);
    }

    [Fact]
    public void DuplicateContradictoryAndEmptyExclusionsCannotPass()
    {
        var p=Plan();
        var e=p.Exclusions.ToArray();
        e[0]=e[0] with {ExternalEvidenceReference=""};
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(p with {Exclusions=e},Evidence()).Outcome);
        p=Plan();
        var appended=p.Exclusions.Append(p.Exclusions[0]).ToArray();
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(p with {Exclusions=appended},Evidence()).Outcome);
        appended=p.Exclusions.Append(new NtcTimberCoverageExclusionG6(
            NtcTimberCoverageAreaG6.StrongAxisBending,
            NtcTimberCoverageExclusionKindG6.PhysicallyNotApplicable,
            "Contradicts scheduled bending","Reviewer X")).ToArray();
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(p with {Exclusions=appended},Evidence()).Outcome);
    }

    [Fact]
    public void DuplicateObligationOrWrongRuleCategoryMappingIsBlocked()
    {
        var o=Obligation();
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(Plan(new[] {o,o with {ObligationId="other"}}),Evidence()).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(Plan(new[] {o,o with {CombinationId="ULS-2"}}),Evidence()).Outcome);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(Plan(new[] {o with {RuleId=Ntc2018TimberRectangularSectionRuleG2.ShearId}}),Evidence()).Outcome);
    }

    [Fact]
    public void RuleForUnsupportedPhysicalPhenomenonCannotCreatePassingAudit()
    {
        var o=Obligation(area:NtcTimberCoverageAreaG6.ConnectionsAndEccentricities);
        var report=Audit(Plan(new[] {o}),Evidence());
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,report.Outcome);
    }

    [Fact]
    public void ServiceabilityAndUltimateCanCoexistButNeverSubstituteEachOther()
    {
        var bending=Obligation();
        var sle=Obligation("SLE-FINAL",NtcTimberCoverageAreaG6.SleFinalDeflection,
            Ntc2018TimberSleDeflectionRuleG5.FinalId,
            combination:"SLE-RARE",profile:"G5-SLE-PROFILE",limit:DesignLimitState.Serviceability);
        var plan=Plan(new[] {bending,sle});
        var onlyUls=Audit(plan,Evidence());
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,onlyUls.Outcome);
        var report=Audit(plan,Evidence(),Evidence(rule:Ntc2018TimberSleDeflectionRuleG5.FinalId,
            combination:"SLE-RARE",profile:"G5-SLE-PROFILE",limit:DesignLimitState.Serviceability));
        Assert.Equal(NtcTimberCoverageOutcomeG6.ChecksPassedWithExplicitPhysicalExclusions,report.Outcome);
        Assert.Equal(2,report.PassedCount);
    }

    [Fact]
    public void AdjacentPointLoadSectionsCannotShareShearEvidence()
    {
        var o=Obligation(side:DesignEffectSectionSide.BeforePointLoad);
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,
            Audit(Plan(new[] {o}),Evidence(side:DesignEffectSectionSide.AfterPointLoad)).Outcome);
    }

    [Fact]
    public void AllKnownNativeNTCRuleIdsAreMappedExactlyToApplicableLimitState()
    {
        var specs=new[]
        {
            (NtcTimberCoverageAreaG6.TensionParallel,Ntc2018TimberRectangularSectionRuleG2.TensionId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.CompressionParallel,Ntc2018TimberRectangularSectionRuleG2.CompressionId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.StrongAxisBending,Ntc2018TimberRectangularSectionRuleG2.BendingId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.InPlaneShear,Ntc2018TimberRectangularSectionRuleG2.ShearId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.TensionBending,Ntc2018TimberCombinedSectionRuleG3.TensionBendingId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.CompressionBending,Ntc2018TimberCombinedSectionRuleG3.CompressionBendingId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.BendingAndShear,Ntc2018TimberCombinedSectionRuleG3.BendingShearId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.ColumnFlexuralBuckling,Ntc2018TimberStabilityRuleG4.ColumnId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.BeamLateralTorsionalBuckling,Ntc2018TimberStabilityRuleG4.BeamId,DesignLimitState.Ultimate),
            (NtcTimberCoverageAreaG6.SleRareVariableDeflection,Ntc2018TimberSleDeflectionRuleG5.VariableId,DesignLimitState.Serviceability),
            (NtcTimberCoverageAreaG6.SleRareTotalDeflection,Ntc2018TimberSleDeflectionRuleG5.RareTotalId,DesignLimitState.Serviceability),
            (NtcTimberCoverageAreaG6.SleFinalDeflection,Ntc2018TimberSleDeflectionRuleG5.FinalId,DesignLimitState.Serviceability)
        };
        foreach (var (area,rule,limit) in specs)
        {
            var o=Obligation(id:"CHECK",area:area,rule:rule,limit:limit);
            var plan=Plan(new[] {o});
            var report=Audit(plan,Evidence(rule:rule,limit:limit));
            Assert.Equal(NtcTimberCoverageOutcomeG6.ChecksPassedWithExplicitPhysicalExclusions,report.Outcome);
            Assert.Equal(1,report.PassedCount);
        }
    }

    [Fact]
    public void NeverConvertsAnEc5EditionIntoAnNTCCheck()
    {
        var p=Plan() with {Code=new DesignCodeEdition("EN 1995-1-1","2004+A1:2008+A2:2014","IT-TEST")};
        Assert.Equal(NtcTimberCoverageOutcomeG6.Incomplete,Audit(p,Evidence()).Outcome);
    }
}
