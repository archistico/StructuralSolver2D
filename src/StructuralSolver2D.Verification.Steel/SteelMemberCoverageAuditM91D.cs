using System.Globalization;
using System.Text;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// A deliberately finite checklist, NOT the complete inventory of EC3 or NTC requirements.
/// Items omitted from this list (e.g. seismic, fire, fatigue, torsion, local loads) remain outside scope.
/// </summary>
public enum SteelCheckAreaM91D
{
    GrossTensionYield,
    NetSectionTensionRupture,
    GrossCompressionResistance,
    StrongAxisBendingResistance,
    InPlaneShearResistance,
    CombinedSectionNmv,
    FlexuralMemberBuckling,
    LateralTorsionalMemberBuckling,
    Connections,
    Serviceability,
    OtherRelevantRequirements
}

/// <summary>One specific rule, FEM target, member station AND side. No envelope mixing.</summary>
public sealed record SteelCoverageObligationM91D(
    string ObligationId,
    SteelCheckAreaM91D Area,
    string RuleId,
    string AnalysisId,
    string CombinationId,
    DesignEffectTargetKind TargetKind,
    double StationMetres,
    DesignEffectSectionSide SectionSide);

/// <summary>Exclusion is a declaration by the caller, NOT independent proof of non-applicability.</summary>
public sealed record SteelCoverageExclusionM91D(
    SteelCheckAreaM91D Area,
    string Reason,
    string ExternalJustification);

/// <summary>
/// Caller-authored schedule of checks for ONE member, code edition and limit state.
/// The audit verifies the evidence against this schedule; it DOES NOT establish that the
/// schedule is sufficient for structural or legal code compliance.
/// </summary>
public sealed record SteelMemberCoveragePlanM91D(
    string PlanId,
    DesignMemberContext Member,
    DesignCodeEdition Code,
    DesignLimitState LimitState,
    IReadOnlyList<SteelCoverageObligationM91D> Obligations,
    IReadOnlyList<SteelCoverageExclusionM91D> Exclusions,
    string AnalysisTargetSelectionEvidence,
    string CriticalStationSelectionEvidence,
    bool? CompletenessExternallyReviewed);

public enum SteelCoverageFindingStatusM91D
{
    PassedWithinRuleScope,
    FailedWithinRuleScope,
    MissingEvidence,
    NotEvaluated,
    InvalidOrConflictingEvidence
}

public enum SteelCoverageOutcomeM91D
{
    Incomplete,
    FailedWithinDeclaredScope,
    ChecksPassedWithinDeclaredScope,
    ChecksPassedWithExplicitExclusions
}

public sealed record SteelCoverageFindingM91D(
    SteelCoverageObligationM91D Obligation,
    SteelCoverageFindingStatusM91D Status,
    string Explanation,
    DesignCheckResult? RuleResult);

public sealed class SteelCoverageAuditReportM91D
{
    public const string Limitations =
        "AUDIT OF DECLARED RULE RESULTS ONLY — NOT an EC3/NTC member or structure compliance certificate. " +
        "The auditor does not prove that the load cases, combinations, stations, exclusions, physical restraints " +
        "or the list of required checks are complete. It does not independently authenticate rule output. " +
        "Fire, fatigue, seismic, torsion, connections and other unlisted phenomena may still require assessment.";

    public SteelCoverageAuditReportM91D(SteelMemberCoveragePlanM91D plan,
        IReadOnlyList<SteelCoverageFindingM91D> findings, IReadOnlyList<string> diagnostics,
        IReadOnlyList<SteelCheckAreaM91D> undeclaredAreas,
        int unexpectedResults)
    {
        Plan = plan;
        Findings = Array.AsReadOnly(findings.ToArray());
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        UndeclaredAreas = Array.AsReadOnly(undeclaredAreas.ToArray());
        UnexpectedResults = unexpectedResults;
    }

    public SteelMemberCoveragePlanM91D Plan { get; }
    public IReadOnlyList<SteelCoverageFindingM91D> Findings { get; }
    public IReadOnlyList<string> Diagnostics { get; }
    public IReadOnlyList<SteelCheckAreaM91D> UndeclaredAreas { get; }
    public int UnexpectedResults { get; }
    public int PassedCount => Findings.Count(x => x.Status == SteelCoverageFindingStatusM91D.PassedWithinRuleScope);
    public int FailedCount => Findings.Count(x => x.Status == SteelCoverageFindingStatusM91D.FailedWithinRuleScope);
    public int IncompleteCount => Findings.Count - PassedCount - FailedCount;

    public SteelCoverageOutcomeM91D Outcome =>
        FailedCount > 0 || Diagnostics.Any(x => x.StartsWith("FAILED:", StringComparison.Ordinal))
            ? SteelCoverageOutcomeM91D.FailedWithinDeclaredScope
            : Diagnostics.Count > 0 || UndeclaredAreas.Count > 0 || UnexpectedResults > 0 ||
              Findings.Count == 0 || IncompleteCount > 0
                ? SteelCoverageOutcomeM91D.Incomplete
                : (Plan.Exclusions?.Count ?? 0) > 0
                    ? SteelCoverageOutcomeM91D.ChecksPassedWithExplicitExclusions
                    : SteelCoverageOutcomeM91D.ChecksPassedWithinDeclaredScope;
}

/// <summary>
/// M91-D: post-process verified design-rule reports. It never runs a new engineering
/// formula and never treats a single rule PASS as approval of a member or structure.
/// All matches require EXACT station coordinates and point-load side; no x-tolerance
/// is used, because two one-sided limits at the same x can have different shear.
/// </summary>
public static class SteelMemberCoverageAuditorM91D
{
    private static readonly IReadOnlyDictionary<SteelCheckAreaM91D, string> ValidatedRuleByArea =
        new Dictionary<SteelCheckAreaM91D, string>
        {
            [SteelCheckAreaM91D.GrossTensionYield] = SteelGrossTensionYieldRuleM89B.Id,
            [SteelCheckAreaM91D.GrossCompressionResistance] = SteelGrossCompressionRuleM89B.Id,
            [SteelCheckAreaM91D.StrongAxisBendingResistance] = ElasticBendingRuleM89A.Id,
            [SteelCheckAreaM91D.InPlaneShearResistance] = SteelPureShearRuleM89B.Id,
            [SteelCheckAreaM91D.CombinedSectionNmv] = SteelClass3ElasticNmvRuleM90C.Id,
            [SteelCheckAreaM91D.FlexuralMemberBuckling] = SteelFlexuralBucklingRuleM91A.Id,
            [SteelCheckAreaM91D.LateralTorsionalMemberBuckling] = SteelLateralTorsionalBucklingRuleM91C.Id
        };

    public static SteelCoverageAuditReportM91D Audit(
        SteelMemberCoveragePlanM91D plan, IEnumerable<DesignCheckReport> designReports)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(designReports);
        var diagnostics = new List<string>();
        var reports = designReports.ToArray();
        var findings = new List<SteelCoverageFindingM91D>();
        if (reports.Any(r => r is null)) diagnostics.Add("Null design check report in supplied evidence.");
        reports = reports.Where(r => r is not null).ToArray();
        var obligations = plan.Obligations ?? Array.Empty<SteelCoverageObligationM91D>();
        var exclusions = plan.Exclusions ?? Array.Empty<SteelCoverageExclusionM91D>();
        var isContextValid = ValidatePlan(plan, obligations, exclusions, diagnostics);

        var areaDeclarations = obligations.Where(o => o is not null).Select(o => o.Area)
            .Concat(exclusions.Where(e => e is not null).Select(e => e.Area)).ToHashSet();
        var undeclared = Enum.GetValues<SteelCheckAreaM91D>()
            .Where(a => !areaDeclarations.Contains(a)).ToArray();
        if (undeclared.Length > 0)
            diagnostics.Add("Some audit categories are neither scheduled nor explicitly excluded. " +
                "No inference of non-applicability is permitted.");

        var used = new HashSet<(int ReportIndex, int ResultIndex)>();
        foreach (var obligation in obligations)
        {
            if (obligation is null) continue;
            string issue = "Invalid obligation or unsupported rule mapping.";
            if (!isContextValid || !ValidObligation(obligation, plan, out issue))
            {
                findings.Add(new SteelCoverageFindingM91D(obligation,
                    SteelCoverageFindingStatusM91D.NotEvaluated,
                    isContextValid ? issue : "Invalid audit plan: evidence cannot be accepted.", null));
                continue;
            }
            var matches = new List<(int ri, int xi, DesignCheckResult result)>();
            for (var ri = 0; ri < reports.Length; ri++)
            {
                var report = reports[ri];
                if (!RequestMatches(report.Request, plan, obligation)) continue;
                for (var xi = 0; xi < report.Results.Count; xi++)
                {
                    var r = report.Results[xi];
                    if (r is not null && Same(r.RuleId, obligation.RuleId))
                        matches.Add((ri, xi, r));
                }
            }
            if (matches.Count != 1)
            {
                findings.Add(new SteelCoverageFindingM91D(obligation,
                    matches.Count == 0 ? SteelCoverageFindingStatusM91D.MissingEvidence :
                        SteelCoverageFindingStatusM91D.InvalidOrConflictingEvidence,
                    matches.Count == 0 ? "No exact matching rule result (member, section, case/combination, " +
                        "station, side, code and limit state)." :
                        "Ambiguous repeated evidence: multiple results match the same obligation.", null));
                foreach (var match in matches) used.Add((match.ri, match.xi));
                continue;
            }
            var unique = matches[0];
            used.Add((unique.ri, unique.xi));
            findings.Add(Classify(obligation, unique.result));
        }

        var unexpected = 0;
        for (var ri = 0; ri < reports.Length; ri++)
            for (var xi = 0; xi < reports[ri].Results.Count; xi++)
                if (!used.Contains((ri, xi)))
                {
                    unexpected++;
                    if (reports[ri].Results[xi]?.Status == DesignCheckStatus.Failed &&
                        Same(reports[ri].Request.Member?.MemberId, plan.Member?.MemberId))
                        diagnostics.Add("FAILED: additional failed rule result outside the declared check schedule.");
                }
        if (unexpected > 0) diagnostics.Add($"{unexpected} unexpected/unmatched rule results; " +
            "the auditor refuses to silently discard additional evidence.");
        return new SteelCoverageAuditReportM91D(plan, findings, diagnostics, undeclared, unexpected);
    }

    private static SteelCoverageFindingM91D Classify(SteelCoverageObligationM91D obligation,
        DesignCheckResult result)
    {
        if (result.Status is DesignCheckStatus.Passed or DesignCheckStatus.Failed)
        {
            if (!ValidCalculatedResult(result))
                return new(obligation, SteelCoverageFindingStatusM91D.InvalidOrConflictingEvidence,
                    "Malformed numerical evidence or inconsistent demand/capacity/status.", result);
            return new(obligation,
                result.Status == DesignCheckStatus.Passed ?
                    SteelCoverageFindingStatusM91D.PassedWithinRuleScope :
                    SteelCoverageFindingStatusM91D.FailedWithinRuleScope,
                "Rule result within the DECLARED scope only; all other checks remain independent.", result);
        }
        return new(obligation, SteelCoverageFindingStatusM91D.NotEvaluated,
            $"Rule did not calculate a resistance: {result.Status}. {result.Message}", result);
    }

    private static bool ValidCalculatedResult(DesignCheckResult r)
    {
        if (r.Demand is not double d || !double.IsFinite(d) || d < 0 ||
            r.Resistance is not double capacity || !double.IsFinite(capacity) || capacity <= 0 ||
            r.Utilization is not double u || !double.IsFinite(u) ||
            string.IsNullOrWhiteSpace(r.SourceClause) || string.IsNullOrWhiteSpace(r.Unit) ||
            string.IsNullOrWhiteSpace(r.CalculationTrace)) return false;
        var ratio = d / capacity;
        if (!double.IsFinite(ratio) || Math.Abs(ratio - u) > 1e-10 * Math.Max(1d, ratio))
            return false;
        return r.Status == DesignCheckStatus.Passed ? ratio <= 1d : ratio > 1d;
    }

    private static bool RequestMatches(DesignCheckRequest? request, SteelMemberCoveragePlanM91D plan,
        SteelCoverageObligationM91D o)
    {
        if (request?.Member is null || request.Profile?.Code is null || request.Effects is null ||
            request.RuleIds is null) return false;
        var e = request.Effects;
        return request.Member.Material == DesignMaterial.Steel &&
            Same(request.Member.MemberId, plan.Member.MemberId) &&
            Same(request.Member.SectionId, plan.Member.SectionId) &&
            request.Member.MemberLengthMetres == plan.Member.MemberLengthMetres &&
            request.LimitState == plan.LimitState && request.Profile.Code == plan.Code &&
            e.Origin == DesignEffectOrigin.FemAnalysis &&
            Same(e.MemberId, plan.Member.MemberId) &&
            Same(e.AnalysisId, o.AnalysisId) && Same(e.CombinationId, o.CombinationId) &&
            e.TargetKind == o.TargetKind && e.PositionMetres == o.StationMetres &&
            e.SectionSide == o.SectionSide &&
            request.RuleIds.Any(id => Same(id, o.RuleId));
    }

    private static bool ValidObligation(SteelCoverageObligationM91D o,
        SteelMemberCoveragePlanM91D plan, out string error)
    {
        error = "Invalid obligation, rule/category mapping, analysis target or section position.";
        return !string.IsNullOrWhiteSpace(o.ObligationId) &&
            !string.IsNullOrWhiteSpace(o.RuleId) && !string.IsNullOrWhiteSpace(o.AnalysisId) &&
            !string.IsNullOrWhiteSpace(o.CombinationId) && Enum.IsDefined(o.Area) &&
            Enum.IsDefined(o.TargetKind) && Enum.IsDefined(o.SectionSide) &&
            double.IsFinite(o.StationMetres) && o.StationMetres >= 0 &&
            o.StationMetres <= plan.Member.MemberLengthMetres &&
            ValidatedRuleByArea.TryGetValue(o.Area, out var ruleId) && Same(o.RuleId, ruleId);
    }

    private static bool ValidatePlan(SteelMemberCoveragePlanM91D p,
        IReadOnlyList<SteelCoverageObligationM91D> obligations,
        IReadOnlyList<SteelCoverageExclusionM91D> exclusions, List<string> errors)
    {
        if (p.Member is null || p.Code is null)
        {
            errors.Add("Missing steel member or standard identity.");
            return false;
        }
        if (p.Member.Material != DesignMaterial.Steel ||
            !Enum.IsDefined(p.LimitState) || string.IsNullOrWhiteSpace(p.PlanId) ||
            string.IsNullOrWhiteSpace(p.Member.MemberId) ||
            string.IsNullOrWhiteSpace(p.Member.SectionId) ||
            !double.IsFinite(p.Member.MemberLengthMetres) || p.Member.MemberLengthMetres <= 0 ||
            string.IsNullOrWhiteSpace(p.Code.DocumentId) ||
            string.IsNullOrWhiteSpace(p.Code.EditionId) ||
            string.IsNullOrWhiteSpace(p.Code.NationalAnnexId))
            errors.Add("Invalid steel member identity, length, edition, annex or limit state.");
        if (!Same(p.Code.DocumentId, "EN 1993-1-1") || !Same(p.Code.EditionId, "2005+A1:2014"))
            errors.Add("M91-D known-rule checklist is restricted to EN 1993-1-1:2005+A1:2014.");
        if (p.CompletenessExternallyReviewed != true ||
            string.IsNullOrWhiteSpace(p.AnalysisTargetSelectionEvidence) ||
            string.IsNullOrWhiteSpace(p.CriticalStationSelectionEvidence))
            errors.Add("External review of case/combination coverage and critical-station selection is required.");
        if (obligations.Count == 0) errors.Add("No planned rule obligations: no partial pass is possible.");
        if (obligations.Any(x => x is null) || exclusions.Any(x => x is null))
            errors.Add("Null coverage obligation or exclusion.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var o in obligations.Where(x => x is not null))
        {
            if (!ids.Add(o.ObligationId ?? string.Empty)) errors.Add("Duplicate obligation ID.");
            var key = $"{o.Area}|{o.RuleId?.ToUpperInvariant()}|{o.AnalysisId?.ToUpperInvariant()}|" +
                $"{o.CombinationId?.ToUpperInvariant()}|{o.TargetKind}|" +
                $"{o.StationMetres.ToString("R", CultureInfo.InvariantCulture)}|{o.SectionSide}";
            if (!keys.Add(key)) errors.Add("Duplicate rule/station/side obligation.");
        }
        var excludedAreas = new HashSet<SteelCheckAreaM91D>();
        foreach (var e in exclusions.Where(x => x is not null))
        {
            if (!Enum.IsDefined(e.Area) || string.IsNullOrWhiteSpace(e.Reason) ||
                string.IsNullOrWhiteSpace(e.ExternalJustification))
                errors.Add("Exclusion without valid category, reason and independent basis.");
            if (!excludedAreas.Add(e.Area)) errors.Add("Duplicate excluded check category.");
            if (obligations.Any(o => o is not null && o.Area == e.Area))
                errors.Add("Category is both scheduled for checking and excluded.");
        }
        return errors.Count == 0;
    }

    private static bool Same(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) &&
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Small human-readable audit artifact. This is NOT a signed engineering report.</summary>
public static class SteelCoverageMarkdownM91D
{
    public static string Render(SteelCoverageAuditReportM91D report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var p = report.Plan;
        var sb = new StringBuilder();
        sb.AppendLine("# M91-D — Report di copertura delle verifiche acciaio");
        sb.AppendLine();
        sb.AppendLine("**NON costituisce una dichiarazione di conformità dell'asta o della struttura.**");
        sb.AppendLine();
        sb.AppendLine($"Piano: `{Esc(p.PlanId)}` · Asta: `{Esc(p.Member?.MemberId)}` · " +
            $"Sezione: `{Esc(p.Member?.SectionId)}`");
        sb.AppendLine($"Edizione: `{Esc(p.Code?.DocumentId)}/{Esc(p.Code?.EditionId)}/{Esc(p.Code?.NationalAnnexId)}` · " +
            $"Esito piano: **{report.Outcome}**");
        sb.AppendLine($"Controlli superati: {report.PassedCount}, non superati: {report.FailedCount}, " +
            $"incompleti: {report.IncompleteCount}; evidenze non abbinate: {report.UnexpectedResults}.");
        sb.AppendLine();
        sb.AppendLine("| Obbligo | Famiglia | Analisi / target | x [m], lato | Esito | Sfruttamento |");
        sb.AppendLine("|---|---|---|---|---|---|");
        foreach (var f in report.Findings)
        {
            var o = f.Obligation;
            var utilization = f.RuleResult?.Utilization is double u && double.IsFinite(u)
                ? u.ToString("G9", CultureInfo.InvariantCulture) : "—";
            sb.AppendLine($"| {Esc(o.ObligationId)} | {o.Area} | " +
                $"{Esc(o.AnalysisId)} / {Esc(o.CombinationId)} | " +
                $"{o.StationMetres.ToString("G17", CultureInfo.InvariantCulture)}, {o.SectionSide} | " +
                $"{f.Status} | {utilization} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Controlli esclusi esplicitamente (NON verificati)");
        foreach (var exclusion in p.Exclusions ?? Array.Empty<SteelCoverageExclusionM91D>())
            sb.AppendLine($"- **{exclusion.Area}**: {Esc(exclusion.Reason)}. " +
                $"Base dichiarata: {Esc(exclusion.ExternalJustification)}.");
        sb.AppendLine();
        sb.AppendLine("## Famiglie non dichiarate");
        foreach (var area in report.UndeclaredAreas) sb.AppendLine($"- {area}");
        sb.AppendLine();
        sb.AppendLine("## Diagnostica");
        foreach (var diagnostic in report.Diagnostics) sb.AppendLine($"- {Esc(diagnostic)}");
        sb.AppendLine();
        sb.AppendLine("## Limitazioni obbligatorie");
        sb.AppendLine(SteelCoverageAuditReportM91D.Limitations);
        return sb.ToString();
    }

    private static string Esc(string? value) => (value ?? "<missing>")
        .Replace("|", "\\|", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);
}
