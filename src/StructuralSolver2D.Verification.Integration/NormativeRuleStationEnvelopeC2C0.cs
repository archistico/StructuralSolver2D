using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Integration;

/// <summary>
/// C2C0 is a provenance/consistency gate over ACTUAL single-station normative
/// rule reports. It does not evaluate or maximize a design rule between stations.
/// No caller-declared proof of completeness can turn its result into Passed.
/// </summary>
public sealed record NormativeRuleStationPlanC2C0(
    string PlanId,
    DesignMemberContext Member,
    DesignCodeEdition Code,
    DesignLimitState LimitState,
    string RuleId,
    DesignEffectTargetKind TargetKind,
    string TargetId,
    IReadOnlyList<DesignCheckReport> StationReports);

public enum NormativeRuleStationEnvelopeStatusC2C0
{
    InvalidInput,
    ObservedRuleFailure,
    ScreeningIncomplete
}

public sealed record NormativeRuleStationObservationC2C0(
    string RuleId,
    string TargetId,
    double DistanceMetres,
    DesignEffectSectionSide Side,
    string ProfileId,
    double Utilization,
    DesignCheckStatus RuleStatus,
    string SourceClause);

public sealed record NormativeRuleStationIssueC2C0(string Code, string Message);

/// <summary>
/// ObservedRuleFailure reflects a recorded failure of the requested rule at a
/// declared station only. An observed maximum is NOT a continuous maximum;
/// audit results are not an authenticated reanalysis of the original FEM.
/// </summary>
public sealed class NormativeRuleStationEnvelopeResultC2C0
{
    public NormativeRuleStationEnvelopeResultC2C0(
        NormativeRuleStationEnvelopeStatusC2C0 status,
        IEnumerable<NormativeRuleStationIssueC2C0> issues,
        IEnumerable<NormativeRuleStationObservationC2C0> observations)
    {
        Status = status;
        Issues = Array.AsReadOnly(issues.ToArray());
        Observations = Array.AsReadOnly(observations.ToArray());
    }

    public NormativeRuleStationEnvelopeStatusC2C0 Status { get; }
    public IReadOnlyList<NormativeRuleStationIssueC2C0> Issues { get; }
    public IReadOnlyList<NormativeRuleStationObservationC2C0> Observations { get; }
    public NormativeRuleStationObservationC2C0? HighestObservedUtilization =>
        Status == NormativeRuleStationEnvelopeStatusC2C0.InvalidInput
            ? null : Observations.OrderByDescending(o => o.Utilization).FirstOrDefault();
    public bool ContinuousNormativeMaximumEstablished => false;
    public bool NormativeRuleIntervalCoverageEstablished => false;
    public bool CompleteActionSetEstablished => false;
    public bool MemberAdequacyEstablished => false;
    public bool WholeStructureVerified => false;
    public bool CapturedReportsIndependentlyAuthenticated => false;
}

/// <summary>
/// Reviews consistency of real DesignCheckReport objects for a SINGLE rule,
/// member, edition, FEM target and profile. It is impossible for this API to
/// emit a member Pass; any recorded Failed status remains prominent even if
/// other stations are missing/unsupported. Use C1 separately to authenticate
/// signed N/V/M against an actual FEM solution.
/// </summary>
public static class NormativeRuleStationEnvelopeAuditorC2C0
{
    public static NormativeRuleStationEnvelopeResultC2C0 Audit(NormativeRuleStationPlanC2C0 plan)
    {
        static NormativeRuleStationEnvelopeResultC2C0 Invalid(string code, string message) =>
            new(NormativeRuleStationEnvelopeStatusC2C0.InvalidInput,
                new[] { new NormativeRuleStationIssueC2C0(code, message) },
                Array.Empty<NormativeRuleStationObservationC2C0>());

        if (plan is null || plan.Member is null || plan.Code is null || plan.StationReports is null ||
            string.IsNullOrWhiteSpace(plan.PlanId) || string.IsNullOrWhiteSpace(plan.RuleId) ||
            string.IsNullOrWhiteSpace(plan.TargetId) || string.IsNullOrWhiteSpace(plan.Member.MemberId) ||
            string.IsNullOrWhiteSpace(plan.Member.SectionId) ||
            !Enum.IsDefined(plan.Member.Material) || !Enum.IsDefined(plan.LimitState) ||
            !Enum.IsDefined(plan.TargetKind) ||
            !double.IsFinite(plan.Member.MemberLengthMetres) || plan.Member.MemberLengthMetres <= 0 ||
            string.IsNullOrWhiteSpace(plan.Code.DocumentId) ||
            string.IsNullOrWhiteSpace(plan.Code.EditionId) ||
            string.IsNullOrWhiteSpace(plan.Code.NationalAnnexId))
            return Invalid("C2C0_PLAN_INVALID", "Member, standard edition, rule, FEM target and station list must be explicit.");

        if (plan.StationReports.Count == 0)
            return new NormativeRuleStationEnvelopeResultC2C0(
                NormativeRuleStationEnvelopeStatusC2C0.ScreeningIncomplete,
                new[] { new NormativeRuleStationIssueC2C0("C2C0_NO_STATIONS", "No normative rule station reports supplied.") },
                Array.Empty<NormativeRuleStationObservationC2C0>());

        List<NormativeRuleStationObservationC2C0> observed = new();
        List<NormativeRuleStationIssueC2C0> issues = new();
        HashSet<(double Distance, DesignEffectSectionSide Side)> seen = new();
        string? profileId = null;
        string? analysisId = null;
        string? calculatedUnit = null;
        string? calculatedClause = null;
        NormativeCoefficient[]? coefficients = null;

        foreach (DesignCheckReport report in plan.StationReports)
        {
            if (report is null || report.Request is null || report.Results is null ||
                report.Request.Member is null || report.Request.Profile is null ||
                report.Request.Profile.Code is null || report.Request.Profile.Coefficients is null ||
                report.Request.Effects is null || report.Request.RuleIds is null)
                return Invalid("C2C0_REPORT_INVALID", "Incomplete normative report object.");

            DesignCheckRequest request = report.Request;
            DesignEffectSnapshot e = request.Effects;
            if (request.Member != plan.Member || request.Profile.Code != plan.Code ||
                request.LimitState != plan.LimitState ||
                request.RuleIds.Count != 1 || !Same(request.RuleIds[0], plan.RuleId) ||
                report.Results.Count != 1 || report.Results[0] is null ||
                !Same(report.Results[0].RuleId, plan.RuleId) ||
                e.Origin != DesignEffectOrigin.FemAnalysis ||
                e.MemberId is null || !Same(e.MemberId, plan.Member.MemberId) ||
                e.TargetKind != plan.TargetKind || !Same(e.CombinationId, plan.TargetId) ||
                !e.SectionSide.HasValue || !Enum.IsDefined(e.SectionSide.Value) ||
                !double.IsFinite(e.PositionMetres) || e.PositionMetres < 0 ||
                e.PositionMetres > plan.Member.MemberLengthMetres ||
                string.IsNullOrWhiteSpace(e.AnalysisId) ||
                string.IsNullOrWhiteSpace(request.Profile.ProfileId))
                return Invalid("C2C0_STATION_PROVENANCE_MISMATCH",
                    "A captured single-rule report differs in member, section, edition, target, identity or station side.");

            if (!seen.Add((e.PositionMetres, e.SectionSide.Value)))
                return Invalid("C2C0_DUPLICATE_STATION", "The same station and side were supplied more than once.");

            NormativeCoefficient[] current = request.Profile.Coefficients
                .OrderBy(c => c?.Id, StringComparer.OrdinalIgnoreCase).ToArray()!;
            if (current.Any(c => c is null || string.IsNullOrWhiteSpace(c.Id) ||
                string.IsNullOrWhiteSpace(c.SourceClause) || !double.IsFinite(c.Value)))
                return Invalid("C2C0_COEFFICIENT_INVALID", "Invalid normative coefficient evidence.");
            if (current.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != current.Length)
                return Invalid("C2C0_COEFFICIENT_DUPLICATE", "A normative coefficient identifier occurs more than once.");
            if (profileId is not null && (!Same(profileId, request.Profile.ProfileId) ||
                !Same(analysisId, e.AnalysisId) || !current.SequenceEqual(coefficients!)))
                return Invalid("C2C0_PROFILE_DRIFT",
                    "Different profiles, normative coefficients or FEM analysis identities cannot form one envelope.");
            profileId ??= request.Profile.ProfileId;
            analysisId ??= e.AnalysisId;
            coefficients ??= current;

            DesignCheckResult rule = report.Results[0];
            if (!Enum.IsDefined(rule.Status))
                return Invalid("C2C0_RESULT_STATUS_INVALID", "Unknown normative result status.");
            if (rule.Status is DesignCheckStatus.Passed or DesignCheckStatus.Failed)
            {
                if (rule.Demand is not double demand || rule.Resistance is not double resistance ||
                    rule.Utilization is not double utilization || !double.IsFinite(demand) || demand < 0 ||
                    !double.IsFinite(resistance) || resistance <= 0 || !double.IsFinite(utilization) ||
                    utilization < 0 || string.IsNullOrWhiteSpace(rule.SourceClause) ||
                    string.IsNullOrWhiteSpace(rule.Unit) || string.IsNullOrWhiteSpace(rule.CalculationTrace) ||
                    !double.IsFinite(demand / resistance) ||
                    !Near(utilization, demand / resistance) ||
                    (rule.Status == DesignCheckStatus.Passed) != (utilization <= 1))
                    return Invalid("C2C0_RULE_RESULT_INCONSISTENT",
                        "A calculated rule result has invalid demand/resistance/utilization or contradicts its Passed/Failed status.");

                if (calculatedUnit is not null &&
                    (!Same(calculatedUnit, rule.Unit) || !Same(calculatedClause, rule.SourceClause)))
                    return Invalid("C2C0_RULE_FORMULA_DRIFT",
                        "Different result units or source clauses cannot be joined as one normative rule envelope.");
                calculatedUnit ??= rule.Unit;
                calculatedClause ??= rule.SourceClause;

                observed.Add(new NormativeRuleStationObservationC2C0(
                    rule.RuleId, plan.TargetId, e.PositionMetres, e.SectionSide.Value,
                    request.Profile.ProfileId, utilization, rule.Status, rule.SourceClause));
            }
            else
                issues.Add(new NormativeRuleStationIssueC2C0("C2C0_RULE_NOT_CALCULATED",
                    "Station x=" + e.PositionMetres.ToString("G17", System.Globalization.CultureInfo.InvariantCulture) +
                    " reports " + rule.Status + "; no utilization inferred."));
        }

        // A failed actual rule at an observed section is a failure regardless
        // of missing stations or missing proofs elsewhere in the plan.
        bool failed = observed.Any(x => x.RuleStatus == DesignCheckStatus.Failed);
        if (failed)
            issues.Add(new NormativeRuleStationIssueC2C0("C2C0_OBSERVED_RULE_FAILURE",
                "A captured rule is Failed at at least one declared station. Do not hide this by other incomplete results."));
        else
            issues.Add(new NormativeRuleStationIssueC2C0("C2C0_CONTINUOUS_MAXIMUM_UNPROVEN",
                "Even all-Passed stations do not prove the maximum of the actual normative formula between stations."));

        issues.Add(new NormativeRuleStationIssueC2C0("C2C0_FEM_NOT_REAUTHENTICATED",
            "Captured reports have not been independently matched to the FEM; use the C1 station auditor."));

        return new NormativeRuleStationEnvelopeResultC2C0(
            failed ? NormativeRuleStationEnvelopeStatusC2C0.ObservedRuleFailure :
                NormativeRuleStationEnvelopeStatusC2C0.ScreeningIncomplete,
            issues, observed);
    }

    private static bool Same(string? a, string? b) => !string.IsNullOrWhiteSpace(a) &&
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    private static bool Near(double a, double b) =>
        Math.Abs(a - b) <= 1e-12 + 1e-10 * Math.Max(Math.Abs(a), Math.Abs(b));
}
