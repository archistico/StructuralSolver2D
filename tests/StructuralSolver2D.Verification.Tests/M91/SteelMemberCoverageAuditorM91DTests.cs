using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Steel;

namespace StructuralSolver2D.Verification.Tests.M91;

public sealed class SteelMemberCoverageAuditorM91DTests
{
    private static readonly DesignCodeEdition Code =
        new("EN 1993-1-1", "2005+A1:2014", "IT-TEST-ANNEX-EXPLICIT");
    private static readonly DesignMemberContext Member =
        new("B1", DesignMaterial.Steel, "HEA-280", 4);

    private static SteelCoverageObligationM91D Obligation(
        string id = "BENDING-ULS1-X2", string analysis = "FEM-1", string target = "ULS1",
        double x = 2, DesignEffectSectionSide side = DesignEffectSectionSide.Continuous) =>
        new(id, SteelCheckAreaM91D.StrongAxisBendingResistance, ElasticBendingRuleM89A.Id,
            analysis, target, DesignEffectTargetKind.LoadCombination, x, side);

    private static SteelMemberCoveragePlanM91D Plan(
        IReadOnlyList<SteelCoverageObligationM91D>? checks = null,
        bool withExclusions = true) =>
        new("M91D-TEST-PLAN", Member, Code, DesignLimitState.Ultimate,
            checks ?? new[] {Obligation()},
            withExclusions ? Enum.GetValues<SteelCheckAreaM91D>()
                .Where(x => x != SteelCheckAreaM91D.StrongAxisBendingResistance)
                .Select(x => new SteelCoverageExclusionM91D(x,
                    "EXPLICITLY OUT OF THIS TEST SCOPE; not a declaration of structural non-applicability",
                    "Independent reviewer reference: hypothetical test-only scheduling note"))
                .ToArray() : Array.Empty<SteelCoverageExclusionM91D>(),
            "Engineer has listed load combinations: TEST ONLY, not checked by auditor",
            "Engineer has listed critical stations: TEST ONLY, not checked by auditor", true);

    private static DesignCheckReport Evidence(
        DesignCheckStatus status = DesignCheckStatus.Passed,
        string member = "B1", string section = "HEA-280", string analysis = "FEM-1",
        string target = "ULS1", double x = 2,
        DesignEffectSectionSide side = DesignEffectSectionSide.Continuous,
        DesignCodeEdition? code = null,
        DesignEffectTargetKind kind = DesignEffectTargetKind.LoadCombination,
        DesignEffectOrigin origin = DesignEffectOrigin.FemAnalysis,
        double demand = 20, double resistance = 40, double? utilization = null,
        string ruleId = ElasticBendingRuleM89A.Id)
    {
        var request = new DesignCheckRequest(
            Member with {MemberId=member, SectionId=section}, DesignLimitState.Ultimate,
            new NormativeProfile("DEMO-NOT-CERTIFICATION", code ?? Code,
                new[] {new NormativeCoefficient("gamma_M0",1,"TEST", "Test")}),
            new DesignEffectSnapshot(analysis,target,origin,x,0,0,20)
            {
                MemberId=member, TargetKind=kind, SectionSide=side
            }, new[] {ruleId});
        var result = status is DesignCheckStatus.Passed or DesignCheckStatus.Failed
            ? new DesignCheckResult(ruleId, status, "TEST ONLY", "Test clause", demand,
                resistance, utilization ?? demand / resistance, "kNm", "test-only audit trace")
            : new DesignCheckResult(ruleId,status,"Missing or unsupported TEST input", "Test clause");
        return new DesignCheckReport(request, new[] {result});
    }

    private static SteelCoverageAuditReportM91D Audit(SteelMemberCoveragePlanM91D? plan = null,
        params DesignCheckReport[] reports) =>
        SteelMemberCoverageAuditorM91D.Audit(plan ?? Plan(), reports);

    [Fact]
    public void OnePassingRuleWithDeclaredExclusionsIsNeverMemberCompliance()
    {
        var report=Audit(null, Evidence());
        Assert.Equal(SteelCoverageOutcomeM91D.ChecksPassedWithExplicitExclusions, report.Outcome);
        Assert.Equal(1, report.PassedCount);
        Assert.Empty(report.UndeclaredAreas);
        var markdown=SteelCoverageMarkdownM91D.Render(report);
        Assert.Contains("NON costituisce una dichiarazione di conformità", markdown);
        Assert.Contains("Connections", markdown);
        Assert.Contains("IT-TEST-ANNEX-EXPLICIT", markdown);
        Assert.Contains("NOT an EC3/NTC", markdown);
    }

    [Fact]
    public void MissingRequiredRuleMustStayIncomplete()
    {
        var result=Audit();
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete, result.Outcome);
        Assert.Equal(SteelCoverageFindingStatusM91D.MissingEvidence, Assert.Single(result.Findings).Status);
    }

    [Fact]
    public void FailingRuleOverrulesMissingEvidence()
    {
        var checks=new[] {Obligation(), Obligation("BENDING-ULS2-X2",target:"ULS2")};
        var result=Audit(Plan(checks), Evidence(DesignCheckStatus.Failed,demand:48,resistance:40));
        Assert.Equal(SteelCoverageOutcomeM91D.FailedWithinDeclaredScope,result.Outcome);
        Assert.Equal(1,result.FailedCount);
        Assert.Equal(1,result.IncompleteCount);
    }

    [Theory]
    [InlineData(DesignCheckStatus.Unsupported)]
    [InlineData(DesignCheckStatus.MissingData)]
    [InlineData(DesignCheckStatus.NotApplicable)]
    [InlineData(DesignCheckStatus.CalculationError)]
    [InlineData(DesignCheckStatus.InvalidInput)]
    public void NonCalculatedRuleStatusesNeverPass(DesignCheckStatus status)
    {
        var report=Audit(null,Evidence(status));
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,report.Outcome);
        Assert.Equal(SteelCoverageFindingStatusM91D.NotEvaluated,Assert.Single(report.Findings).Status);
    }

    [Fact]
    public void DifferentAnalysisOrCombinationNeverSatisfiesObligation()
    {
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(null,Evidence(analysis:"FEM-OTHER")).Outcome);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(null,Evidence(target:"ULS-OTHER")).Outcome);
    }

    [Fact]
    public void SameStationButOppositePointLoadSideCannotBeMixed()
    {
        var side=Plan(new[] {Obligation(side:DesignEffectSectionSide.BeforePointLoad)});
        var result=Audit(side,Evidence(side:DesignEffectSectionSide.AfterPointLoad));
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,result.Outcome);
        Assert.Equal(1,result.UnexpectedResults);
    }

    [Fact]
    public void StationMatchingIsExact_NoRoundingOrNearbyStationSubstitutions()
    {
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(null,Evidence(x:2.0000000001)).Outcome);
    }

    [Fact]
    public void WrongSectionMemberEditionTargetKindOrOriginNeverMatches()
    {
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(null,Evidence(section:"IPE-200")).Outcome);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(null,Evidence(member:"B2")).Outcome);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(null,Evidence(code:Code with {EditionId="2022"})).Outcome);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(null,Evidence(kind:DesignEffectTargetKind.LoadCase)).Outcome);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(null,Evidence(origin:DesignEffectOrigin.ExternalManual)).Outcome);
    }

    [Fact]
    public void DuplicateResultReportsAreAmbiguousNotTwoConfirmations()
    {
        var evidence=Evidence();
        var report=Audit(null,evidence,evidence);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,report.Outcome);
        Assert.Equal(SteelCoverageFindingStatusM91D.InvalidOrConflictingEvidence,
            Assert.Single(report.Findings).Status);
    }

    [Fact]
    public void ForgedPassedOutputWithInconsistentNumbersIsRejected()
    {
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(null,Evidence(utilization:0.1)).Outcome);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(null,Evidence(demand:80,resistance:40,utilization:2)).Outcome);
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(null,Evidence(resistance:double.PositiveInfinity)).Outcome);
    }

    [Fact]
    public void UnscheduledFailedResultIsNeverSilentlyDiscarded()
    {
        var unexpected=Evidence(DesignCheckStatus.Failed,target:"ULS2",demand:45,resistance:40);
        var report=Audit(null,Evidence(), unexpected);
        Assert.Equal(SteelCoverageOutcomeM91D.FailedWithinDeclaredScope,report.Outcome);
        Assert.Equal(1,report.UnexpectedResults);
    }

    [Fact]
    public void UnscheduledPassedResultAlsoPreventsCoverageCompletion()
    {
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(null,Evidence(),Evidence(target:"ULS2")).Outcome);
    }

    [Fact]
    public void NoEvidenceOfExternalReviewPreventsAnyPositiveOutcome()
    {
        var plan=Plan() with {CompletenessExternallyReviewed=null};
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(plan,Evidence()).Outcome);
    }

    [Fact]
    public void OmittedCategoriesAreNotAssumedIrrelevant()
    {
        var report=Audit(Plan(withExclusions:false),Evidence());
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,report.Outcome);
        Assert.Contains(SteelCheckAreaM91D.Connections,report.UndeclaredAreas);
    }

    [Fact]
    public void MissingExclusionJustificationInvalidatesSchedule()
    {
        var p=Plan();
        var e=p.Exclusions.ToArray();
        e[0]=e[0] with {ExternalJustification=" "};
        var report=Audit(p with {Exclusions=e},Evidence());
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,report.Outcome);
    }

    [Fact]
    public void DuplicateRequirementsOrContradictoryExclusionsAreRejected()
    {
        var o=Obligation();
        var duplicate=Plan(new[] {o, o with {ObligationId="SAME-TARGET-OTHER-ID"}});
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(duplicate,Evidence()).Outcome);
        var basePlan=Plan();
        var contradictory=basePlan with {Exclusions=basePlan.Exclusions.Concat(new[] {
            new SteelCoverageExclusionM91D(SteelCheckAreaM91D.StrongAxisBendingResistance,
                "Contradictory", "external")}).ToArray()};
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,Audit(contradictory,Evidence()).Outcome);
    }

    [Fact]
    public void UnknownRuleCannotClaimAValidatedCheckArea()
    {
        var forged=Plan(new[] {Obligation() with {RuleId="CUSTOM-NOT-REGISTERED"}});
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(forged,Evidence(ruleId:"CUSTOM-NOT-REGISTERED")).Outcome);
    }

    [Fact]
    public void CompletePlanRequiresEveryExplicitAnalysisTargetAndStation()
    {
        var plan=Plan(new[] {Obligation(),Obligation("ULS2",target:"ULS2")});
        var report=Audit(plan,Evidence(),Evidence(target:"ULS2"));
        Assert.Equal(SteelCoverageOutcomeM91D.ChecksPassedWithExplicitExclusions,report.Outcome);
        Assert.Equal(2,report.PassedCount);
    }

    [Fact]
    public void EngineGeneratedRealRuleResultIsAcceptedOnlyForItsStationAndScope()
    {
        var catalog=CommercialRolledSteelCatalogM88B2.Load(
            Path.Combine(AppContext.BaseDirectory,"steel-oppo-ipe-hea-heb-inp.json"));
        var materials=CharacteristicMaterialCatalogM88C.Load(
            Path.Combine(AppContext.BaseDirectory,"characteristic-strength-classes.json"));
        var rule=new ElasticBendingRuleM89A(Code,catalog,materials,new[] {
            new SteelElasticBendingAssignmentM89A("B1","HEA-280","S355",3,
                "TEST: independently reviewed class-3 assignment",true,true)
        });
        var req=Evidence().Request;
        var engineReport=new DesignCheckEngine(new[] {rule}).Evaluate(req);
        Assert.Equal(DesignCheckStatus.Passed,Assert.Single(engineReport.Results).Status);
        var audit=Audit(null,engineReport);
        Assert.Equal(SteelCoverageOutcomeM91D.ChecksPassedWithExplicitExclusions,audit.Outcome);
        Assert.Contains("Rule result within",Assert.Single(audit.Findings).Explanation);
    }

    [Fact]
    public void EmptyScheduleCannotPassEvenIfThereAreExclusions()
    {
        var plan=Plan(Array.Empty<SteelCoverageObligationM91D>());
        Assert.Equal(SteelCoverageOutcomeM91D.Incomplete,
            Audit(plan).Outcome);
    }
}
