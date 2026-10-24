using System.Globalization;
using System.Text;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>
/// FINITE project checklist, not the universe of NTC obligations. Pure and combined checks
/// are different obligations; a result at one station never covers another station.
/// Book-derived model risks are warnings/obligations, not new normative formulas.
/// </summary>
public enum NtcTimberCoverageAreaG6
{
    TensionParallel, CompressionParallel, StrongAxisBending, InPlaneShear,
    TensionBending, CompressionBending, BendingAndShear,
    ColumnFlexuralBuckling, BeamLateralTorsionalBuckling,
    SleRareVariableDeflection, SleRareTotalDeflection, SleFinalDeflection,
    TensionPerpendicularToGrain, CompressionPerpendicularToGrain, InclinedCompression,
    ShearAndTorsion, LocalBearingAndSupports, NotchesHolesAndDamagedSections,
    ConnectionsAndEccentricities, WeakAxisAndOutOfPlaneEffects,
    SecondOrderAndImperfections, ShearDeformationEulerBernoulliLimit,
    UnilateralBracingAndTensionOnlyMembers, UpliftContactAndRealSupports,
    VibrationAndDynamicServiceability, OtherRelevantRequirements
}

/// <summary>Only an evidenced physical non-applicability can close an exclusion. Out of product scope never closes it.</summary>
public enum NtcTimberCoverageExclusionKindG6 { PhysicallyNotApplicable, UnsupportedOrNotEvaluated }

public enum NtcTimberCoverageFindingStatusG6
{
    PassedWithinRuleScope, FailedWithinRuleScope, MissingEvidence,
    NotEvaluated, InvalidOrConflictingEvidence
}

public enum NtcTimberCoverageOutcomeG6
{
    Incomplete, FailedWithinDeclaredScope, ChecksPassedWithinDeclaredScope,
    ChecksPassedWithExplicitPhysicalExclusions
}

/// <summary>Exact FEM snapshot plus precise normative profile selected by G1/G5. No force envelopes.</summary>
public sealed record NtcTimberCoverageObligationG6(
    string ObligationId, NtcTimberCoverageAreaG6 Area, string RuleId,
    DesignLimitState LimitState, string ProfileId, string AnalysisId,
    string CombinationId, double StationMetres, DesignEffectSectionSide SectionSide);

/// <summary>
/// External evidence is USER DECLARED, not independently authenticated. The auditor does
/// not infer physical non-applicability merely because a rule is unavailable.
/// </summary>
public sealed record NtcTimberCoverageExclusionG6(
    NtcTimberCoverageAreaG6 Area, NtcTimberCoverageExclusionKindG6 Kind,
    string PhysicalReason, string ExternalEvidenceReference);

/// <summary>
/// Caller-authored SLU+SLE plan for ONE timber member and edition. Coverage of the whole
/// member, load-combination generator, actual braces and documentation is NOT proved.
/// </summary>
public sealed record NtcTimberCoveragePlanG6(
    string PlanId, DesignMemberContext Member, DesignCodeEdition Code,
    IReadOnlyList<NtcTimberCoverageObligationG6> Obligations,
    IReadOnlyList<NtcTimberCoverageExclusionG6> Exclusions,
    bool? CombinationInventoryExternallyReviewed, string CombinationInventoryEvidence,
    bool? CriticalStationsExternallyReviewed, string CriticalStationsEvidence,
    bool? PhysicalModelLimitationsExternallyReviewed, string PhysicalModelEvidence);

public sealed record NtcTimberCoverageFindingG6(
    NtcTimberCoverageObligationG6 Obligation, NtcTimberCoverageFindingStatusG6 Status,
    string Explanation, DesignCheckResult? RuleResult);

public sealed class NtcTimberCoverageAuditReportG6
{
    public const string Limitations =
        "AUDIT OF DECLARED EVIDENCE ONLY; NOT A MEMBER, STRUCTURE, NTC, EC5 OR LEGAL COMPLIANCE CERTIFICATE. " +
        "The checklist is deliberately finite. Caller attestations do not independently prove load combinations, " +
        "station exhaustiveness, construction details, out-of-plane restraint effectiveness, actual joint behaviour, " +
        "fire/seismic/dynamic checks or material/product conformity. Audit cannot authenticate a fabricated rule result.";

    public NtcTimberCoverageAuditReportG6(NtcTimberCoveragePlanG6 plan,
        IEnumerable<NtcTimberCoverageFindingG6> findings, IEnumerable<string> diagnostics,
        IEnumerable<NtcTimberCoverageAreaG6> undeclaredAreas, int unmatchedResults,
        int unmatchedVerifiedFailures, bool planValid, int unsupportedExclusions)
    {
        Plan = plan;
        Findings = Array.AsReadOnly(findings.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        UndeclaredAreas = Array.AsReadOnly(undeclaredAreas.ToArray());
        UnmatchedResults = unmatchedResults;
        UnmatchedVerifiedFailures = unmatchedVerifiedFailures;
        PlanValid = planValid;
        UnsupportedExclusions = unsupportedExclusions;
    }

    public NtcTimberCoveragePlanG6 Plan { get; }
    public IReadOnlyList<NtcTimberCoverageFindingG6> Findings { get; }
    public IReadOnlyList<string> Diagnostics { get; }
    public IReadOnlyList<NtcTimberCoverageAreaG6> UndeclaredAreas { get; }
    public int UnmatchedResults { get; }
    public int UnmatchedVerifiedFailures { get; }
    public int UnsupportedExclusions { get; }
    public bool PlanValid { get; }
    public int PassedCount => Findings.Count(f => f.Status == NtcTimberCoverageFindingStatusG6.PassedWithinRuleScope);
    public int FailedCount => Findings.Count(f => f.Status == NtcTimberCoverageFindingStatusG6.FailedWithinRuleScope);
    public int IncompleteCount => Findings.Count - PassedCount - FailedCount;
    public NtcTimberCoverageOutcomeG6 Outcome =>
        FailedCount > 0 || UnmatchedVerifiedFailures > 0
            ? NtcTimberCoverageOutcomeG6.FailedWithinDeclaredScope
            : !PlanValid || Diagnostics.Count > 0 || UndeclaredAreas.Count > 0 ||
              UnsupportedExclusions > 0 || UnmatchedResults > 0 ||
              Findings.Count == 0 || IncompleteCount > 0
                ? NtcTimberCoverageOutcomeG6.Incomplete
                : Plan.Exclusions is { Count: > 0 }
                    ? NtcTimberCoverageOutcomeG6.ChecksPassedWithExplicitPhysicalExclusions
                    : NtcTimberCoverageOutcomeG6.ChecksPassedWithinDeclaredScope;
}

/// <summary>
/// G6 post-processor: NO modification of FEM, coefficients, G1–G5, or normative checks.
/// All matches exact including station + side, normalized case-insensitive identifiers,
/// edition, limit state and ProfileId (which identifies the intended action selection).
/// </summary>
public static class NtcTimberCoverageAuditorG6
{
    private static readonly IReadOnlyDictionary<NtcTimberCoverageAreaG6, (string Id, DesignLimitState Limit)> Rules =
        new Dictionary<NtcTimberCoverageAreaG6, (string, DesignLimitState)>
        {
            [NtcTimberCoverageAreaG6.TensionParallel] = (Ntc2018TimberRectangularSectionRuleG2.TensionId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.CompressionParallel] = (Ntc2018TimberRectangularSectionRuleG2.CompressionId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.StrongAxisBending] = (Ntc2018TimberRectangularSectionRuleG2.BendingId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.InPlaneShear] = (Ntc2018TimberRectangularSectionRuleG2.ShearId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.TensionBending] = (Ntc2018TimberCombinedSectionRuleG3.TensionBendingId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.CompressionBending] = (Ntc2018TimberCombinedSectionRuleG3.CompressionBendingId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.BendingAndShear] = (Ntc2018TimberCombinedSectionRuleG3.BendingShearId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.ColumnFlexuralBuckling] = (Ntc2018TimberStabilityRuleG4.ColumnId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.BeamLateralTorsionalBuckling] = (Ntc2018TimberStabilityRuleG4.BeamId, DesignLimitState.Ultimate),
            [NtcTimberCoverageAreaG6.SleRareVariableDeflection] = (Ntc2018TimberSleDeflectionRuleG5.VariableId, DesignLimitState.Serviceability),
            [NtcTimberCoverageAreaG6.SleRareTotalDeflection] = (Ntc2018TimberSleDeflectionRuleG5.RareTotalId, DesignLimitState.Serviceability),
            [NtcTimberCoverageAreaG6.SleFinalDeflection] = (Ntc2018TimberSleDeflectionRuleG5.FinalId, DesignLimitState.Serviceability)
        };

    public static NtcTimberCoverageAuditReportG6 Audit(NtcTimberCoveragePlanG6 plan,
        IEnumerable<DesignCheckReport> reports)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(reports);
        var diagnostics = new List<string>();
        var evidence = reports.ToArray();
        if (evidence.Any(r => r is null)) diagnostics.Add("Null rule report in supplied evidence.");
        var cleanReports = evidence.Where(r => r is not null).ToArray();
        var obligations = plan.Obligations ?? Array.Empty<NtcTimberCoverageObligationG6>();
        var exclusions = plan.Exclusions ?? Array.Empty<NtcTimberCoverageExclusionG6>();
        var validPlan = ValidatePlan(plan, obligations, exclusions, diagnostics);
        var declared = obligations.Where(o => o is not null).Select(o => o.Area)
            .Concat(exclusions.Where(e => e is not null).Select(e => e.Area)).ToHashSet();
        var undeclared = Enum.GetValues<NtcTimberCoverageAreaG6>()
            .Where(a => !declared.Contains(a)).ToArray();
        if (undeclared.Length > 0)
            diagnostics.Add("Unscheduled/unexcluded check areas remain: no implicit physical non-applicability.");
        var unsupported = exclusions.Count(e => e is not null &&
            e.Kind == NtcTimberCoverageExclusionKindG6.UnsupportedOrNotEvaluated);
        if (unsupported > 0)
            diagnostics.Add("Applicable or potentially applicable areas excluded solely because not implemented/evaluated.");

        var findings = new List<NtcTimberCoverageFindingG6>();
        var used = new HashSet<(int Report, int Result)>();
        foreach (var obligation in obligations)
        {
            if (obligation is null) continue;
            if (!validPlan || !ValidObligation(obligation,plan.Member))
            {
                findings.Add(new(obligation, NtcTimberCoverageFindingStatusG6.NotEvaluated,
                    "Invalid audit schedule or rule-category/limit-state mapping; result cannot be credited.", null));
                continue;
            }
            var matches = new List<(int Report, int Result, DesignCheckResult Value)>();
            for (var ri=0; ri<cleanReports.Length; ri++)
            {
                var report=cleanReports[ri];
                if (!Matches(report.Request,plan,obligation)) continue;
                for (var i=0; i<report.Results.Count; i++)
                    if (report.Results[i] is { } value && Same(value.RuleId,obligation.RuleId))
                        matches.Add((ri,i,value));
            }
            foreach (var m in matches) used.Add((m.Report,m.Result));
            if (matches.Count != 1)
            {
                findings.Add(new(obligation,
                    matches.Count==0 ? NtcTimberCoverageFindingStatusG6.MissingEvidence :
                        NtcTimberCoverageFindingStatusG6.InvalidOrConflictingEvidence,
                    matches.Count==0 ? "No exact rule result for analysis, combination, profile, station and side." :
                        "Multiple reports/results claim the same exact obligation.",null));
                continue;
            }
            var found=matches[0].Value;
            if (found.Status is DesignCheckStatus.Passed or DesignCheckStatus.Failed)
            {
                if (!ValidCalculated(found))
                {
                    findings.Add(new(obligation,NtcTimberCoverageFindingStatusG6.InvalidOrConflictingEvidence,
                        "Numerically inconsistent or untraceable rule result; no credit.",found));
                    continue;
                }
                findings.Add(new(obligation,found.Status==DesignCheckStatus.Passed ?
                    NtcTimberCoverageFindingStatusG6.PassedWithinRuleScope :
                    NtcTimberCoverageFindingStatusG6.FailedWithinRuleScope,
                    "Calculated RULE-ONLY result: not whole-member or whole-structure compliance.",found));
            }
            else
                findings.Add(new(obligation,NtcTimberCoverageFindingStatusG6.NotEvaluated,
                    $"Rule did not produce a calculated verification ({found.Status}): {found.Message}",found));
        }

        int unmatched=0, unmatchedFailures=0;
        for (var ri=0; ri<cleanReports.Length; ri++)
        {
            var report=cleanReports[ri];
            for (var i=0; i<report.Results.Count; i++)
            {
                if (used.Contains((ri,i))) continue;
                unmatched++;
                var result=report.Results[i];
                if (result is not null && result.Status==DesignCheckStatus.Failed &&
                    ValidCalculated(result) && Same(report.Request.Member?.MemberId,plan.Member?.MemberId) &&
                    Same(report.Request.Member?.SectionId,plan.Member?.SectionId) &&
                    report.Request.Member?.MemberLengthMetres==plan.Member?.MemberLengthMetres &&
                    report.Request.Member?.Material==DesignMaterial.Timber &&
                    report.Request.Profile?.Code==plan.Code &&
                    report.Request.Effects?.Origin==DesignEffectOrigin.FemAnalysis &&
                    report.Request.Effects?.TargetKind==DesignEffectTargetKind.LoadCombination &&
                    Same(report.Request.Effects?.MemberId,plan.Member?.MemberId) &&
                    obligations.Any(o=>o is not null &&
                        Same(o.AnalysisId,report.Request.Effects?.AnalysisId)))
                    unmatchedFailures++;
            }
        }
        if (unmatched>0) diagnostics.Add($"{unmatched} unmatched/unscheduled rule result(s); no silent discard.");
        if (unmatchedFailures>0) diagnostics.Add($"{unmatchedFailures} credible failed NTC results outside schedule retained as failures.");
        return new(plan,findings,diagnostics,undeclared,unmatched,unmatchedFailures,validPlan,unsupported);
    }

    private static bool ValidCalculated(DesignCheckResult r)
    {
        if (r.Demand is not double d || !double.IsFinite(d) || d<0 ||
            r.Resistance is not double c || !double.IsFinite(c) || c<=0 ||
            r.Utilization is not double u || !double.IsFinite(u) ||
            string.IsNullOrWhiteSpace(r.CalculationTrace) || string.IsNullOrWhiteSpace(r.Unit) ||
            string.IsNullOrWhiteSpace(r.SourceClause)) return false;
        double ratio=d/c;
        if (!double.IsFinite(ratio) || Math.Abs(u-ratio)>1e-10*Math.Max(1d,ratio)) return false;
        return r.Status==DesignCheckStatus.Passed ? ratio<=1d : ratio>1d;
    }

    private static bool Matches(DesignCheckRequest? r,NtcTimberCoveragePlanG6 p,NtcTimberCoverageObligationG6 o)
    {
        if (r?.Member is null || r.Profile?.Code is null || r.Effects is null || r.RuleIds is null)
            return false;
        var e=r.Effects;
        return r.Member.Material==DesignMaterial.Timber && r.Member.Material==p.Member.Material &&
            Same(r.Member.MemberId,p.Member.MemberId) && Same(r.Member.SectionId,p.Member.SectionId) &&
            r.Member.MemberLengthMetres==p.Member.MemberLengthMetres && r.Profile.Code==p.Code &&
            r.LimitState==o.LimitState && Same(r.Profile.ProfileId,o.ProfileId) &&
            e.Origin==DesignEffectOrigin.FemAnalysis &&
            e.TargetKind==DesignEffectTargetKind.LoadCombination &&
            Same(e.MemberId,p.Member.MemberId) && Same(e.AnalysisId,o.AnalysisId) &&
            Same(e.CombinationId,o.CombinationId) && e.PositionMetres==o.StationMetres &&
            e.SectionSide==o.SectionSide && r.RuleIds.Any(rule=>Same(rule,o.RuleId));
    }

    private static bool ValidObligation(NtcTimberCoverageObligationG6 o,DesignMemberContext? m) =>
        m is not null && !string.IsNullOrWhiteSpace(o.ObligationId) &&
        !string.IsNullOrWhiteSpace(o.RuleId) && !string.IsNullOrWhiteSpace(o.ProfileId) &&
        !string.IsNullOrWhiteSpace(o.AnalysisId) && !string.IsNullOrWhiteSpace(o.CombinationId) &&
        Enum.IsDefined(o.Area) && Enum.IsDefined(o.LimitState) && Enum.IsDefined(o.SectionSide) &&
        double.IsFinite(o.StationMetres) && o.StationMetres>=0 &&
        o.StationMetres<=m.MemberLengthMetres && Rules.TryGetValue(o.Area,out var known) &&
        Same(o.RuleId,known.Id) && o.LimitState==known.Limit;

    private static bool ValidatePlan(NtcTimberCoveragePlanG6 p,
        IReadOnlyList<NtcTimberCoverageObligationG6> obligations,
        IReadOnlyList<NtcTimberCoverageExclusionG6> exclusions,List<string> errors)
    {
        if (p.Member is null || p.Code is null)
        {
            errors.Add("Missing audited member or standard.");
            return false;
        }
        if (p.Member.Material!=DesignMaterial.Timber || !Same(p.Code.DocumentId,Ntc2018TimberCoefficientSelectorG1.Edition.DocumentId) ||
            p.Code!=Ntc2018TimberCoefficientSelectorG1.Edition ||
            string.IsNullOrWhiteSpace(p.PlanId) || string.IsNullOrWhiteSpace(p.Member.MemberId) ||
            string.IsNullOrWhiteSpace(p.Member.SectionId) || !double.IsFinite(p.Member.MemberLengthMetres) ||
            p.Member.MemberLengthMetres<=0)
            errors.Add("Invalid NTC2018 timber/edition/member identity.");
        if (p.CombinationInventoryExternallyReviewed!=true || string.IsNullOrWhiteSpace(p.CombinationInventoryEvidence) ||
            p.CriticalStationsExternallyReviewed!=true || string.IsNullOrWhiteSpace(p.CriticalStationsEvidence) ||
            p.PhysicalModelLimitationsExternallyReviewed!=true || string.IsNullOrWhiteSpace(p.PhysicalModelEvidence))
            errors.Add("External attestations required: load-case inventory, critical stations, physical model applicability.");
        if (obligations.Count==0) errors.Add("Empty obligation schedule is never a positive audit.");
        if (obligations.Any(o=>o is null) || exclusions.Any(e=>e is null))
            errors.Add("Null obligation or exclusion.");
        var ids=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keys=new HashSet<string>(StringComparer.Ordinal);
        var scheduledAreas=new HashSet<NtcTimberCoverageAreaG6>();
        foreach (var o in obligations.Where(o=>o is not null))
        {
            if (!ids.Add(o.ObligationId)) errors.Add("Duplicate obligation ID.");
            var key=$"{o.Area}|{o.RuleId?.ToUpperInvariant()}|{o.AnalysisId?.ToUpperInvariant()}|"+
                $"{o.CombinationId?.ToUpperInvariant()}|{o.ProfileId?.ToUpperInvariant()}|{o.LimitState}|"+
                $"{o.StationMetres.ToString("R",CultureInfo.InvariantCulture)}|{o.SectionSide}";
            if (!keys.Add(key)) errors.Add("Duplicate rule target/station/side obligation.");
            scheduledAreas.Add(o.Area);
            if (!ValidObligation(o,p.Member)) errors.Add("Invalid scheduled obligation or rule mapping.");
        }
        var excluded=new HashSet<NtcTimberCoverageAreaG6>();
        foreach (var e in exclusions.Where(e=>e is not null))
        {
            if (!Enum.IsDefined(e.Area) || !Enum.IsDefined(e.Kind) ||
                !excluded.Add(e.Area) || scheduledAreas.Contains(e.Area) ||
                string.IsNullOrWhiteSpace(e.PhysicalReason) ||
                string.IsNullOrWhiteSpace(e.ExternalEvidenceReference))
                errors.Add("Invalid/duplicate/contradictory exclusion; reason and independent source required.");
        }
        return errors.Count==0;
    }

    private static bool Same(string? a,string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) &&
        string.Equals(a,b,StringComparison.OrdinalIgnoreCase);
}

/// <summary>Report is a technical trace, not a professional verification report or certificate.</summary>
public static class NtcTimberCoverageMarkdownG6
{
    public static string Render(NtcTimberCoverageAuditReportG6 report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var p=report.Plan;
        var sb=new StringBuilder();
        sb.AppendLine("# StructuralSolver2D G6 — audit limitato dei controlli dichiarati");
        sb.AppendLine();
        sb.AppendLine("**NON costituisce una dichiarazione di conformità dell'asta o della struttura.**");
        sb.AppendLine($"Piano `{Clean(p.PlanId)}` — asta `{Clean(p.Member?.MemberId)}` — sezione `{Clean(p.Member?.SectionId)}`.");
        sb.AppendLine($"Norma `{Clean(p.Code?.DocumentId)}` / `{Clean(p.Code?.EditionId)}` / `{Clean(p.Code?.NationalAnnexId)}`.");
        sb.AppendLine($"Esito controlli dichiarati: **{report.Outcome}**. " +
            $"Superati: {report.PassedCount}; falliti: {report.FailedCount}; incompleti: {report.IncompleteCount}. " +
            $"Risultati fuori piano: {report.UnmatchedResults}.");
        sb.AppendLine();
        sb.AppendLine("| Obbligo | Categoria | Stato limite | Analisi / combinazione | Stazione [m] / lato | Esito | η |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var f in report.Findings)
        {
            var o=f.Obligation;
            var u=f.RuleResult?.Utilization is double value && double.IsFinite(value)
                ? value.ToString("G17",CultureInfo.InvariantCulture) : "—";
            sb.AppendLine($"| {Clean(o.ObligationId)} | {o.Area} | {o.LimitState} | " +
                $"{Clean(o.AnalysisId)} / {Clean(o.CombinationId)} | " +
                $"{o.StationMetres.ToString("G17",CultureInfo.InvariantCulture)} / {o.SectionSide} | {f.Status} | {u} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Esclusioni dichiarate (solo attestazioni esterne; NON verificate dall'auditor)");
        foreach (var e in p.Exclusions??Array.Empty<NtcTimberCoverageExclusionG6>())
            sb.AppendLine($"- **{e.Area} — {e.Kind}**: {Clean(e.PhysicalReason)}; documento: {Clean(e.ExternalEvidenceReference)}.");
        sb.AppendLine();
        sb.AppendLine("## Famiglie di controllo prive di dichiarazione");
        foreach (var a in report.UndeclaredAreas) sb.AppendLine($"- {a}");
        sb.AppendLine();
        sb.AppendLine("## Anomalie e motivi d'incompletezza");
        foreach (var d in report.Diagnostics) sb.AppendLine($"- {Clean(d)}");
        sb.AppendLine();
        sb.AppendLine("## Limitazioni sempre applicabili");
        sb.AppendLine(NtcTimberCoverageAuditReportG6.Limitations);
        return sb.ToString();
    }

    private static string Clean(string? text) => (text??"<missing>")
        .Replace("|","\\|",StringComparison.Ordinal)
        .Replace("\r"," ",StringComparison.Ordinal)
        .Replace("\n"," ",StringComparison.Ordinal);
}
