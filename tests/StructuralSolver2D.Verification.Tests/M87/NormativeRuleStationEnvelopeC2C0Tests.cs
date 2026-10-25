using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Integration;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Tests.M87;

public sealed class NormativeRuleStationEnvelopeC2C0Tests
{
    private static readonly DesignCodeEdition Code = new("TEST-NORM", "2026", "TEST-ANNEX");
    private static readonly DesignMemberContext Member = new("M", DesignMaterial.Steel, "SEC", 4);
    private static readonly NormativeProfile Profile = new("P", Code,
        new[] { new NormativeCoefficient("test-factor", 1, "TEST CLAUSE", "Fixture only") });

    [Fact]
    public void TwoPassedRealEngineReports_KeepObservedMaximumButNeverCertifyIntervalOrMember()
    {
        var result = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(
            Report(1, 3), Report(3, 8)));

        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.ScreeningIncomplete, result.Status);
        Assert.Equal(.8, result.HighestObservedUtilization!.Utilization, 12);
        Assert.Equal(3, result.HighestObservedUtilization.DistanceMetres);
        Assert.Contains(result.Issues, i => i.Code == "C2C0_CONTINUOUS_MAXIMUM_UNPROVEN");
        Assert.False(result.ContinuousNormativeMaximumEstablished);
        Assert.False(result.NormativeRuleIntervalCoverageEstablished);
        Assert.False(result.MemberAdequacyEstablished);
        Assert.False(result.CompleteActionSetEstablished);
        Assert.False(result.WholeStructureVerified);
        Assert.False(result.CapturedReportsIndependentlyAuthenticated);
    }

    [Fact]
    public void OneFailedReport_NeverDisappearsBecauseAnotherStationIsMissing()
    {
        var reportMissing = Report(2, null);
        Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(reportMissing.Results).Status);

        var result = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(
            Report(1, 12), reportMissing, Report(3, 4)));

        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.ObservedRuleFailure, result.Status);
        Assert.Equal(1.2, result.HighestObservedUtilization!.Utilization, 12);
        Assert.Contains(result.Issues, i => i.Code == "C2C0_RULE_NOT_CALCULATED");
        Assert.Contains(result.Issues, i => i.Code == "C2C0_OBSERVED_RULE_FAILURE");
        Assert.False(result.MemberAdequacyEstablished);
    }

    [Fact]
    public void NoStationsOrUnsupportedStation_StayIncomplete()
    {
        var empty = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan());
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.ScreeningIncomplete, empty.Status);
        Assert.Null(empty.HighestObservedUtilization);

        DesignCheckRequest request = Request(1, 4);
        var unsupported = new DesignCheckEngine(Array.Empty<IDesignCheckRule>()).Evaluate(request);
        var result = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(unsupported));
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.ScreeningIncomplete, result.Status);
        Assert.Empty(result.Observations);
        Assert.Contains(result.Issues, i => i.Code == "C2C0_RULE_NOT_CALCULATED");
    }

    [Fact]
    public void SamePhysicalStationWithTwoSides_IsAllowed_ExactDuplicateIsRejected()
    {
        var before = Report(2, 6, DesignEffectSectionSide.BeforePointLoad);
        var after = Report(2, 9, DesignEffectSectionSide.AfterPointLoad);
        var valid = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(before, after));
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.ScreeningIncomplete, valid.Status);
        Assert.Equal(2, valid.Observations.Count);

        var duplicate = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(before, before));
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput, duplicate.Status);
        Assert.Contains(duplicate.Issues, i => i.Code == "C2C0_DUPLICATE_STATION");
        Assert.Null(duplicate.HighestObservedUtilization);
    }

    [Fact]
    public void ForgedPassedWithUtilizationAboveOne_IsRejected()
    {
        DesignCheckReport original = Report(1, 12);
        var changed = new DesignCheckReport(original.Request,
            new[] { Assert.Single(original.Results) with { Status = DesignCheckStatus.Passed } });
        var result = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(changed));
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput, result.Status);
        Assert.Contains(result.Issues, i => i.Code == "C2C0_RULE_RESULT_INCONSISTENT");
    }

    [Fact]
    public void ForgedNumericalUtilization_InconsistentWithDemandResistance_IsRejected()
    {
        DesignCheckReport original = Report(1, 3);
        var changed = new DesignCheckReport(original.Request,
            new[] { Assert.Single(original.Results) with { Utilization = .9 } });
        var result = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(changed));
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput, result.Status);
        Assert.Empty(result.Observations);
    }

    [Fact]
    public void MutatedFemTargetOrMemberIdentity_IsRejected()
    {
        DesignCheckReport original = Report(1, 3);
        var targetForgery = new DesignCheckReport(original.Request with
        {
            Effects = original.Request.Effects with { CombinationId = "OTHER" }
        }, original.Results);
        var memberForgery = new DesignCheckReport(original.Request with
        {
            Effects = original.Request.Effects with { MemberId = "OTHER" }
        }, original.Results);

        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput,
            NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(targetForgery)).Status);
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput,
            NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(memberForgery)).Status);
    }

    [Fact]
    public void ChangedEditionOrProfileWithinSameTarget_IsRejected()
    {
        DesignCheckReport first = Report(1, 3);
        DesignCheckReport second = Report(3, 5);
        var editionForgery = new DesignCheckReport(second.Request with
        {
            Profile = second.Request.Profile with
            {
                Code = new DesignCodeEdition("TEST-NORM", "DIFFERENT", "TEST-ANNEX")
            }
        }, second.Results);
        var coefficientForgery = new DesignCheckReport(second.Request with
        {
            Profile = second.Request.Profile with
            {
                Coefficients = new[] { new NormativeCoefficient("test-factor", 1.5, "TEST CLAUSE", "Fixture only") }
            }
        }, second.Results);

        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput,
            NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(first, editionForgery)).Status);
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput,
            NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(first, coefficientForgery)).Status);
    }

    [Fact]
    public void ManualEffectsAreNotMisrepresentedAsFemEvidence()
    {
        DesignCheckReport original = Report(1, 3);
        var manual = new DesignCheckReport(original.Request with
        {
            Effects = original.Request.Effects with { Origin = DesignEffectOrigin.ExternalManual }
        }, original.Results);

        var result = NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(manual));
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput, result.Status);
        Assert.Null(result.HighestObservedUtilization);
    }

    [Fact]
    public void SameRuleIdWithDifferentClausesOrUnits_CannotBeJoined()
    {
        DesignCheckReport first = Report(1, 3);
        DesignCheckReport second = Report(3, 5);
        var falseClause = new DesignCheckReport(second.Request,
            new[] { Assert.Single(second.Results) with { SourceClause = "ANOTHER FORMULA" } });
        var falseUnit = new DesignCheckReport(second.Request,
            new[] { Assert.Single(second.Results) with { Unit = "kN" } });

        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput,
            NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(first, falseClause)).Status);
        Assert.Equal(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput,
            NormativeRuleStationEnvelopeAuditorC2C0.Audit(Plan(first, falseUnit)).Status);
    }

    private static NormativeRuleStationPlanC2C0 Plan(params DesignCheckReport[] reports) =>
        new("PLAN", Member, Code, DesignLimitState.Ultimate, SyntheticRule.Rule,
            DesignEffectTargetKind.LoadCombination, "COMB", reports);

    private static DesignCheckRequest Request(double x, double? moment,
        DesignEffectSectionSide side = DesignEffectSectionSide.Continuous) =>
        new(Member, DesignLimitState.Ultimate, Profile,
            new DesignEffectSnapshot("AN", "COMB", DesignEffectOrigin.FemAnalysis,
                x, 0, 0, moment)
            {
                MemberId = Member.MemberId,
                TargetKind = DesignEffectTargetKind.LoadCombination,
                SectionSide = side
            }, new[] { SyntheticRule.Rule });

    private static DesignCheckReport Report(double x, double? moment,
        DesignEffectSectionSide side = DesignEffectSectionSide.Continuous) =>
        new DesignCheckEngine(new IDesignCheckRule[] { new SyntheticRule() })
            .Evaluate(Request(x, moment, side));

    // Only a fixture for testing the REAL DesignCheckEngine report contract;
    // it is not an EC3/EC5/NTC design rule and supplies no continuous proof.
    private sealed class SyntheticRule : IDesignCheckRule
    {
        public const string Rule = "TEST-LOCAL-BENDING";
        public string RuleId => Rule;
        public DesignCodeEdition Code => NormativeRuleStationEnvelopeC2C0Tests.Code;
        public DesignMaterial Material => DesignMaterial.Steel;
        public DesignLimitState LimitState => DesignLimitState.Ultimate;
        public string SourceClause => "TEST CLAUSE ONLY";
        public DesignRuleEvaluation Evaluate(DesignCheckRequest request) =>
            request.Effects.BendingMomentZKnm is double m
                ? DesignRuleEvaluation.Calculated(Math.Abs(m), 10, "kNm", "TEST TRACE ONLY")
                : DesignRuleEvaluation.MissingData("Test station has no moment.");
    }
}
