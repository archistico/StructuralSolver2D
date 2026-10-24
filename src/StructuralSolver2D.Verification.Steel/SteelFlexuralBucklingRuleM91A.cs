using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Designer-provided buckling assumptions for ONE uniform, purely compressed rolled I/H member.
/// Effective buckling lengths Lcr,y and Lcr,z are NOT identical to geometrical unbraced lengths
/// and are NEVER inferred from a planar FEM model. All quantities and evidence are mandatory.
/// </summary>
public sealed record SteelFlexuralBucklingAssignmentM91A(
    string MemberId,
    string SectionId,
    string SteelGrade,
    int? CompressionSectionClass,
    string? ClassificationEvidence,
    double? EffectiveLengthStrongAxisMetres,
    string? StrongAxisLengthEvidence,
    double? EffectiveLengthWeakAxisMetres,
    string? WeakAxisLengthEvidence,
    double? YoungModulusMpa,
    string? YoungModulusEvidence,
    bool? StrongWeakAxisMappingConfirmed,
    bool? UniformPrismaticMemberConfirmed,
    bool? UniformConcentricCompressionConfirmed,
    bool? NoRelevantHolesConfirmed);

/// <summary>
/// One-axis EC3 2005+A1:2014 flexural buckling computation, units: MPa, cm2, cm4, m, kN.
/// Curve selection uses EN 1993-1-1:2005 Table 6.2 (hot-rolled I/H, S235/S275/S355).
/// </summary>
public sealed record SteelFlexuralBucklingAxisM91A(
    string Axis,
    string Curve,
    double ImperfectionFactor,
    double EffectiveLengthMetres,
    double EulerCriticalForceKn,
    double NonDimensionalSlenderness,
    double ReductionFactor,
    double BucklingResistanceKn);

/// <summary>
/// EN 1993-1-1:2005+A1:2014, §6.3.1.1-6.3.1.3, Eqs (6.46),(6.47),(6.49),(6.50).
/// ONLY flexural buckling about BOTH principal axes for rolled parallel-flange IPE/HEA/HEB
/// (section class 1-3) under PURE, UNIFORM compression. Buckling-curve selection uses Table 6.2.
/// Torsional/flexural-torsional modes, member N-M interaction, initial eccentricities, local
/// class 4 effective properties, braces/connections and entire-member code compliance are NOT checked.
/// </summary>
public sealed class SteelFlexuralBucklingRuleM91A : IDesignCheckRule
{
    public const string Id = "EC3-2005-PURE-COMPRESSION-BIAXIAL-FLEXURAL-BUCKLING-M91A";
    public const string GammaId = "gamma_M1";
    public const string Clause = "EN 1993-1-1:2005+A1:2014, §6.3.1.1-6.3.1.3, Eqs (6.46),(6.47),(6.49),(6.50), Tables 6.1-6.2";

    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, SteelFlexuralBucklingAssignmentM91A> assignments;

    public SteelFlexuralBucklingRuleM91A(
        DesignCodeEdition code,
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<SteelFlexuralBucklingAssignmentM91A> assignments)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.DocumentId != "EN 1993-1-1" || code.EditionId != "2005+A1:2014" ||
            string.IsNullOrWhiteSpace(code.NationalAnnexId))
            throw new NotSupportedException("M91-A is limited to EN 1993-1-1:2005+A1:2014 with explicit National Annex ID.");
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(assignments);
        var map = new Dictionary<string, SteelFlexuralBucklingAssignmentM91A>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in assignments)
        {
            if (a is null || string.IsNullOrWhiteSpace(a.MemberId) ||
                string.IsNullOrWhiteSpace(a.SectionId) || string.IsNullOrWhiteSpace(a.SteelGrade))
                throw new ArgumentException("Invalid steel flexural buckling member assignment.", nameof(assignments));
            if (!map.TryAdd(a.MemberId, a))
                throw new ArgumentException("Duplicate M91-A member assignment.", nameof(assignments));
        }
        this.assignments = map;
        Code = code;
    }

    public string RuleId => Id;
    public DesignCodeEdition Code { get; }
    public DesignMaterial Material => DesignMaterial.Steel;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public string SourceClause => Clause;
    public bool EstablishesCompleteMemberCompliance => false;

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!assignments.TryGetValue(request.Member.MemberId, out var a))
            return DesignRuleEvaluation.MissingData("M91-A assignment missing for member.");
        if (!string.Equals(request.Member.SectionId, a.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Assignment and FEM section do not match.");
        if (a.CompressionSectionClass is null || string.IsNullOrWhiteSpace(a.ClassificationEvidence))
            return DesignRuleEvaluation.MissingData("Documented classification under uniform compression is required.");
        if (a.CompressionSectionClass is < 1 or > 3)
            return DesignRuleEvaluation.NotApplicable("Only gross-section classes 1-3; class 4 needs effective area.");
        if (a.StrongWeakAxisMappingConfirmed != true || a.UniformPrismaticMemberConfirmed != true ||
            a.UniformConcentricCompressionConfirmed != true || a.NoRelevantHolesConfirmed != true)
            return DesignRuleEvaluation.MissingData("Confirm section axes, prismatic geometry, uniform concentric compression and gross section.");
        if (a.EffectiveLengthStrongAxisMetres is not double ly || !ValidPositive(ly) ||
            a.EffectiveLengthWeakAxisMetres is not double lz || !ValidPositive(lz) ||
            string.IsNullOrWhiteSpace(a.StrongAxisLengthEvidence) ||
            string.IsNullOrWhiteSpace(a.WeakAxisLengthEvidence))
            return DesignRuleEvaluation.MissingData("Explicit justified effective buckling lengths Lcr,y and Lcr,z are both required; never infer from FEM.");
        if (a.YoungModulusMpa is not double eModulus || !ValidPositive(eModulus) ||
            string.IsNullOrWhiteSpace(a.YoungModulusEvidence))
            return DesignRuleEvaluation.MissingData("Explicit finite E [MPa] and source required.");

        var effects = request.Effects;
        if (effects.Origin != DesignEffectOrigin.FemAnalysis ||
            effects.TargetKind is null || effects.SectionSide is null ||
            !string.Equals(effects.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(effects.AnalysisId) || string.IsNullOrWhiteSpace(effects.CombinationId))
            return DesignRuleEvaluation.MissingData("Concomitant, fully traceable FEM station required.");
        if (effects.AxialForceKn is not double n || effects.ShearForceYKn is not double v ||
            effects.BendingMomentZKnm is not double m || !double.IsFinite(n) ||
            !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("All finite signed N,V,M must be provided.");
        if (n >= 0 || v != 0 || m != 0)
            return DesignRuleEvaluation.NotApplicable("M91-A is only for pure axial compression (N<0, V=M=0); no N-M interactions.");

        var gamma = request.Profile.Coefficients?.FirstOrDefault(x =>
            string.Equals(x.Id, GammaId, StringComparison.OrdinalIgnoreCase));
        if (gamma is null || !ValidPositive(gamma.Value) || string.IsNullOrWhiteSpace(gamma.SourceClause))
            return DesignRuleEvaluation.MissingData("Explicit positive gamma_M1 and source required; no default.");

        PublishedRolledSteelSectionM88B2 profile;
        try { profile = sections.Find(a.SectionId).Definition; }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown steel section in M88-B2."); }
        if (profile.Family is not ("IPE" or "HEA" or "HEB"))
            return DesignRuleEvaluation.NotApplicable("Only hot-rolled parallel-flange IPE/HEA/HEB; no inclined-flange INP.");
        // Table 6.2 group for S235, S275, S355. S420 and S460 are not in this catalog scope.
        if (a.SteelGrade is not ("S235" or "S275" or "S355"))
            return DesignRuleEvaluation.MissingData("M91-A only has catalog-backed curves for S235/S275/S355 rolled I/H steel grades.");
        double fy;
        try { fy = materials.GetSteelYieldMpa(a.SteelGrade, Math.Max(profile.FlangeThicknessMm, profile.WebThicknessMm)); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Steel grade not in characteristic catalog."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.MissingData("Steel nominal thickness outside documented characteristic ranges."); }
        if (!ValidPositive(fy) || !ValidPositive(profile.AreaCm2) ||
            !ValidPositive(profile.StrongInertiaCm4) || !ValidPositive(profile.WeakInertiaCm4) ||
            !ValidPositive(profile.HeightMm) || !ValidPositive(profile.WidthMm) ||
            !ValidPositive(profile.FlangeThicknessMm))
            return DesignRuleEvaluation.MissingData("Invalid section geometry, inertias or fy.");

        // Never trust an unsupported or falsely favourable user-declared section class.
        SteelISectionClassificationM90A computedClass;
        try
        {
            computedClass = SteelISectionClassifierM90A.ClassifyCatalogProfile(Code, sections, materials,
                a.SectionId, a.SteelGrade, SteelISectionActionM90A.PureUniformCompression);
        }
        catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or NotSupportedException)
        {
            return DesignRuleEvaluation.MissingData("Independent M90-A compression classification could not be established.");
        }
        if (computedClass.Class == 4)
            return DesignRuleEvaluation.NotApplicable("Independent M90-A identifies a class-4 section; Aeff buckling omitted.");
        if (computedClass.Class != a.CompressionSectionClass)
            return DesignRuleEvaluation.MissingData("Declared compression class differs from independent M90-A result.");
        var curves = SelectRolledICurves(profile.HeightMm, profile.WidthMm, profile.FlangeThicknessMm);
        if (string.IsNullOrEmpty(curves.Strong) || string.IsNullOrEmpty(curves.Weak))
            return DesignRuleEvaluation.NotApplicable("Section dimensions outside implemented rolled-I Table 6.2 branches.");
        var strong = CalculateAxis("y-y", curves.Strong, ly, profile.StrongInertiaCm4,
            profile.AreaCm2, fy, eModulus, gamma.Value);
        var weak = CalculateAxis("z-z", curves.Weak, lz, profile.WeakInertiaCm4,
            profile.AreaCm2, fy, eModulus, gamma.Value);
        if (strong is null || weak is null)
            return DesignRuleEvaluation.MissingData("Invalid/nonfinite buckling intermediate values; cannot infer resistance.");

        static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
        string trace = $"M91-A PURE UNIFORM COMPRESSION, BIAXIAL FLEXURAL BUCKLING ONLY | {Clause} | " +
            $"member={a.MemberId} section={profile.Id} grade={a.SteelGrade} class={a.CompressionSectionClass} " +
            $"classEvidence={a.ClassificationEvidence} M90A-computedClass={computedClass.Class} | " +
            $"source={profile.SourcePageUrl}, status={profile.DataStatus} (INDICATIVE / not certified) | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId} | " +
            $"A={F(profile.AreaCm2)}cm2 fy={F(fy)}MPa E={F(eModulus)}MPa E-source={a.YoungModulusEvidence} " +
            $"gamma_M1={F(gamma.Value)} gamma-source={gamma.SourceClause} | " +
            $"y-y: Lcr={F(ly)}m source={a.StrongAxisLengthEvidence} I={F(profile.StrongInertiaCm4)}cm4 " +
            $"curve={strong.Curve} alpha={F(strong.ImperfectionFactor)} Ncr={F(strong.EulerCriticalForceKn)}kN " +
            $"lambdaBar={F(strong.NonDimensionalSlenderness)} chi={F(strong.ReductionFactor)} " +
            $"NbRd={F(strong.BucklingResistanceKn)}kN | " +
            $"z-z: Lcr={F(lz)}m source={a.WeakAxisLengthEvidence} I={F(profile.WeakInertiaCm4)}cm4 " +
            $"curve={weak.Curve} alpha={F(weak.ImperfectionFactor)} Ncr={F(weak.EulerCriticalForceKn)}kN " +
            $"lambdaBar={F(weak.NonDimensionalSlenderness)} chi={F(weak.ReductionFactor)} " +
            $"NbRd={F(weak.BucklingResistanceKn)}kN | " +
            $"governing={(strong.BucklingResistanceKn <= weak.BucklingResistanceKn ? "y-y" : "z-z")} " +
            $"N_Ed={F(-n)}kN target={effects.CombinationId} analysis={effects.AnalysisId} " +
            $"x={F(effects.PositionMetres)}m side={effects.SectionSide} | " +
            "NOT CHECKED: torsional or flexural-torsional buckling, N-M interactions, bracing strength/stiffness, " +
            "connections, imperfections/global frame stability, class 4, complete member or NTC/EC3 compliance.";
        return DesignRuleEvaluation.Calculated(-n,
            Math.Min(strong.BucklingResistanceKn, weak.BucklingResistanceKn), "kN", trace);
    }

    /// <summary>EN 1993-1-1:2005 Table 6.2. Return (major y-y, minor z-z) buckling curves.</summary>
    public static (string Strong, string Weak) SelectRolledICurves(
        double heightMm, double widthMm, double flangeThicknessMm)
    {
        if (!ValidPositive(heightMm) || !ValidPositive(widthMm) || !ValidPositive(flangeThicknessMm))
            throw new ArgumentOutOfRangeException(nameof(heightMm), "Positive finite rolled-section dimensions required.");
        if (heightMm / widthMm > 1.2)
            return flangeThicknessMm <= 40 ? ("a", "b") : flangeThicknessMm <= 100 ? ("b", "c") : ("", "");
        return flangeThicknessMm <= 100 ? ("b", "c") : ("d", "d");
    }

    /// <summary>Independent one-axis evaluation, for benchmarks and traceability. Null for invalid inputs.</summary>
    public static SteelFlexuralBucklingAxisM91A? CalculateAxis(string axis, string curve,
        double effectiveLengthMetres, double inertiaCm4, double areaCm2,
        double yieldMpa, double youngMpa, double gammaM1)
    {
        if (!ValidPositive(effectiveLengthMetres) || !ValidPositive(inertiaCm4) ||
            !ValidPositive(areaCm2) || !ValidPositive(yieldMpa) || !ValidPositive(youngMpa) ||
            !ValidPositive(gammaM1)) return null;
        double alpha = curve switch
        {
            "a0" => 0.13, "a" => 0.21, "b" => 0.34, "c" => 0.49, "d" => 0.76,
            _ => double.NaN
        };
        if (!double.IsFinite(alpha)) return null;
        // MPa=N/mm2, cm4=1e4 mm4, m=1e3 mm, N=1e-3 kN.
        double ncr = Math.PI * Math.PI * youngMpa * inertiaCm4 * 1e-5 /
            (effectiveLengthMetres * effectiveLengthMetres);
        double yieldForce = areaCm2 * yieldMpa * 0.1; // kN
        if (!ValidPositive(ncr) || !ValidPositive(yieldForce)) return null;
        double lambda = Math.Sqrt(yieldForce / ncr);
        double phi = 0.5 * (1 + alpha * (lambda - 0.2) + lambda * lambda);
        double radicand = phi * phi - lambda * lambda;
        if (!double.IsFinite(lambda) || !double.IsFinite(phi) || !double.IsFinite(radicand)) return null;
        if (radicand < 0 && radicand > -1e-12) radicand = 0; // rounding at lambda≈0.2
        if (radicand < 0) return null;
        double denominator = phi + Math.Sqrt(radicand);
        if (!ValidPositive(denominator)) return null;
        double chi = Math.Min(1d, 1d / denominator);
        double capacity = chi * yieldForce / gammaM1;
        if (!ValidPositive(chi) || !ValidPositive(capacity)) return null;
        return new SteelFlexuralBucklingAxisM91A(axis, curve, alpha,
            effectiveLengthMetres, ncr, lambda, chi, capacity);
    }

    private static bool ValidPositive(double d) => double.IsFinite(d) && d > 0;
}
