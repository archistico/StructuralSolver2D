using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>
/// G3 is strictly a uniaxial STR section screening rule for a constant, undamaged rectangular
/// section at ONE FEM station. It does not verify beam or column stability, bearings or joints.
/// The simultaneous V check is the independent NTC 4.4.8.1.9 inequality, NOT an invented
/// N-V-M strength interaction equation.
/// </summary>
public enum Ntc2018TimberCombinedEffectG3
{
    TensionBending,
    CompressionBending,
    BendingAndShear
}

/// <summary>
/// External physical and analytical evidence not available from a planar first-order FEM.
/// A G2 section evidence declaration is reused for its NTC G1 selection and geometry;
/// no G2 resistance rule is executed and no EC5 rule is re-labelled.
/// </summary>
public sealed record Ntc2018TimberCombinedEvidenceG3(
    Ntc2018TimberSectionEvidenceG2 Section,
    bool? SignedConcomitantFemEffectsConfirmed,
    string ConcomitantEffectsEvidence,
    bool? NoOutOfPlaneBendingConfirmed,
    string OutOfPlaneBendingEvidence,
    bool? LinearElasticStressAssumptionConfirmed,
    string LinearElasticEvidence,
    double VerifiedStationMetres,
    DesignEffectSectionSide VerifiedSectionSide,
    string StationEvidence);

/// <summary>
/// NTC2018 equations 4.4.5a/b, 4.4.6a/b, 4.4.7a/b and independent 4.4.8;
/// 2019 Circolare C4.4.8.1.9 for the shear crack factor.
/// G3 covers only strong-axis 2D bending, without an out-of-plane moment, with externally
/// verified constant section and concomitant signed N, V, M; kh is conservatively 1.
/// </summary>
public sealed class Ntc2018TimberCombinedSectionRuleG3 : IDesignCheckRule
{
    public const string TensionBendingId = "NTC2018-TIMBER-RECT-TENSO-BENDING-G3";
    public const string CompressionBendingId = "NTC2018-TIMBER-RECT-PRESSO-BENDING-G3";
    public const string BendingShearId = "NTC2018-TIMBER-RECT-BENDING-SHEAR-G3";
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly Ntc2018TimberCoefficientTablesG1 tables;
    private readonly IReadOnlyDictionary<string, Ntc2018TimberCombinedEvidenceG3> evidence;

    public Ntc2018TimberCombinedEffectG3 Effect { get; }
    public DesignCodeEdition Code => Ntc2018TimberCoefficientSelectorG1.Edition;
    public DesignMaterial Material => DesignMaterial.Timber;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public bool EstablishesCompleteMemberCompliance => false;
    public string RuleId => Effect switch
    {
        Ntc2018TimberCombinedEffectG3.TensionBending => TensionBendingId,
        Ntc2018TimberCombinedEffectG3.CompressionBending => CompressionBendingId,
        Ntc2018TimberCombinedEffectG3.BendingAndShear => BendingShearId,
        _ => throw new InvalidOperationException("Unknown G3 effect.")
    };
    public string SourceClause => Effect switch
    {
        Ntc2018TimberCombinedEffectG3.TensionBending =>
            "NTC 2018 4.4.6, 4.4.8.1.7 [4.4.6a,b]; 4.4.8.1.9 [4.4.8] when V != 0",
        Ntc2018TimberCombinedEffectG3.CompressionBending =>
            "NTC 2018 4.4.6, 4.4.8.1.8 [4.4.7a,b]; 4.4.8.1.9 [4.4.8] when V != 0",
        Ntc2018TimberCombinedEffectG3.BendingAndShear =>
            "NTC 2018 4.4.8.1.6 [4.4.5a,b], 4.4.8.1.9 [4.4.8]; Circolare C4.4.8.1.9",
        _ => throw new InvalidOperationException("Unknown G3 effect.")
    };

    public Ntc2018TimberCombinedSectionRuleG3(Ntc2018TimberCombinedEffectG3 effect,
        CharacteristicMaterialCatalogM88C materials,
        Ntc2018TimberCoefficientTablesG1 tables,
        IEnumerable<Ntc2018TimberCombinedEvidenceG3> declarations)
    {
        if (!Enum.IsDefined(effect)) throw new ArgumentOutOfRangeException(nameof(effect));
        Effect = effect;
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        this.tables = tables ?? throw new ArgumentNullException(nameof(tables));
        ArgumentNullException.ThrowIfNull(declarations);
        var map = new Dictionary<string, Ntc2018TimberCombinedEvidenceG3>(StringComparer.OrdinalIgnoreCase);
        foreach (var proof in declarations)
        {
            var s = proof?.Section?.Selection;
            var a = s?.Assignment;
            if (a is null || s!.Profile.Code != Code ||
                string.IsNullOrWhiteSpace(a.MemberId) || string.IsNullOrWhiteSpace(a.SectionId) ||
                string.IsNullOrWhiteSpace(a.AnalysisId) || string.IsNullOrWhiteSpace(a.TargetId) ||
                a.TargetKind != DesignEffectTargetKind.LoadCombination ||
                !double.IsFinite(proof!.VerifiedStationMetres) || proof.VerifiedStationMetres < 0 ||
                !Enum.IsDefined(proof.VerifiedSectionSide) ||
                string.IsNullOrWhiteSpace(proof.StationEvidence))
                throw new ArgumentException("G3 requires selected NTC G1 coefficients and exact member/combination evidence.", nameof(declarations));
            if (!map.TryAdd(Key(a.MemberId, a.AnalysisId, a.TargetId,
                proof!.VerifiedStationMetres, proof.VerifiedSectionSide), proof))
                throw new ArgumentException("Duplicate G3 evidence for member/analysis/combination.", nameof(declarations));
        }
        evidence = map;
    }

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Member is null || request.Effects is null || request.Profile is null)
            return DesignRuleEvaluation.MissingData("Member, FEM result and profile required.");
        if (request.Member.Material != DesignMaterial.Timber || request.LimitState != DesignLimitState.Ultimate)
            return DesignRuleEvaluation.NotApplicable("G3 is restricted to timber SLU.");
        if (request.Profile.Code != Code)
            return DesignRuleEvaluation.NotApplicable("NTC 2018 G3 is not an EC5 design rule.");
        var e = request.Effects;
        if (string.IsNullOrWhiteSpace(e.AnalysisId) || string.IsNullOrWhiteSpace(e.CombinationId) ||
            e.SectionSide is null ||
            !evidence.TryGetValue(Key(request.Member.MemberId, e.AnalysisId, e.CombinationId,
                e.PositionMetres, e.SectionSide.Value), out var proof))
            return DesignRuleEvaluation.MissingData("Missing exact G3 member/analysis/combination evidence.");
        var sec = proof.Section;
        var s = sec.Selection;
        var a = s.Assignment;
        if (e.Origin != DesignEffectOrigin.FemAnalysis ||
            e.TargetKind != DesignEffectTargetKind.LoadCombination ||
            e.SectionSide is null || !Enum.IsDefined(e.SectionSide.Value) ||
            !string.Equals(e.MemberId, a.MemberId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Member.SectionId, a.SectionId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.AnalysisId, a.AnalysisId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.CombinationId, a.TargetId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("FEM member, section, analysis, combination and one-sided station must match exactly.");
        if (proof.SignedConcomitantFemEffectsConfirmed != true ||
            string.IsNullOrWhiteSpace(proof.ConcomitantEffectsEvidence) ||
            proof.NoOutOfPlaneBendingConfirmed != true ||
            string.IsNullOrWhiteSpace(proof.OutOfPlaneBendingEvidence) ||
            proof.LinearElasticStressAssumptionConfirmed != true ||
            string.IsNullOrWhiteSpace(proof.LinearElasticEvidence))
            return DesignRuleEvaluation.MissingData("Signed concomitance, absent weak-axis moment and linear-elastic validity require external evidence.");
        if (!MatchingG1Profile(request.Profile, s) ||
            s.Assignment.LoadDuration != s.GoverningDuration ||
            string.IsNullOrWhiteSpace(s.CombinationFingerprint) ||
            s.Manifest is null || s.Manifest.Count == 0)
            return DesignRuleEvaluation.MissingData("NTC selected coefficients/combination/duration cannot be established.");
        try
        {
            if (s.CombinationFingerprint != Ntc2018TimberCoefficientSelectorG1.Fingerprint(
                a.AnalysisId, a.TargetId, s.Manifest) ||
                s.Manifest.Where(x => x.CombinationFactor != 0).Max(x => x.Duration) != s.GoverningDuration)
                return DesignRuleEvaluation.MissingData("Selected duration or G1 combination fingerprint does not match manifest.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return DesignRuleEvaluation.MissingData("Malformed G1 combination action manifest.");
        }
        if (!double.IsFinite(a.WidthMm) || !double.IsFinite(a.DepthMm) ||
            a.WidthMm <= 0 || a.DepthMm <= 0 ||
            a.ServiceClass is not (1 or 2 or 3) ||
            string.IsNullOrWhiteSpace(a.GeometryEvidence) ||
            string.IsNullOrWhiteSpace(a.ServiceClassEvidence) ||
            string.IsNullOrWhiteSpace(a.LoadDurationEvidence) ||
            string.IsNullOrWhiteSpace(sec.SectionAndMaterialEvidence) ||
            sec.GrainParallelConfirmed != true || sec.StrongAxisMappingConfirmed != true ||
            sec.PrismaticRectangularBaseSectionConfirmed != true ||
            sec.UnweakenedGrossSectionConfirmed != true ||
            sec.NoBeneficialSizeFactorConfirmed != true)
            return DesignRuleEvaluation.MissingData("Intact rectangular geometry, grain, strong axis, kh=1 and documented material/environment are required.");
        if (e.AxialForceKn is not double n || e.ShearForceYKn is not double v ||
            e.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("All three signed concomitant N/V/M FEM components, including explicit zeros, are required.");
        if (m == 0 ||
            Effect == Ntc2018TimberCombinedEffectG3.TensionBending && n <= 0 ||
            Effect == Ntc2018TimberCombinedEffectG3.CompressionBending && n >= 0 ||
            Effect == Ntc2018TimberCombinedEffectG3.BendingAndShear && (n != 0 || v == 0))
            return DesignRuleEvaluation.NotApplicable("Selected G3 interaction does not occur; do not substitute another branch.");
        TimberStrengthClassM88C grade;
        try { grade = materials.FindTimber(a.StrengthClassId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unsupported timber strength class."); }
        if (grade.Kind is not (StrengthMaterialKindM88C.SolidSoftwood or StrengthMaterialKindM88C.HomogeneousGlulam))
            return DesignRuleEvaluation.NotApplicable("Only solid softwood and homogeneous glulam in G3.");
        // Normative values are checked from independent NTC tables, not trusted merely because
        // a caller supplied an authentic-looking profile identifier or matching object.
        if (s.Kmod != tables.Kmod(grade.Kind, a.ServiceClass!.Value, s.GoverningDuration) ||
            s.GammaM != tables.Gamma(grade.Kind, s.Situation, s.GammaColumn))
            return DesignRuleEvaluation.MissingData("G1 kmod or gamma_M differs from NTC 2018 source table.");
        if (s.Kcr.HasValue && s.Kcr.Value !=
            (grade.Kind == StrengthMaterialKindM88C.SolidSoftwood ? 2.0 : 2.5) / grade.ShearStrengthMpa)
            return DesignRuleEvaluation.MissingData("G1 shear crack factor differs from Circolare 2019.");
        double area = a.WidthMm * a.DepthMm;
        double w = a.WidthMm * a.DepthMm * a.DepthMm / 6.0;
        if (!double.IsFinite(area) || !double.IsFinite(w) || area <= 0 || w <= 0 ||
            !double.IsFinite(s.Kmod) || s.Kmod <= 0 ||
            !double.IsFinite(s.GammaM) || s.GammaM <= 0)
            return DesignRuleEvaluation.MissingData("Section geometry or NTC resistance coefficients invalid.");
        if (Effect == Ntc2018TimberCombinedEffectG3.TensionBending &&
            (sec.VerifiedNetTensionAreaMm2 is not double net || !double.IsFinite(net) ||
             net != area || string.IsNullOrWhiteSpace(sec.NetTensionAreaEvidence)))
            return DesignRuleEvaluation.MissingData("Tensoflessione G3 only supports fully intact section with verified A_net=A_gross (no holes/eccentricity).");
        bool shearRequired = v != 0;
        if (shearRequired && (s.Kcr is not double kcr || !double.IsFinite(kcr) ||
            kcr <= 0 || kcr > 1 || sec.ShearDirectionConfirmed != true ||
            string.IsNullOrWhiteSpace(sec.ShearEvidence)))
            return DesignRuleEvaluation.MissingData("Concomitant shear requires verified Circolare kcr, orientation and intact web.");

        double fm = s.Kmod * grade.BendingStrengthMpa / s.GammaM;
        double faxial = s.Kmod * (n > 0 ? grade.TensionParallelMpa : grade.CompressionParallelMpa) / s.GammaM;
        double fv = s.Kmod * grade.ShearStrengthMpa / s.GammaM;
        double sigmaM = Math.Abs(m) * 1e6 / w;
        double sigmaN = Math.Abs(n) * 1000.0 / area;
        double sigmaV = shearRequired ? 1.5 * Math.Abs(v) * 1000.0 / (s.Kcr!.Value * area) : 0;
        if (!double.IsFinite(fm) || !double.IsFinite(faxial) || !double.IsFinite(fv) ||
            !double.IsFinite(sigmaM) || !double.IsFinite(sigmaN) || !double.IsFinite(sigmaV) ||
            fm <= 0 || faxial <= 0 || fv <= 0)
            return DesignRuleEvaluation.MissingData("Invalid axial/bending/shear strength or stress.");
        const double km = 0.7; // NTC 2018 4.4.8.1.6: rectangular section.
        double axialTerm = n < 0 ? Math.Pow(sigmaN / faxial, 2) : sigmaN / faxial;
        double bendingTerm = sigmaM / fm;
        // NTC 4.4.5a/b, 4.4.6a/b, 4.4.7a/b for My=0 (externally confirmed).
        double equationA = axialTerm + bendingTerm;
        double equationB = axialTerm + km * bendingTerm;
        // NTC 4.4.8 (Jourawski, crack-reduced shear area) is checked independently.
        // max(ratios) combines STATUS only; it is NOT a fabricated N-V-M interaction.
        double shearRatio = shearRequired ? sigmaV / fv : 0;
        double utilization = Math.Max(Math.Max(equationA, equationB), shearRatio);
        if (!double.IsFinite(utilization) || utilization < 0)
            return DesignRuleEvaluation.MissingData("Non-finite combined section utilization.");

        static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
        var trace = $"G3 NTC2018 RECTANGULAR LOCAL SECTION, NOT MEMBER APPROVAL; " +
            $"effect={Effect}; normative={SourceClause}; member={a.MemberId}; section={a.SectionId}; " +
            $"material={grade.Id} ({grade.ReferenceStandard}; {grade.DataStatus}); " +
            $"analysis={e.AnalysisId}; combination={e.CombinationId}; fp={s.CombinationFingerprint}; " +
            $"station={F(e.PositionMetres)} m; side={e.SectionSide}; station_evidence={proof.StationEvidence}; " +
            $"N={F(n)} kN; V={F(v)} kN; Mz={F(m)} kNm; My=0 EXTERNALLY CONFIRMED; " +
            $"A={F(area)} mm2; Wz={F(w)} mm3; b={F(a.WidthMm)} mm; h={F(a.DepthMm)} mm; " +
            $"sigma_N={F(sigmaN)} MPa; sigma_m={F(sigmaM)} MPa; tau={F(sigmaV)} MPa; " +
            $"ft/c,d={F(faxial)} MPa; fm,d={F(fm)} MPa; fv,d={F(fv)} MPa; " +
            $"kmod={F(s.Kmod)}; gamma_M={F(s.GammaM)}; km={F(km)}; kh=1; " +
            $"kcr={(s.Kcr.HasValue ? F(s.Kcr.Value) : "not used")}; situation={s.Situation}; column={s.GammaColumn}; " +
            $"N-M/bending equation a={F(equationA)}; equation b={F(equationB)}; " +
            $"separate shear ratio={F(shearRatio)}; governing ratio={F(utilization)}. " +
            $"evidence concomitance={proof.ConcomitantEffectsEvidence}; weak axis={proof.OutOfPlaneBendingEvidence}; " +
            $"elasticity={proof.LinearElasticEvidence}; geometry={a.GeometryEvidence}; " +
            $"G1 coefficients are externally evidenced, not independently certified by G3. " +
            "NOT VERIFIED: column/beam buckling; lateral torsional buckling; out-of-plane action; " +
            "bearing and perpendicular-grain stresses; connections; notches/holes; fire; SLE; " +
            "generation or global exhaustiveness of NTC combinations; entire member/code compliance.";
        return DesignRuleEvaluation.Calculated(utilization, 1.0, "ratio", trace);
    }

    private static bool MatchingG1Profile(NormativeProfile actual, Ntc2018TimberSelectedCoefficientsG1 s)
    {
        var expected = s.Profile;
        if (actual.Code != Ntc2018TimberCoefficientSelectorG1.Edition ||
            actual.Code != expected.Code ||
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
        return expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.KmodId && c.Value == s.Kmod) &&
            expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.GammaId && c.Value == s.GammaM) &&
            (!s.Kcr.HasValue || expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.CrackId && c.Value == s.Kcr.Value));
    }

    private static string Key(string member, string analysis, string combination,
        double station, DesignEffectSectionSide side) =>
        $"{member.ToUpperInvariant()}\u001f{analysis.ToUpperInvariant()}\u001f{combination.ToUpperInvariant()}\u001f{BitConverter.DoubleToInt64Bits(station)}\u001f{(int)side}";
}
