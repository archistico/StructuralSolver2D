using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Timber;

public enum Ntc2018TimberSituationG1 { FundamentalStr, Exceptional }
public enum Ntc2018GammaColumnG1 { A, B, NotApplicableExceptional }

/// <summary>Independent evidence of the requirements for NTC 2018 timber gamma_M Column B.</summary>
public sealed record Ntc2018ColumnBProductionEvidenceG1(
    bool? ContinuousProductionAndControlConfirmed,
    bool? Ntc11_7QualitySystemConfirmed,
    bool? ApprovedProductDocumentationConfirmed,
    string ProductCertificationReference,
    string ProductionControlReference,
    double? MeasuredResistanceCoefficientOfVariation,
    string CoefficientOfVariationEvidence);

/// <summary>
/// Complete combination manifest: zero-factor actions are recorded but do NOT influence kmod.
/// This describes already-combined actions and does NOT construct or certify an NTC combination.
/// </summary>
public sealed record Ntc2018CombinationActionG1(string ActionId,
    Ntc2018ActionCategoryG1 Category,
    TimberLoadDurationM92A Duration,
    double CombinationFactor,
    string ClassificationEvidence,
    string FactorEvidence);

public sealed record Ntc2018TimberSelectionG1(
    DesignCodeEdition Code,
    TimberRectangularAssignmentM92A Assignment,
    Ntc2018TimberSituationG1 Situation,
    string SituationEvidence,
    Ntc2018GammaColumnG1 GammaColumn,
    Ntc2018ColumnBProductionEvidenceG1? ColumnBProduction,
    IReadOnlyList<Ntc2018CombinationActionG1> CombinationActions,
    bool? CompleteActionListConfirmed,
    string CompletenessEvidence,
    string ExpectedCombinationFingerprint,
    string FingerprintMatchEvidence,
    string KmodApplicabilityEvidence,
    string MaterialEvidence,
    bool? CircolareCrackRuleApplies,
    string? CrackRuleApplicabilityEvidence);

public sealed record Ntc2018TimberSelectedCoefficientsG1(
    NormativeProfile Profile,
    TimberRectangularAssignmentM92A Assignment,
    Ntc2018TimberSituationG1 Situation,
    Ntc2018GammaColumnG1 GammaColumn,
    TimberLoadDurationM92A GoverningDuration,
    double Kmod, double GammaM, double? Kcr,
    string CombinationFingerprint,
    IReadOnlyList<Ntc2018CombinationActionG1> Manifest,
    string Audit)
{
    public bool IsDesignCheck => false;
    public bool EstablishesMemberCompliance => false;
    public bool ConfirmsWholeCombinationIsNormative => false;
    public bool IndependentlyConfirmsColumnBDocumentation => false;
}

/// <summary>
/// NTC 2018 timber *coefficient and provenance preparation only*, not a design-check rule.
/// DOES NOT adapt the EC5 M92-A rule by replacing its label: NTC design rules are a later gate.
/// This selector cannot independently verify declared environmental/product certificates or
/// the actual FEM combination; its external fingerprint must be matched to FEM source data.
/// </summary>
public sealed class Ntc2018TimberCoefficientSelectorG1
{
    public static readonly DesignCodeEdition Edition = new(
        Ntc2018TimberCoefficientTablesG1.DocumentId,
        Ntc2018TimberCoefficientTablesG1.EditionId,
        Ntc2018TimberCoefficientTablesG1.AnnexId);

    private readonly Ntc2018TimberCoefficientTablesG1 tables;
    private readonly CharacteristicMaterialCatalogM88C materials;

    public Ntc2018TimberCoefficientSelectorG1(Ntc2018TimberCoefficientTablesG1 tables,
        CharacteristicMaterialCatalogM88C materials)
    {
        this.tables = tables ?? throw new ArgumentNullException(nameof(tables));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
    }

    /// <summary>
    /// Deterministic SHA-256 over the FULL declared combination action manifest plus analysis/target.
    /// It is NOT proof that the caller's manifest equals the FEM combination: independent matching
    /// evidence is mandatory. Alphabetically sorted by ActionId to remove iteration-order effects.
    /// </summary>
    public static string Fingerprint(string analysisId, string combinationId,
        IReadOnlyList<Ntc2018CombinationActionG1> actions)
    {
        if (string.IsNullOrWhiteSpace(analysisId) || string.IsNullOrWhiteSpace(combinationId) ||
            actions is null || actions.Count == 0)
            throw new ArgumentException("Analysis/combination IDs and a nonempty action manifest are required.");
        var entries = actions.ToArray();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var action in entries)
        {
            if (action is null || string.IsNullOrWhiteSpace(action.ActionId) ||
                !ids.Add(action.ActionId) || !Enum.IsDefined(action.Category) ||
                !Enum.IsDefined(action.Duration) || !double.IsFinite(action.CombinationFactor) ||
                string.IsNullOrWhiteSpace(action.ClassificationEvidence) ||
                string.IsNullOrWhiteSpace(action.FactorEvidence))
                throw new ArgumentException("Unique actions, finite combination factors, categories and evidence are required.");
        }
        var output = new StringBuilder();
        static void Add(StringBuilder b, string value) => b.Append(value.Length)
            .Append(':').Append(value).Append('|');
        Add(output, "NTC2018-G1-COMBO-FP1");
        Add(output, analysisId);
        Add(output, combinationId);
        foreach (var action in entries.OrderBy(a => a.ActionId, StringComparer.OrdinalIgnoreCase))
        {
            Add(output, action.ActionId.ToUpperInvariant());
            Add(output, ((int)action.Category).ToString(CultureInfo.InvariantCulture));
            Add(output, ((int)action.Duration).ToString(CultureInfo.InvariantCulture));
            Add(output, action.CombinationFactor.ToString("G17", CultureInfo.InvariantCulture));
            Add(output, action.ClassificationEvidence);
            Add(output, action.FactorEvidence);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output.ToString())));
    }

    public Ntc2018TimberSelectedCoefficientsG1 Select(Ntc2018TimberSelectionG1 input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Code != Edition)
            throw new NotSupportedException("G1 only supports independently identified DM 17/01/2018 NTC2018, never EC5/EC3/2025.");
        if (!Enum.IsDefined(input.Situation) || !Enum.IsDefined(input.GammaColumn) ||
            string.IsNullOrWhiteSpace(input.SituationEvidence))
            throw new ArgumentException("An identified NTC design situation and its evidence are mandatory.");
        var a = input.Assignment ?? throw new ArgumentException("Timber member assignment is required.");
        if (string.IsNullOrWhiteSpace(a.MemberId) || string.IsNullOrWhiteSpace(a.SectionId) ||
            string.IsNullOrWhiteSpace(a.StrengthClassId) || string.IsNullOrWhiteSpace(a.AnalysisId) ||
            string.IsNullOrWhiteSpace(a.TargetId) || a.TargetKind != DesignEffectTargetKind.LoadCombination ||
            a.ServiceClass is not (1 or 2 or 3) || string.IsNullOrWhiteSpace(a.ServiceClassEvidence))
            throw new ArgumentException("Exact timber member, combination, material and evidenced service class required.");
        if (input.CompleteActionListConfirmed != true || string.IsNullOrWhiteSpace(input.CompletenessEvidence) ||
            string.IsNullOrWhiteSpace(input.FingerprintMatchEvidence) ||
            string.IsNullOrWhiteSpace(input.KmodApplicabilityEvidence) ||
            string.IsNullOrWhiteSpace(input.MaterialEvidence))
            throw new ArgumentException("Complete action list, independent matching and coefficient/material applicability evidence required.");
        var fp = Fingerprint(a.AnalysisId, a.TargetId, input.CombinationActions);
        if (string.IsNullOrWhiteSpace(input.ExpectedCombinationFingerprint) ||
            !string.Equals(fp, input.ExpectedCombinationFingerprint, StringComparison.Ordinal))
            throw new ArgumentException("NTC action manifest differs from the recorded FEM combination fingerprint.");
        var actions = input.CombinationActions.ToArray();
        var participating = actions.Where(x => x.CombinationFactor != 0).ToArray();
        if (participating.Length == 0)
            throw new ArgumentException("Zero-contribution combination has no governing duration.");
        bool hasExceptional = participating.Any(x => x.Category == Ntc2018ActionCategoryG1.Exceptional);
        if (input.Situation == Ntc2018TimberSituationG1.FundamentalStr && hasExceptional ||
            input.Situation == Ntc2018TimberSituationG1.Exceptional && !hasExceptional)
            throw new ArgumentException("NTC situation and exceptional actions disagree.");
        var timber = materials.FindTimber(a.StrengthClassId);
        if (timber.Kind is not (StrengthMaterialKindM88C.SolidSoftwood or StrengthMaterialKindM88C.HomogeneousGlulam))
            throw new NotSupportedException("NTC G1 timber: solid softwood / homogeneous glulam only.");
        var duration = participating.Max(x => x.Duration);
        if (a.LoadDuration is not null && a.LoadDuration != duration)
            throw new ArgumentException("Manually assigned duration is inconsistent with the complete manifest.");

        ValidateProduction(input);
        var kmod = tables.Kmod(timber.Kind, a.ServiceClass.Value, duration);
        var gamma = tables.Gamma(timber.Kind, input.Situation, input.GammaColumn);
        double? kcr = null;
        if (input.CircolareCrackRuleApplies == true)
        {
            if (string.IsNullOrWhiteSpace(input.CrackRuleApplicabilityEvidence) ||
                !double.IsFinite(timber.ShearStrengthMpa) || timber.ShearStrengthMpa <= 0)
                throw new ArgumentException("Circolare crack reduction requires sound shear strength and applicability evidence.");
            kcr = (timber.Kind == StrengthMaterialKindM88C.SolidSoftwood ? 2.0 : 2.5)
                / timber.ShearStrengthMpa; // fv,k has already been confirmed as MPa in catalog M88-C
            if (!double.IsFinite(kcr.Value) || kcr.Value <= 0 || kcr.Value > 1)
                throw new NotSupportedException("Computed kcr is outside documented (0,1] application range.");
        }
        else if (input.CircolareCrackRuleApplies != false ||
            string.IsNullOrWhiteSpace(input.CrackRuleApplicabilityEvidence))
            throw new ArgumentException("Explicit crack-reduction applicability decision and evidence required even when not applied.");

        var selected = a with
        {
            LoadDuration = duration,
            LoadDurationEvidence = $"NTC G1: {input.CompletenessEvidence}; fingerprint={fp}; " +
                $"governing={duration}; {input.KmodApplicabilityEvidence}"
        };
        var gammaBasis = input.Situation == Ntc2018TimberSituationG1.Exceptional
            ? "NTC exceptional situation (no A/B column)"
            : $"NTC fundamental STR, Column {input.GammaColumn}";
        var coeff = new List<NormativeCoefficient>
        {
            new(TimberRectangularSectionRuleM92A.KmodId, kmod,
                Ntc2018TimberCoefficientTablesG1.KmodClause,
                $"{timber.Kind}, service={a.ServiceClass}, duration={duration}; {input.KmodApplicabilityEvidence}; fp={fp}"),
            new(TimberRectangularSectionRuleM92A.GammaId, gamma,
                Ntc2018TimberCoefficientTablesG1.GammaClause,
                $"{gammaBasis}; {input.SituationEvidence}"),
        };
        if (kcr is double kc)
            coeff.Add(new NormativeCoefficient(TimberRectangularSectionRuleM92A.CrackId, kc,
                Ntc2018TimberCoefficientTablesG1.CrackClause,
                $"fv,k={timber.ShearStrengthMpa.ToString("G17", CultureInfo.InvariantCulture)} MPa; {input.CrackRuleApplicabilityEvidence}"));
        var profile = new NormativeProfile(
            $"NTC2018-G1-{a.AnalysisId}-{a.TargetId}-{a.MemberId}-{fp}",
            Edition, Array.AsReadOnly(coeff.ToArray()));
        var trace = $"NTC G1 COEFFICIENTS ONLY, not a rule or member approval. " +
            $"analysis={a.AnalysisId}, combo={a.TargetId}, member={a.MemberId}, section={a.SectionId}, " +
            $"grade={timber.Id}, material evidence={input.MaterialEvidence}, service={a.ServiceClass}, " +
            $"situation={input.Situation}, column={input.GammaColumn}, " +
            $"kmod={kmod.ToString("G17", CultureInfo.InvariantCulture)}, " +
            $"gamma_M={gamma.ToString("G17", CultureInfo.InvariantCulture)}, " +
            $"kcr={(kcr?.ToString("G17", CultureInfo.InvariantCulture) ?? "not selected")}, " +
            $"fingerprint={fp}, manifest={actions.Length} ({participating.Length} active). " +
            "The caller must independently establish true FEM combination fidelity, source certificates, " +
            "NTC design-situation applicability and all required design verifications.";
        return new Ntc2018TimberSelectedCoefficientsG1(profile, selected,
            input.Situation, input.GammaColumn, duration, kmod, gamma, kcr, fp,
            Array.AsReadOnly(actions), trace);
    }

    private static void ValidateProduction(Ntc2018TimberSelectionG1 input)
    {
        if (input.Situation == Ntc2018TimberSituationG1.Exceptional)
        {
            if (input.GammaColumn != Ntc2018GammaColumnG1.NotApplicableExceptional ||
                input.ColumnBProduction is not null)
                throw new ArgumentException("Exceptional situation gamma_M=1; no Column A/B or Column B production evidence.");
            return;
        }
        if (input.GammaColumn == Ntc2018GammaColumnG1.A)
        {
            if (input.ColumnBProduction is not null)
                throw new ArgumentException("Column A and Column B production evidence are incompatible.");
            return;
        }
        if (input.GammaColumn != Ntc2018GammaColumnG1.B)
            throw new ArgumentException("Fundamental STR requires Column A or B.");
        var p = input.ColumnBProduction;
        if (p is null || p.ContinuousProductionAndControlConfirmed != true ||
            p.Ntc11_7QualitySystemConfirmed != true ||
            p.ApprovedProductDocumentationConfirmed != true ||
            string.IsNullOrWhiteSpace(p.ProductCertificationReference) ||
            string.IsNullOrWhiteSpace(p.ProductionControlReference) ||
            string.IsNullOrWhiteSpace(p.CoefficientOfVariationEvidence) ||
            p.MeasuredResistanceCoefficientOfVariation is not double cov ||
            !double.IsFinite(cov) || cov < 0 || cov > 0.15)
            throw new ArgumentException("NTC 2018 Column B demands documented controlled continuous production, " +
                "certification/quality requirements and resistance coefficient of variation <=15%.");
    }
}
