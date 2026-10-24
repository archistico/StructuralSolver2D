using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Engine;

/// <summary>
/// Dispatches audited, edition-specific design rules. With zero registered rules every request is
/// Unsupported; the engine never fabricates a normative demand, resistance or successful result.
/// </summary>
public sealed class DesignCheckEngine
{
    private readonly IReadOnlyDictionary<string, IDesignCheckRule> rules;

    public DesignCheckEngine(IEnumerable<IDesignCheckRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var registry = new Dictionary<string, IDesignCheckRule>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in rules)
        {
            if (rule is null) throw new ArgumentException("Null design rule.", nameof(rules));
            if (string.IsNullOrWhiteSpace(rule.RuleId) || rule.Code is null ||
                string.IsNullOrWhiteSpace(rule.Code.DocumentId) ||
                string.IsNullOrWhiteSpace(rule.Code.EditionId) ||
                string.IsNullOrWhiteSpace(rule.Code.NationalAnnexId) ||
                string.IsNullOrWhiteSpace(rule.SourceClause) ||
                !Enum.IsDefined(rule.Material) || !Enum.IsDefined(rule.LimitState))
                throw new ArgumentException("Incomplete design-rule identity or source clause.", nameof(rules));
            if (!registry.TryAdd(rule.RuleId, rule))
                throw new ArgumentException($"Duplicate design rule ID: {rule.RuleId}", nameof(rules));
        }
        this.rules = registry;
    }

    public DesignCheckReport Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        // No request identifiers -> zero evaluations, NEVER a successful report.
        if (request.RuleIds is null || request.RuleIds.Count == 0)
            return new DesignCheckReport(request, Array.Empty<DesignCheckResult>());

        var invalid = ValidateRequest(request);
        var results = new List<DesignCheckResult>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var ruleId in request.RuleIds)
        {
            var id = ruleId ?? "<null>";
            if (!ids.Add(id))
            {
                results.Add(new DesignCheckResult(id, DesignCheckStatus.InvalidInput, "Duplicate requested rule ID."));
                continue;
            }
            if (invalid is not null)
            {
                results.Add(new DesignCheckResult(id, DesignCheckStatus.InvalidInput, invalid));
                continue;
            }
            if (string.IsNullOrWhiteSpace(id))
            {
                results.Add(new DesignCheckResult(id, DesignCheckStatus.InvalidInput, "Empty rule ID."));
                continue;
            }
            if (!rules.TryGetValue(id, out var rule))
            {
                results.Add(new DesignCheckResult(id, DesignCheckStatus.Unsupported,
                    "No validated design rule is registered for this ID."));
                continue;
            }
            if (rule.Code != request.Profile.Code)
            {
                results.Add(new DesignCheckResult(id, DesignCheckStatus.Unsupported,
                    "Requested standard, edition or national annex differs from the rule's explicit version."));
                continue;
            }
            if (rule.Material != request.Member.Material || rule.LimitState != request.LimitState)
            {
                results.Add(new DesignCheckResult(id, DesignCheckStatus.NotApplicable,
                    "Rule material or limit state differs from the requested context.", rule.SourceClause));
                continue;
            }

            try
            {
                var evaluation = rule.Evaluate(request);
                if (evaluation is null)
                {
                    results.Add(Error(id, "Rule returned a null evaluation.", rule.SourceClause));
                    continue;
                }
                switch (evaluation.Kind)
                {
                    case DesignRuleEvaluationKind.MissingData:
                        results.Add(new DesignCheckResult(id, DesignCheckStatus.MissingData,
                            SafeReason(evaluation.Message), rule.SourceClause));
                        break;
                    case DesignRuleEvaluationKind.NotApplicable:
                        results.Add(new DesignCheckResult(id, DesignCheckStatus.NotApplicable,
                            SafeReason(evaluation.Message), rule.SourceClause));
                        break;
                    case DesignRuleEvaluationKind.Calculated:
                        results.Add(Calculate(id, rule.SourceClause, evaluation));
                        break;
                    default:
                        results.Add(Error(id, "Rule returned an unknown evaluation kind.", rule.SourceClause));
                        break;
                }
            }
            catch (Exception ex)
            {
                results.Add(Error(id, $"Rule evaluation error ({ex.GetType().Name}).", rule.SourceClause));
            }
        }
        return new DesignCheckReport(request, results);
    }

    private static DesignCheckResult Calculate(string id, string clause, DesignRuleEvaluation value)
    {
        if (value.Demand is not double demand || !double.IsFinite(demand) || demand < 0 ||
            value.Resistance is not double capacity || !double.IsFinite(capacity) || capacity <= 0 ||
            string.IsNullOrWhiteSpace(value.Unit) || string.IsNullOrWhiteSpace(value.CalculationTrace))
            return Error(id, "Invalid rule output: demand, resistance, unit or audit trace.", clause);
        var utilization = demand / capacity;
        if (!double.IsFinite(utilization))
            return Error(id, "Invalid utilization: non-finite demand/resistance ratio.", clause);
        return new DesignCheckResult(id,
            utilization <= 1d ? DesignCheckStatus.Passed : DesignCheckStatus.Failed,
            "Rule calculation completed; result applies only to the declared scope and station.",
            clause, demand, capacity, utilization, value.Unit, value.CalculationTrace);
    }

    private static DesignCheckResult Error(string id, string message, string? clause) =>
        new(id, DesignCheckStatus.CalculationError, message, clause);

    private static string SafeReason(string? message) =>
        string.IsNullOrWhiteSpace(message) ? "Rule did not provide an explanation." : message;

    private static string? ValidateRequest(DesignCheckRequest r)
    {
        if (r.Member is null || r.Profile is null || r.Effects is null || r.Profile.Code is null ||
            r.Profile.Coefficients is null)
            return "Missing member, profile, standard, coefficient list or structural results.";
        if (!Enum.IsDefined(r.Member.Material) || !Enum.IsDefined(r.LimitState) ||
            !Enum.IsDefined(r.Effects.Origin)) return "Unknown material, limit state or data origin.";
        if (string.IsNullOrWhiteSpace(r.Member.MemberId) ||
            string.IsNullOrWhiteSpace(r.Member.SectionId) ||
            string.IsNullOrWhiteSpace(r.Profile.ProfileId) ||
            string.IsNullOrWhiteSpace(r.Profile.Code.DocumentId) ||
            string.IsNullOrWhiteSpace(r.Profile.Code.EditionId) ||
            string.IsNullOrWhiteSpace(r.Profile.Code.NationalAnnexId) ||
            string.IsNullOrWhiteSpace(r.Effects.AnalysisId) ||
            string.IsNullOrWhiteSpace(r.Effects.CombinationId))
            return "Missing member, section, standard edition, national annex or analysis provenance identifier.";
        if (!double.IsFinite(r.Member.MemberLengthMetres) || r.Member.MemberLengthMetres <= 0 ||
            !double.IsFinite(r.Effects.PositionMetres) || r.Effects.PositionMetres < 0 ||
            r.Effects.PositionMetres > r.Member.MemberLengthMetres)
            return "Invalid member length or result station.";
        // Additional bridge provenance, when provided, must never contradict the member.
        if (r.Effects.MemberId is not null &&
            !string.Equals(r.Member.MemberId, r.Effects.MemberId, StringComparison.OrdinalIgnoreCase))
            return "FEM result member identifier differs from the requested design member.";
        if ((r.Effects.TargetKind.HasValue && !Enum.IsDefined(r.Effects.TargetKind.Value)) ||
            (r.Effects.SectionSide.HasValue && !Enum.IsDefined(r.Effects.SectionSide.Value)))
            return "Invalid FEM target kind or section-side provenance.";
        if (InvalidOptionalPositive(r.Member.UnbracedLengthInPlaneMetres) ||
            InvalidOptionalPositive(r.Member.UnbracedLengthOutOfPlaneMetres))
            return "Unbraced length must be finite and strictly positive when supplied.";
        if (InvalidOptionalFinite(r.Effects.AxialForceKn) ||
            InvalidOptionalFinite(r.Effects.ShearForceYKn) ||
            InvalidOptionalFinite(r.Effects.BendingMomentZKnm) ||
            InvalidOptionalFinite(r.Effects.DeflectionMetres))
            return "Structural effects must be finite when supplied.";
        var coefficientIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in r.Profile.Coefficients)
        {
            if (c is null || string.IsNullOrWhiteSpace(c.Id) ||
                string.IsNullOrWhiteSpace(c.SourceClause) || !double.IsFinite(c.Value))
                return "Invalid normative coefficient or missing source clause.";
            if (!coefficientIds.Add(c.Id)) return "Duplicate normative coefficient identifier.";
        }
        return null;
    }

    private static bool InvalidOptionalFinite(double? value) =>
        value.HasValue && !double.IsFinite(value.Value);

    private static bool InvalidOptionalPositive(double? value) =>
        value.HasValue && (!double.IsFinite(value.Value) || value.Value <= 0);
}
