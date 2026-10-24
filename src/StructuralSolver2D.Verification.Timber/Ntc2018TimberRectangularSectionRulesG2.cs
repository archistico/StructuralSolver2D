using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>
/// G2 NTC2018 check scope: ONE pure local action on ONE constant rectangular section at
/// ONE sourced FEM station. Not a combined action or member/global compliance check.
/// </summary>
public enum Ntc2018TimberPureEffectG2
{
    TensionParallel,
    CompressionParallel,
    StrongAxisBending,
    InPlaneShear
}

/// <summary>
/// Every evidence statement describes externally checked physical facts, not deductions
/// from the 2D FEM. Net area is mandatory for tension and must include all relevant reductions.
/// No size-strength enhancement (kh) is applied, deliberately conservatively.
/// </summary>
public sealed record Ntc2018TimberSectionEvidenceG2(
    Ntc2018TimberSelectedCoefficientsG1 Selection,
    string SectionAndMaterialEvidence,
    bool? GrainParallelConfirmed,
    bool? StrongAxisMappingConfirmed,
    bool? PrismaticRectangularBaseSectionConfirmed,
    bool? UnweakenedGrossSectionConfirmed,
    bool? NoBeneficialSizeFactorConfirmed,
    double? VerifiedNetTensionAreaMm2,
    string? NetTensionAreaEvidence,
    bool? ShearDirectionConfirmed,
    string? ShearEvidence);

/// <summary>
/// NTC 2018 section-only checks: 4.4.6, 4.4.8.1.1, .1.3, .1.6, .1.9;
/// Circolare 7/2019 C4.4.8.1.9 for kcr. Not an EC5 rule re-labelled as NTC.
/// No compression buckling, flexural lateral stability, interaction, connections,
/// perpendicular-grain effects, notches, bearing, SLE, or global/member approval.
/// </summary>
public sealed class Ntc2018TimberRectangularSectionRuleG2 : IDesignCheckRule
{
    public const string TensionId = "NTC2018-TIMBER-RECT-PURE-TENSION-G2";
    public const string CompressionId = "NTC2018-TIMBER-RECT-PURE-COMPRESSION-G2";
    public const string BendingId = "NTC2018-TIMBER-RECT-PURE-BENDING-G2";
    public const string ShearId = "NTC2018-TIMBER-RECT-PURE-SHEAR-G2";

    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly Ntc2018TimberCoefficientTablesG1 tables;
    private readonly IReadOnlyDictionary<string, Ntc2018TimberSectionEvidenceG2> evidence;
    public Ntc2018TimberPureEffectG2 Effect { get; }
    public DesignCodeEdition Code => Ntc2018TimberCoefficientSelectorG1.Edition;
    public DesignMaterial Material => DesignMaterial.Timber;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public bool EstablishesCompleteMemberCompliance => false;

    public Ntc2018TimberRectangularSectionRuleG2(Ntc2018TimberPureEffectG2 effect,
        CharacteristicMaterialCatalogM88C materials,
        Ntc2018TimberCoefficientTablesG1 tables,
        IEnumerable<Ntc2018TimberSectionEvidenceG2> declarations)
    {
        if (!Enum.IsDefined(effect)) throw new ArgumentOutOfRangeException(nameof(effect));
        Effect = effect;
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        this.tables = tables ?? throw new ArgumentNullException(nameof(tables));
        ArgumentNullException.ThrowIfNull(declarations);
        var dictionary = new Dictionary<string, Ntc2018TimberSectionEvidenceG2>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in declarations)
        {
            var selection = item?.Selection;
            var a = selection?.Assignment;
            if (a is null || selection!.Profile.Code != Code ||
                string.IsNullOrWhiteSpace(a.MemberId) || string.IsNullOrWhiteSpace(a.SectionId) ||
                string.IsNullOrWhiteSpace(a.AnalysisId) || string.IsNullOrWhiteSpace(a.TargetId) ||
                a.TargetKind != DesignEffectTargetKind.LoadCombination)
                throw new ArgumentException("G2 requires genuine G1 NTC2018 selection and an exact load-combination target.", nameof(declarations));
            if (!dictionary.TryAdd(Key(a.MemberId, a.AnalysisId, a.TargetId), item!))
                throw new ArgumentException("Duplicate G2 section evidence for member/analysis/combination.", nameof(declarations));
        }
        evidence = dictionary;
    }

    public string RuleId => Effect switch
    {
        Ntc2018TimberPureEffectG2.TensionParallel => TensionId,
        Ntc2018TimberPureEffectG2.CompressionParallel => CompressionId,
        Ntc2018TimberPureEffectG2.StrongAxisBending => BendingId,
        Ntc2018TimberPureEffectG2.InPlaneShear => ShearId,
        _ => throw new InvalidOperationException("Unknown G2 effect.")
    };

    public string SourceClause => Effect switch
    {
        Ntc2018TimberPureEffectG2.TensionParallel => "NTC 2018, 4.4.6, 4.4.8.1.1 [4.4.2] (net area; kh=1 conservative)",
        Ntc2018TimberPureEffectG2.CompressionParallel => "NTC 2018, 4.4.6, 4.4.8.1.3 [4.4.3] (no stability claim)",
        Ntc2018TimberPureEffectG2.StrongAxisBending => "NTC 2018, 4.4.6, 4.4.8.1.6 [4.4.5a,b] (single-axis; kh=1)",
        Ntc2018TimberPureEffectG2.InPlaneShear => "NTC 2018, 4.4.8.1.9 [4.4.8]; Circolare 7/2019 C4.4.8.1.9",
        _ => throw new InvalidOperationException("Unknown G2 effect.")
    };

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Member is null || request.Effects is null || request.Profile is null)
            return DesignRuleEvaluation.MissingData("Member, coefficients and FEM snapshot required.");
        if (request.Member.Material != DesignMaterial.Timber || request.LimitState != DesignLimitState.Ultimate)
            return DesignRuleEvaluation.NotApplicable("G2 only supports timber ULS.");
        if (request.Profile.Code != Code)
            return DesignRuleEvaluation.NotApplicable("G2 is NTC 2018 ONLY, not an EC5/annex design rule.");
        var e = request.Effects;
        if (string.IsNullOrWhiteSpace(e.AnalysisId) || string.IsNullOrWhiteSpace(e.CombinationId) ||
            !evidence.TryGetValue(Key(request.Member.MemberId, e.AnalysisId, e.CombinationId), out var proof))
            return DesignRuleEvaluation.MissingData("No exact G1 selection and independent G2 section evidence for this member/combination.");
        var s = proof.Selection;
        var a = s.Assignment;
        if (!string.Equals(e.MemberId, a.MemberId, StringComparison.OrdinalIgnoreCase) ||
            e.TargetKind != DesignEffectTargetKind.LoadCombination || e.TargetKind != a.TargetKind ||
            e.SectionSide is null ||
            !string.Equals(request.Member.SectionId, a.SectionId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.AnalysisId, a.AnalysisId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.CombinationId, a.TargetId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("FEM member/section/combination/section-side provenance is incomplete or inconsistent.");
        if (!MatchingG1Profile(request.Profile, s) || s.Assignment.LoadDuration != s.GoverningDuration)
            return DesignRuleEvaluation.MissingData("G1 coefficient identity, values or duration do not match selected NTC2018 scenario.");
        if (string.IsNullOrWhiteSpace(s.CombinationFingerprint) ||
            s.CombinationFingerprint != Ntc2018TimberCoefficientSelectorG1.Fingerprint(a.AnalysisId, a.TargetId, s.Manifest))
            return DesignRuleEvaluation.MissingData("G1 action manifest fingerprint is inconsistent with its declared combination.");
        if (!double.IsFinite(a.WidthMm) || !double.IsFinite(a.DepthMm) ||
            a.WidthMm <= 0 || a.DepthMm <= 0 || string.IsNullOrWhiteSpace(a.GeometryEvidence) ||
            string.IsNullOrWhiteSpace(proof.SectionAndMaterialEvidence) ||
            string.IsNullOrWhiteSpace(a.ServiceClassEvidence) ||
            string.IsNullOrWhiteSpace(a.LoadDurationEvidence) ||
            a.ServiceClass is not (1 or 2 or 3) ||
            proof.GrainParallelConfirmed != true || proof.StrongAxisMappingConfirmed != true ||
            proof.PrismaticRectangularBaseSectionConfirmed != true)
            return DesignRuleEvaluation.MissingData("Fully evidenced intact rectangular geometry, grain, axes, service and duration are required.");
        if (request.Effects.AxialForceKn is not double n ||
            request.Effects.ShearForceYKn is not double v ||
            request.Effects.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("All signed, concomitant N/V/M values are required, including zeros.");

        bool pure = Effect switch
        {
            Ntc2018TimberPureEffectG2.TensionParallel or Ntc2018TimberPureEffectG2.CompressionParallel => v == 0 && m == 0,
            Ntc2018TimberPureEffectG2.StrongAxisBending => n == 0 && v == 0,
            Ntc2018TimberPureEffectG2.InPlaneShear => n == 0 && m == 0,
            _ => false
        };
        if (!pure)
            return DesignRuleEvaluation.NotApplicable("Simultaneous N/V/M cannot be assessed with a pure G2 rule; interactions are future G3.");
        if (Effect == Ntc2018TimberPureEffectG2.TensionParallel && n <= 0 ||
            Effect == Ntc2018TimberPureEffectG2.CompressionParallel && n >= 0 ||
            Effect == Ntc2018TimberPureEffectG2.StrongAxisBending && m == 0 ||
            Effect == Ntc2018TimberPureEffectG2.InPlaneShear && v == 0)
            return DesignRuleEvaluation.NotApplicable("Selected pure action does not occur at this station.");

        TimberStrengthClassM88C grade;
        try { grade = materials.FindTimber(a.StrengthClassId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Uncatalogued timber strength class."); }
        if (grade.Kind is not (StrengthMaterialKindM88C.SolidSoftwood or StrengthMaterialKindM88C.HomogeneousGlulam))
            return DesignRuleEvaluation.NotApplicable("Only solid softwood and homogeneous glulam supported in G2.");
        // Guard against forged/mixed selected coefficient records: check the original NTC tables,
        // independently of the G1 profile object supplied to this design rule.
        if (s.Kmod != tables.Kmod(grade.Kind, a.ServiceClass!.Value, s.GoverningDuration) ||
            s.GammaM != tables.Gamma(grade.Kind, s.Situation, s.GammaColumn) ||
            (s.Kcr.HasValue && (s.Kcr.Value !=
                (grade.Kind == StrengthMaterialKindM88C.SolidSoftwood ? 2.0 : 2.5) / grade.ShearStrengthMpa)))
            return DesignRuleEvaluation.MissingData("G1 selected coefficients differ from NTC source tables or Circolare kcr.");
        var area = a.WidthMm * a.DepthMm;
        var w = a.WidthMm * a.DepthMm * a.DepthMm / 6.0;
        if (!double.IsFinite(area) || !double.IsFinite(w) || area <= 0 || w <= 0 ||
            !double.IsFinite(s.Kmod) || s.Kmod <= 0 || !double.IsFinite(s.GammaM) || s.GammaM <= 0)
            return DesignRuleEvaluation.MissingData("Invalid geometry or NTC design factors.");
        if ((Effect is Ntc2018TimberPureEffectG2.TensionParallel or Ntc2018TimberPureEffectG2.StrongAxisBending) &&
            proof.NoBeneficialSizeFactorConfirmed != true)
            return DesignRuleEvaluation.MissingData("Conservative kh=1 (no beneficial size effect) must be explicitly confirmed.");
        if (Effect != Ntc2018TimberPureEffectG2.TensionParallel &&
            proof.UnweakenedGrossSectionConfirmed != true)
            return DesignRuleEvaluation.MissingData("Compression/bending/shear require intact gross rectangular section; holes/notches require different methods.");
        double tensionArea = area;
        if (Effect == Ntc2018TimberPureEffectG2.TensionParallel)
        {
            if (proof.VerifiedNetTensionAreaMm2 is not double net ||
                !double.IsFinite(net) || net <= 0 || net > area ||
                string.IsNullOrWhiteSpace(proof.NetTensionAreaEvidence))
                return DesignRuleEvaluation.MissingData("Verified positive net tensile area <= gross area and source evidence are mandatory.");
            tensionArea = net;
        }
        if (Effect == Ntc2018TimberPureEffectG2.InPlaneShear &&
            (proof.ShearDirectionConfirmed != true || string.IsNullOrWhiteSpace(proof.ShearEvidence) ||
             s.Kcr is not double kcr || !double.IsFinite(kcr) || kcr <= 0 || kcr > 1))
            return DesignRuleEvaluation.MissingData("NTC Circolare kcr and shear direction with source evidence are required.");

        var fk = Effect switch
        {
            Ntc2018TimberPureEffectG2.TensionParallel => grade.TensionParallelMpa,
            Ntc2018TimberPureEffectG2.CompressionParallel => grade.CompressionParallelMpa,
            Ntc2018TimberPureEffectG2.StrongAxisBending => grade.BendingStrengthMpa,
            Ntc2018TimberPureEffectG2.InPlaneShear => grade.ShearStrengthMpa,
            _ => double.NaN
        };
        var fd = s.Kmod * fk / s.GammaM;
        var stress = Effect switch
        {
            Ntc2018TimberPureEffectG2.TensionParallel => n * 1000.0 / tensionArea,
            Ntc2018TimberPureEffectG2.CompressionParallel => -n * 1000.0 / area,
            Ntc2018TimberPureEffectG2.StrongAxisBending => Math.Abs(m) * 1e6 / w,
            Ntc2018TimberPureEffectG2.InPlaneShear => 1.5 * Math.Abs(v) * 1000.0 / (s.Kcr!.Value * area),
            _ => double.NaN
        };
        if (!double.IsFinite(fd) || fd <= 0 || !double.IsFinite(stress) || stress < 0)
            return DesignRuleEvaluation.MissingData("Non-finite/invalid NTC stress or capacity.");

        static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
        var trace = $"G2 NTC2018 PURE LOCAL SECTION ONLY effect={Effect}; source={SourceClause}; " +
            $"member={a.MemberId}; section={a.SectionId}; material={grade.Id} ({grade.ReferenceStandard}; {grade.DataStatus}); " +
            $"b={F(a.WidthMm)} mm; h={F(a.DepthMm)} mm; A_gross={F(area)} mm2; A_net_tension={F(tensionArea)} mm2; W={F(w)} mm3; " +
            $"N={F(n)} kN; V={F(v)} kN; M={F(m)} kNm; " +
            $"fk={F(fk)} MPa; kmod={F(s.Kmod)}; gamma_M={F(s.GammaM)}; " +
            $"kcr={(s.Kcr.HasValue ? F(s.Kcr.Value) : "not applied")}; kh=1 no enhancement; " +
            $"stress={F(stress)} MPa; fd={F(fd)} MPa; " +
            $"analysis={e.AnalysisId}; combination={e.CombinationId}; fingerprint={s.CombinationFingerprint}; " +
            $"station={F(e.PositionMetres)} m; side={e.SectionSide}; situation={s.Situation}; column={s.GammaColumn}; " +
            $"geometry={a.GeometryEvidence}; scope={proof.SectionAndMaterialEvidence}; " +
            $"net={proof.NetTensionAreaEvidence ?? "not relevant"}; shear={proof.ShearEvidence ?? "not relevant"}. " +
            "G1 inputs are externally evidenced and are not independently certified by G2. " +
            "NOT VERIFIED: N-M/V-M interactions; compression buckling; lateral-torsional buckling; " +
            "perpendicular-grain/bearing; holes other than certified net tension area; joints, notches, fire, SLE, complete combinations, overall member/code compliance.";
        return DesignRuleEvaluation.Calculated(stress, fd, "MPa", trace);
    }

    private static bool MatchingG1Profile(NormativeProfile actual, Ntc2018TimberSelectedCoefficientsG1 selection)
    {
        var expected = selection.Profile;
        if (actual.Code != Ntc2018TimberCoefficientSelectorG1.Edition || actual.Code != expected.Code ||
            !string.Equals(actual.ProfileId, expected.ProfileId, StringComparison.Ordinal) ||
            actual.Coefficients is null || expected.Coefficients is null ||
            actual.Coefficients.Count != expected.Coefficients.Count)
            return false;
        foreach (var c in expected.Coefficients)
        {
            var matches = actual.Coefficients.Where(x => x is not null &&
                string.Equals(x.Id, c.Id, StringComparison.Ordinal)).ToArray();
            if (matches.Length != 1 || matches[0] != c) return false;
        }
        return expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.KmodId && c.Value == selection.Kmod) &&
               expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.GammaId && c.Value == selection.GammaM) &&
               (!selection.Kcr.HasValue || expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.CrackId && c.Value == selection.Kcr.Value));
    }

    private static string Key(string member, string analysis, string target) =>
        $"{member.ToUpperInvariant()}\u001f{analysis.ToUpperInvariant()}\u001f{target.ToUpperInvariant()}";
}
