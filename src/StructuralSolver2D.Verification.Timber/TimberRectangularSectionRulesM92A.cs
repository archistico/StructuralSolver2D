using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Timber;

public enum TimberLoadDurationM92A { Permanent, LongTerm, MediumTerm, ShortTerm, Instantaneous }
public enum TimberPureEffectM92A { TensionParallel, CompressionParallel, StrongAxisBending, InPlaneShear }

/// <summary>
/// Explicit engineering evidence; NO inference of moisture, duration, grain, geometry, or
/// restraints from a 2-D FEM model. A member ID identifies ONE constant rectangular section.
/// Service class and duration must be independently established for each applicable combination.
/// </summary>
public sealed record TimberRectangularAssignmentM92A(
    string MemberId, string SectionId, string StrengthClassId,
    double WidthMm, double DepthMm, string GeometryEvidence,
    int? ServiceClass, TimberLoadDurationM92A? LoadDuration,
    string? ServiceClassEvidence, string? LoadDurationEvidence,
    string AnalysisId, string TargetId, DesignEffectTargetKind TargetKind,
    bool? GrainParallelConfirmed, bool? StrongAxisMappingConfirmed,
    bool? UnweakenedNetSectionConfirmed, bool? NoStrengthEnhancementClaimed,
    bool? ShearDirectionConfirmed = null);

/// <summary>
/// EN 1995-1-1:2004+A1:2008+A2:2014 (first generation) §2.4.1 and §6.1.2/6.1.4/6.1.6/6.1.7.
/// Checks ONLY a single, pure local action at one prismatic rectangular cross section, without
/// member stability, N-M interaction, perpendicular-grain, fasteners, notches or full compliance.
/// kmod and gamma_M MUST be provided and documented for the selected situation/annex.
/// For shear kcr must also be provided (no silent 0.67 assumption).
/// No strength enhancement (kh, kl etc.) is used. Non-applicable conditions are fail closed.
/// </summary>
public sealed class TimberRectangularSectionRuleM92A : IDesignCheckRule
{
    public const string Edition = "2004+A1:2008+A2:2014";
    public const string KmodId = "kmod";
    public const string GammaId = "gamma_M";
    public const string CrackId = "kcr";
    public const string TensionId = "EC5-2004-TIMBER-RECT-PURE-TENSION-M92A";
    public const string CompressionId = "EC5-2004-TIMBER-RECT-PURE-COMPRESSION-M92A";
    public const string BendingId = "EC5-2004-TIMBER-RECT-PURE-BENDING-M92A";
    public const string ShearId = "EC5-2004-TIMBER-RECT-PURE-SHEAR-M92A";

    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, TimberRectangularAssignmentM92A> assignments;
    public TimberPureEffectM92A Effect { get; }
    public DesignCodeEdition Code { get; }

    public TimberRectangularSectionRuleM92A(
        DesignCodeEdition code,
        TimberPureEffectM92A effect,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<TimberRectangularAssignmentM92A> assignments)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.DocumentId != "EN 1995-1-1" || code.EditionId != Edition ||
            string.IsNullOrWhiteSpace(code.NationalAnnexId))
            throw new NotSupportedException("M92-A implements only first-generation EC5 with an explicit annex ID, not EN 1995-1-1:2025 or NTC.");
        if (!Enum.IsDefined(effect)) throw new ArgumentOutOfRangeException(nameof(effect));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(assignments);
        var map = new Dictionary<string, TimberRectangularAssignmentM92A>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in assignments)
        {
            if (a is null || string.IsNullOrWhiteSpace(a.MemberId) ||
                string.IsNullOrWhiteSpace(a.SectionId) || string.IsNullOrWhiteSpace(a.StrengthClassId) ||
                string.IsNullOrWhiteSpace(a.AnalysisId) || string.IsNullOrWhiteSpace(a.TargetId) ||
                !Enum.IsDefined(a.TargetKind))
                throw new ArgumentException("Timber assignment must identify a member, section and strength class.", nameof(assignments));
            if (!map.TryAdd(AssignmentKey(a.MemberId, a.AnalysisId, a.TargetId), a))
                throw new ArgumentException("Duplicate timber assignment for member and analysis target.", nameof(assignments));
        }
        this.assignments = map;
        Code = code;
        Effect = effect;
    }

    public string RuleId => Effect switch
    {
        TimberPureEffectM92A.TensionParallel => TensionId,
        TimberPureEffectM92A.CompressionParallel => CompressionId,
        TimberPureEffectM92A.StrongAxisBending => BendingId,
        TimberPureEffectM92A.InPlaneShear => ShearId,
        _ => throw new InvalidOperationException("Unknown timber effect.")
    };
    public string SourceClause => Effect switch
    {
        TimberPureEffectM92A.TensionParallel => "EN 1995-1-1:2004+A1:2008+A2:2014, 6.1.2, Eq. 6.1; 2.4.1",
        TimberPureEffectM92A.CompressionParallel => "EN 1995-1-1:2004+A1:2008+A2:2014, 6.1.4, Eq. 6.2; 2.4.1",
        TimberPureEffectM92A.StrongAxisBending => "EN 1995-1-1:2004+A1:2008+A2:2014, 6.1.6, Eq. 6.11-6.12; 2.4.1",
        TimberPureEffectM92A.InPlaneShear => "EN 1995-1-1:2004+A1:2008+A2:2014, 6.1.7, Eq. 6.13-6.13a; 2.4.1",
        _ => throw new InvalidOperationException("Unknown timber effect.")
    };
    public DesignMaterial Material => DesignMaterial.Timber;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public bool EstablishesCompleteMemberCompliance => false;

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Member is null || request.Effects is null || request.Profile is null ||
            string.IsNullOrWhiteSpace(request.Effects.AnalysisId) ||
            string.IsNullOrWhiteSpace(request.Effects.CombinationId))
            return DesignRuleEvaluation.MissingData("Member, effect snapshot and normative profile are required.");
        if (request.Member.Material != DesignMaterial.Timber || request.LimitState != DesignLimitState.Ultimate)
            return DesignRuleEvaluation.NotApplicable("Only timber ULS checks are implemented.");
        if (request.Profile.Code != Code)
            return DesignRuleEvaluation.NotApplicable("EC5 edition or national annex mismatch.");
        if (!assignments.TryGetValue(AssignmentKey(request.Member.MemberId, request.Effects.AnalysisId, request.Effects.CombinationId), out var a))
            return DesignRuleEvaluation.MissingData("No documented member-to-timber assignment.");
        if (request.Effects.TargetKind != a.TargetKind ||
            request.Effects.MemberId is null ||
            !string.Equals(request.Effects.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            request.Effects.SectionSide is null)
            return DesignRuleEvaluation.MissingData("Exact FEM target kind, member and section-side provenance required.");
        if (!string.Equals(a.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Assigned section differs from FEM member section.");
        if (!double.IsFinite(a.WidthMm) || a.WidthMm <= 0 ||
            !double.IsFinite(a.DepthMm) || a.DepthMm <= 0 ||
            string.IsNullOrWhiteSpace(a.GeometryEvidence))
            return DesignRuleEvaluation.MissingData("Valid rectangle width/depth in mm and source evidence required.");
        if (a.GrainParallelConfirmed != true || a.StrongAxisMappingConfirmed != true ||
            a.UnweakenedNetSectionConfirmed != true || a.NoStrengthEnhancementClaimed != true)
            return DesignRuleEvaluation.MissingData("Grain, local strong axis, unweakened section and no beneficial size factor must be confirmed.");
        if (a.ServiceClass is not (1 or 2 or 3) ||
            a.LoadDuration is null || !Enum.IsDefined(a.LoadDuration.Value) ||
            string.IsNullOrWhiteSpace(a.ServiceClassEvidence) || string.IsNullOrWhiteSpace(a.LoadDurationEvidence))
            return DesignRuleEvaluation.MissingData("Supported service class 1-3, load duration and independent evidence are mandatory.");
        if (request.Effects.AxialForceKn is not double n ||
            request.Effects.ShearForceYKn is not double v ||
            request.Effects.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Finite concomitant N, V and M are mandatory, even in a pure-action test.");
        if (request.Effects.MemberId is not null &&
            !string.Equals(request.Effects.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("FEM effect provenance does not match member ID.");

        bool pure = Effect switch
        {
            TimberPureEffectM92A.TensionParallel or TimberPureEffectM92A.CompressionParallel => v == 0 && m == 0,
            TimberPureEffectM92A.StrongAxisBending => n == 0 && v == 0,
            TimberPureEffectM92A.InPlaneShear => n == 0 && m == 0,
            _ => false
        };
        if (!pure) return DesignRuleEvaluation.NotApplicable("Combined N/V/M requires M93 interaction checks, not implemented by M92-A.");
        if (Effect == TimberPureEffectM92A.TensionParallel && n <= 0 ||
            Effect == TimberPureEffectM92A.CompressionParallel && n >= 0 ||
            Effect == TimberPureEffectM92A.StrongAxisBending && m == 0 ||
            Effect == TimberPureEffectM92A.InPlaneShear && v == 0)
            return DesignRuleEvaluation.NotApplicable("No nonzero pure effect of the selected type/sign at this section.");

        var kmod = GetCoefficient(request.Profile, KmodId);
        var gamma = GetCoefficient(request.Profile, GammaId);
        if (kmod is null || gamma is null || kmod.Value <= 0 || kmod.Value > 2 || gamma.Value <= 0)
            return DesignRuleEvaluation.MissingData("Positive, explicitly sourced kmod (0,2] and gamma_M required; no default coefficients.");
        NormativeCoefficient? kcr = null;
        if (Effect == TimberPureEffectM92A.InPlaneShear)
        {
            kcr = GetCoefficient(request.Profile, CrackId);
            if (a.ShearDirectionConfirmed != true || kcr is null || kcr.Value <= 0 || kcr.Value > 1)
                return DesignRuleEvaluation.MissingData("Shear direction and explicit sourced crack-width coefficient kcr in (0,1] are required.");
        }

        TimberStrengthClassM88C timber;
        try { timber = materials.FindTimber(a.StrengthClassId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown timber strength class in M88-C."); }
        if (timber.Kind is not (StrengthMaterialKindM88C.SolidSoftwood or StrengthMaterialKindM88C.HomogeneousGlulam))
            return DesignRuleEvaluation.NotApplicable("Only solid softwood and homogeneous glulam in M88-C are supported.");

        double fk = Effect switch
        {
            TimberPureEffectM92A.TensionParallel => timber.TensionParallelMpa,
            TimberPureEffectM92A.CompressionParallel => timber.CompressionParallelMpa,
            TimberPureEffectM92A.StrongAxisBending => timber.BendingStrengthMpa,
            TimberPureEffectM92A.InPlaneShear => timber.ShearStrengthMpa,
            _ => double.NaN
        };
        double fd = kmod.Value * fk / gamma.Value; // MPa
        double area = a.WidthMm * a.DepthMm; // mm2
        double w = a.WidthMm * a.DepthMm * a.DepthMm / 6.0; // mm3
        double demandMpa = Effect switch
        {
            TimberPureEffectM92A.TensionParallel or TimberPureEffectM92A.CompressionParallel => Math.Abs(n) * 1000.0 / area,
            TimberPureEffectM92A.StrongAxisBending => Math.Abs(m) * 1e6 / w,
            TimberPureEffectM92A.InPlaneShear => 1.5 * Math.Abs(v) * 1000.0 / (kcr!.Value * area),
            _ => double.NaN
        };
        if (!double.IsFinite(area) || area <= 0 || !double.IsFinite(w) || w <= 0 ||
            !double.IsFinite(demandMpa) || !double.IsFinite(fd) || fd <= 0)
            return DesignRuleEvaluation.MissingData("Geometry or design stress is non-finite or outside numeric range.");
        static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
        var audit = $"M92-A SINGLE PURE SECTION EFFECT {Effect} | member={request.Member.MemberId}; section={a.SectionId}; " +
            $"grade={timber.Id} ({timber.ReferenceStandard}; {timber.SourceUrl}; {timber.DataStatus}); " +
            $"b={F(a.WidthMm)} mm; h={F(a.DepthMm)} mm; A={F(area)} mm2; W={F(w)} mm3; geometry={a.GeometryEvidence}; " +
            $"service={a.ServiceClass} ({a.ServiceClassEvidence}); duration={a.LoadDuration} ({a.LoadDurationEvidence}); " +
            $"kmod={F(kmod.Value)} ({kmod.SourceClause}); gamma_M={F(gamma.Value)} ({gamma.SourceClause}); " +
            (kcr is not null ? $"kcr={F(kcr.Value)} ({kcr.SourceClause}); " : "") +
            $"N={F(n)} kN V={F(v)} kN M={F(m)} kNm; fk={F(fk)} MPa; fd={F(fd)} MPa; " +
            $"stress={F(demandMpa)} MPa; analysis={request.Effects.AnalysisId}; " +
            $"target={request.Effects.CombinationId}; x={F(request.Effects.PositionMetres)} m; " +
            $"edition={Code.EditionId}; annex={Code.NationalAnnexId}; " +
            "NOT checked: combined actions, buckling, lateral stability, perpendicular-grain stresses, notches, holes, joints, fire, SLE, NTC or overall member compliance.";
        return DesignRuleEvaluation.Calculated(demandMpa, fd, "MPa", audit);
    }

    private static string AssignmentKey(string member, string analysis, string target) =>
        $"{member.ToUpperInvariant()}\u001f{analysis.ToUpperInvariant()}\u001f{target.ToUpperInvariant()}";

    private static NormativeCoefficient? GetCoefficient(NormativeProfile p, string id) =>
        p.Coefficients?.SingleOrDefault(c =>
            c is not null && string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase) &&
            double.IsFinite(c.Value) && !string.IsNullOrWhiteSpace(c.SourceClause));
}
