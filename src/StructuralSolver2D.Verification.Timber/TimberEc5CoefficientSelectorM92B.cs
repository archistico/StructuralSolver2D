using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>These values are recommended by first-generation EC5; they are NOT certified national-annex values.</summary>
public enum TimberGammaBasisM92B
{
    EurocodeRecommendedNotNationalAnnex,
    ExternallyDeclaredNationalAnnex
}

/// <summary>An independently identified, nonzero participating load action; never reconstructed from the net FEM force.</summary>
public sealed record TimberCombinationActionM92B(
    string ActionId, TimberLoadDurationM92A Duration,
    double NonzeroCombinationFactor, string ClassificationEvidence);

/// <summary>
/// The caller must attest that the COMPLETE list of participating actions was checked against
/// the analysis/combination; the selector cannot verify this from the resulting N/V/M.
/// </summary>
public sealed record TimberCoefficientSelectionM92B(
    DesignCodeEdition Code,
    TimberRectangularAssignmentM92A Assignment,
    IReadOnlyList<TimberCombinationActionM92B> ParticipatingActions,
    bool? ActionListCompletenessConfirmed,
    string ActionListEvidence,
    string Table31ApplicabilityEvidence,
    TimberGammaBasisM92B GammaBasis,
    double? DeclaredNationalGammaM = null,
    string? NationalGammaEvidence = null,
    double? DeclaredKcr = null,
    string? KcrEvidence = null);

public sealed record TimberSelectedCoefficientsM92B(
    NormativeProfile Profile,
    TimberRectangularAssignmentM92A Assignment,
    TimberLoadDurationM92A GoverningDuration,
    double Kmod, double GammaM,
    IReadOnlyList<TimberCombinationActionM92B> ParticipatingActions,
    string Audit)
{
    public bool EstablishesMemberCompliance => false;
    public bool AutomaticallyValidatesNationalAnnex => false;

    /// <summary>Prefer this bound rule when using M92-B. It rejects a profile from any other FEM target.</summary>
    public IDesignCheckRule CreateBoundRule(TimberPureEffectM92A effect,
        CharacteristicMaterialCatalogM88C materials) =>
        new TimberTargetBoundRuleM92B(this, effect, materials);
}

internal sealed class TimberTargetBoundRuleM92B : IDesignCheckRule
{
    private readonly TimberSelectedCoefficientsM92B selected;
    private readonly TimberRectangularSectionRuleM92A inner;

    internal TimberTargetBoundRuleM92B(TimberSelectedCoefficientsM92B selected,
        TimberPureEffectM92A effect, CharacteristicMaterialCatalogM88C materials)
    {
        this.selected = selected;
        inner = new TimberRectangularSectionRuleM92A(selected.Profile.Code, effect,
            materials, new[] { selected.Assignment });
    }

    public string RuleId => inner.RuleId;
    public DesignCodeEdition Code => inner.Code;
    public DesignMaterial Material => inner.Material;
    public DesignLimitState LimitState => inner.LimitState;
    public string SourceClause => inner.SourceClause;

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Profile is null || request.Effects is null || request.Member is null ||
            request.Profile.Code != selected.Profile.Code ||
            !string.Equals(request.Profile.ProfileId, selected.Profile.ProfileId, StringComparison.Ordinal) ||
            !string.Equals(request.Effects.AnalysisId, selected.Assignment.AnalysisId, StringComparison.Ordinal) ||
            !string.Equals(request.Effects.CombinationId, selected.Assignment.TargetId, StringComparison.Ordinal) ||
            !string.Equals(request.Member.MemberId, selected.Assignment.MemberId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Member.SectionId, selected.Assignment.SectionId, StringComparison.OrdinalIgnoreCase) ||
            request.Effects.TargetKind != selected.Assignment.TargetKind)
            return DesignRuleEvaluation.MissingData("M92-B coefficient profile does not belong to this exact member/analysis/target.");
        if (request.Profile.Coefficients is null ||
            request.Profile.Coefficients.Count != selected.Profile.Coefficients.Count ||
            selected.Profile.Coefficients.Any(expected => !request.Profile.Coefficients.Any(actual =>
                actual is not null && actual.Id == expected.Id && actual.Value == expected.Value &&
                actual.SourceClause == expected.SourceClause)))
            return DesignRuleEvaluation.MissingData("M92-B coefficient profile values or evidence differ from the selected version.");
        return inner.Evaluate(request);
    }
}

/// <summary>
/// Deterministic EC5 first-generation Table 3.1 selector: from ALL nonzero actions,
/// take the shortest-duration class (Permanent &lt; ... &lt; Instantaneous).
/// Gamma_M recommended Table 2.3 is opt-in only under a conspicuous NON-NA marker.
/// For an actual national annex gamma_M is caller-provided, with independent evidence.
/// The returned pair (assignment/profile) is bound to the same FEM analysis target.
/// </summary>
public sealed class TimberEc5CoefficientSelectorM92B
{
    public const string RecommendedNonNationalAnnexId = "EC5-RECOMMENDED-NOT-NATIONAL-ANNEX";
    private readonly TimberEc5CoefficientTablesM92B tables;
    private readonly CharacteristicMaterialCatalogM88C materialCatalog;

    public TimberEc5CoefficientSelectorM92B(
        TimberEc5CoefficientTablesM92B tables,
        CharacteristicMaterialCatalogM88C materialCatalog)
    {
        this.tables = tables ?? throw new ArgumentNullException(nameof(tables));
        this.materialCatalog = materialCatalog ?? throw new ArgumentNullException(nameof(materialCatalog));
    }

    public TimberSelectedCoefficientsM92B Select(TimberCoefficientSelectionM92B input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Code is null || input.Code.DocumentId != "EN 1995-1-1" ||
            input.Code.EditionId != TimberRectangularSectionRuleM92A.Edition ||
            string.IsNullOrWhiteSpace(input.Code.NationalAnnexId))
            throw new NotSupportedException("Only explicitly identified first-generation EC5 is supported; no 2025 EC5 or NTC fallback.");
        var assignment = input.Assignment ?? throw new ArgumentException("Member assignment is required.", nameof(input));
        if (string.IsNullOrWhiteSpace(assignment.MemberId) ||
            string.IsNullOrWhiteSpace(assignment.SectionId) ||
            string.IsNullOrWhiteSpace(assignment.StrengthClassId) ||
            string.IsNullOrWhiteSpace(assignment.AnalysisId) ||
            string.IsNullOrWhiteSpace(assignment.TargetId) ||
            !Enum.IsDefined(assignment.TargetKind) ||
            assignment.ServiceClass is not (1 or 2 or 3) ||
            string.IsNullOrWhiteSpace(assignment.ServiceClassEvidence))
            throw new ArgumentException("Exact member, FEM analysis target, service class and evidence are required.", nameof(input));
        if (input.ActionListCompletenessConfirmed != true ||
            string.IsNullOrWhiteSpace(input.ActionListEvidence) ||
            string.IsNullOrWhiteSpace(input.Table31ApplicabilityEvidence))
            throw new ArgumentException("Complete participating actions and Table 3.1 applicability must be independently evidenced.", nameof(input));
        if (input.ParticipatingActions is null || input.ParticipatingActions.Count == 0)
            throw new ArgumentException("At least one independently declared participating action is required.", nameof(input));
        var actions = input.ParticipatingActions.ToArray();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in actions)
        {
            if (action is null || string.IsNullOrWhiteSpace(action.ActionId) ||
                !ids.Add(action.ActionId) || !Enum.IsDefined(action.Duration) ||
                !double.IsFinite(action.NonzeroCombinationFactor) || action.NonzeroCombinationFactor == 0 ||
                string.IsNullOrWhiteSpace(action.ClassificationEvidence))
                throw new ArgumentException("Distinct, finite nonzero participating actions with classified duration and evidence are mandatory.", nameof(input));
        }
        if (!Enum.IsDefined(input.GammaBasis))
            throw new ArgumentOutOfRangeException(nameof(input), "Unsupported gamma_M selection basis.");
        var material = materialCatalog.FindTimber(assignment.StrengthClassId);
        if (material.Kind is not (StrengthMaterialKindM88C.SolidSoftwood or StrengthMaterialKindM88C.HomogeneousGlulam))
            throw new NotSupportedException("Only solid softwood and homogeneous glulam are supported.");

        // The enum order is deliberately shortest LAST. This is a selection over DECLARED actions only.
        var governing = actions.Max(action => action.Duration);
        if (assignment.LoadDuration is not null && assignment.LoadDuration != governing)
            throw new ArgumentException("Declared member duration disagrees with the complete load-combination action list.", nameof(input));

        double gamma;
        string gammaSource;
        if (input.GammaBasis == TimberGammaBasisM92B.EurocodeRecommendedNotNationalAnnex)
        {
            if (input.Code.NationalAnnexId != RecommendedNonNationalAnnexId ||
                input.DeclaredNationalGammaM is not null || !string.IsNullOrWhiteSpace(input.NationalGammaEvidence))
                throw new ArgumentException("Recommended gamma_M requires the explicit non-NA identifier, with no national gamma data.", nameof(input));
            gamma = tables.RecommendedGammaM(material.Kind);
            gammaSource = $"{tables.GammaSource}; {tables.SourceUrl}; RECOMMENDED ONLY, NOT a national annex";
        }
        else
        {
            if (input.Code.NationalAnnexId == RecommendedNonNationalAnnexId ||
                input.DeclaredNationalGammaM is not double candidate ||
                !double.IsFinite(candidate) || candidate <= 0 ||
                string.IsNullOrWhiteSpace(input.NationalGammaEvidence))
                throw new ArgumentException("Actual national annex requires an explicitly documented positive gamma_M; no defaults.", nameof(input));
            gamma = candidate;
            gammaSource = $"User-provided {input.Code.NationalAnnexId}, NOT independently verified by selector: {input.NationalGammaEvidence}";
        }
        var kmod = tables.Kmod(material.Kind, assignment.ServiceClass.Value, governing);
        string actionTrace = string.Join("; ", actions.Select(a =>
            $"{a.ActionId}({a.Duration}, factor={a.NonzeroCombinationFactor.ToString("G17", CultureInfo.InvariantCulture)}, evidence={a.ClassificationEvidence})"));
        string durationEvidence = $"M92-B complete action list: {input.ActionListEvidence}; selected {governing} " +
            $"from {actionTrace}";
        var prepared = assignment with
        {
            LoadDuration = governing,
            LoadDurationEvidence = durationEvidence
        };
        var coeff = new List<NormativeCoefficient>
        {
            new(TimberRectangularSectionRuleM92A.KmodId, kmod,
                $"{tables.KmodSource}; {tables.SourceUrl}; annex applicability declared: {input.Table31ApplicabilityEvidence}",
                $"{material.Kind}, service class {prepared.ServiceClass}, {governing}, target {prepared.AnalysisId}/{prepared.TargetId}"),
            new(TimberRectangularSectionRuleM92A.GammaId, gamma, gammaSource,
                $"{input.GammaBasis}, {material.Kind}, target {prepared.AnalysisId}/{prepared.TargetId}")
        };
        if (input.DeclaredKcr is double kcr)
        {
            if (!double.IsFinite(kcr) || kcr <= 0 || kcr > 1 || string.IsNullOrWhiteSpace(input.KcrEvidence))
                throw new ArgumentException("kcr must have explicit evidence and be in (0,1]; never defaulted.", nameof(input));
            coeff.Add(new NormativeCoefficient(TimberRectangularSectionRuleM92A.CrackId, kcr,
                $"Externally declared crack allowance: {input.KcrEvidence}",
                "Not calculated by M92-B; engineering input only"));
        }
        else if (!string.IsNullOrWhiteSpace(input.KcrEvidence))
            throw new ArgumentException("kcr evidence without a numeric kcr is inconsistent.", nameof(input));

        var profile = new NormativeProfile(
            $"EC5-M92B-{prepared.AnalysisId}-{prepared.TargetId}-{prepared.MemberId}",
            input.Code, Array.AsReadOnly(coeff.ToArray()));
        var audit = $"M92-B: member={prepared.MemberId}, material={material.Id} ({material.Kind}), " +
            $"analysis={prepared.AnalysisId}, target={prepared.TargetId}, service={prepared.ServiceClass} " +
            $"({prepared.ServiceClassEvidence}), governing={governing}, kmod={kmod.ToString("G17", CultureInfo.InvariantCulture)}, " +
            $"gamma_M={gamma.ToString("G17", CultureInfo.InvariantCulture)}, basis={input.GammaBasis}, " +
            $"edition={input.Code.EditionId}, annex={input.Code.NationalAnnexId}. " +
            $"Actions: {actionTrace}. {input.ActionListEvidence}. " +
            "NO automated verification of national annex applicability, action completeness, kcr or member compliance.";
        return new TimberSelectedCoefficientsM92B(profile, prepared, governing, kmod, gamma,
            Array.AsReadOnly(actions), audit);
    }
}
