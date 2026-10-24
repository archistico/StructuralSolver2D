using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>Two strictly partial NTC 2018 §4.4.8.2 stability checks, NOT member compliance.</summary>
public enum Ntc2018TimberStabilityEffectG4 { PureAxialColumn, StrongAxisBendingBeam }

/// <summary>
/// The 2D FEM supplies neither out-of-plane support stiffness/end restraints nor initial
/// straightness. All documented external assertions below must be independently established.
/// For a column, both effective lengths must be derived externally for the SAME member.
/// For a beam, sigma_m,crit must be independently calculated using characteristic 5% stiffness.
/// </summary>
public sealed record Ntc2018TimberStabilityEvidenceG4(
    Ntc2018TimberSectionEvidenceG2 Section,
    double VerifiedStationMetres, DesignEffectSectionSide VerifiedSectionSide,
    string StationEvidence,
    bool? IntactSinglePrismaticMemberConfirmed, string MemberEvidence,
    bool? CharacteristicStiffnessAndStraightnessConfirmed, string StiffnessAndStraightnessEvidence,
    bool? NoUnmodelledOutOfPlaneActionsConfirmed, string OutOfPlaneEvidence,
    // Columns only; these are buckling/effective lengths, NOT automatically FEM lengths.
    double? EffectiveLengthStrongAxisMetres, double? EffectiveLengthWeakAxisMetres,
    bool? BothEffectiveLengthsIndependentlyVerified, string? EffectiveLengthEvidence,
    bool? PureConcentricColumnAlongFullMemberConfirmed, string? PureColumnEvidence,
    // Beams only: externally validated elastic critical bending stress for the restrained segment.
    double? SegmentStartMetres, double? SegmentEndMetres,
    double? ElasticCriticalBendingStressMpa,
    bool? BothSegmentTorsionalRestraintsVerified, string? TorsionalRestraintEvidence,
    bool? CriticalStressUsesCharacteristic05StiffnessConfirmed, string? CriticalStressEvidence,
    bool? GoverningSegmentMomentAtStationConfirmed, string? GoverningMomentEvidence,
    bool? SeparateConcomitantShearCheckPassed, string? SeparateShearEvidence);

/// <summary>
/// Column: NTC 2018 [4.4.13]-[4.4.16] and §4.4.15. Beam: [4.4.11]-[4.4.12].
/// Limited to intact rectangular solid softwood and homogeneous glulam, NTC-only,
/// characteristic E0,05 (never mean modulus), full independent evidence. Does not verify
/// bracing construction, connections, global sway, combined N-M stability, SLE or member safety.
/// </summary>
public sealed class Ntc2018TimberStabilityRuleG4 : IDesignCheckRule
{
    public const string ColumnId = "NTC2018-TIMBER-RECT-PURE-COLUMN-BUCKLING-G4";
    public const string BeamId = "NTC2018-TIMBER-RECT-STRONG-AXIS-LTB-G4";
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly Ntc2018TimberCoefficientTablesG1 tables;
    private readonly IReadOnlyDictionary<string, Ntc2018TimberStabilityEvidenceG4> evidence;

    public Ntc2018TimberStabilityEffectG4 Effect { get; }
    public DesignCodeEdition Code => Ntc2018TimberCoefficientSelectorG1.Edition;
    public DesignMaterial Material => DesignMaterial.Timber;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public bool EstablishesCompleteMemberCompliance => false;
    public string RuleId => Effect == Ntc2018TimberStabilityEffectG4.PureAxialColumn ? ColumnId : BeamId;
    public string SourceClause => Effect == Ntc2018TimberStabilityEffectG4.PureAxialColumn
        ? "NTC 2018 §4.4.8.2.2 [4.4.13]-[4.4.16]; §4.4.15"
        : "NTC 2018 §4.4.8.2.1 [4.4.11]-[4.4.12]; §4.4.15";

    public Ntc2018TimberStabilityRuleG4(Ntc2018TimberStabilityEffectG4 effect,
        CharacteristicMaterialCatalogM88C materials,
        Ntc2018TimberCoefficientTablesG1 tables,
        IEnumerable<Ntc2018TimberStabilityEvidenceG4> declarations)
    {
        if (!Enum.IsDefined(effect)) throw new ArgumentOutOfRangeException(nameof(effect));
        Effect = effect;
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        this.tables = tables ?? throw new ArgumentNullException(nameof(tables));
        ArgumentNullException.ThrowIfNull(declarations);
        var dictionary = new Dictionary<string, Ntc2018TimberStabilityEvidenceG4>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in declarations)
        {
            var a = d?.Section?.Selection?.Assignment;
            if (a is null || d!.Section.Selection.Profile.Code != Code ||
                string.IsNullOrWhiteSpace(a.MemberId) || string.IsNullOrWhiteSpace(a.AnalysisId) ||
                string.IsNullOrWhiteSpace(a.TargetId) || string.IsNullOrWhiteSpace(a.SectionId) ||
                a.TargetKind != DesignEffectTargetKind.LoadCombination ||
                !double.IsFinite(d.VerifiedStationMetres) || d.VerifiedStationMetres < 0 ||
                !Enum.IsDefined(d.VerifiedSectionSide) || string.IsNullOrWhiteSpace(d.StationEvidence))
                throw new ArgumentException("Exact NTC G1 coefficient profile, target and FEM station required.", nameof(declarations));
            if (!dictionary.TryAdd(Key(a.MemberId, a.AnalysisId, a.TargetId,
                    d.VerifiedStationMetres, d.VerifiedSectionSide), d))
                throw new ArgumentException("Duplicate G4 stability declaration at exact FEM station.", nameof(declarations));
        }
        evidence = dictionary;
    }

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Member is null || request.Effects is null || request.Profile is null)
            return DesignRuleEvaluation.MissingData("Member, profile and FEM results required.");
        if (request.Member.Material != DesignMaterial.Timber || request.LimitState != DesignLimitState.Ultimate)
            return DesignRuleEvaluation.NotApplicable("G4 covers timber SLU only.");
        if (request.Profile.Code != Code)
            return DesignRuleEvaluation.NotApplicable("NTC 2018 G4 cannot reuse EC5 calculations.");
        var e = request.Effects;
        if (e.SectionSide is null || !Enum.IsDefined(e.SectionSide.Value) ||
            !evidence.TryGetValue(Key(request.Member.MemberId, e.AnalysisId, e.CombinationId,
                    e.PositionMetres, e.SectionSide.Value), out var proof))
            return DesignRuleEvaluation.MissingData("Exact member/analysis/combination/station/side stability evidence missing.");
        var sec = proof.Section;
        var s = sec.Selection;
        var a = s.Assignment;
        if (e.Origin != DesignEffectOrigin.FemAnalysis ||
            e.TargetKind != DesignEffectTargetKind.LoadCombination ||
            !string.Equals(e.MemberId, a.MemberId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(request.Member.SectionId, a.SectionId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.AnalysisId, a.AnalysisId, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(e.CombinationId, a.TargetId, StringComparison.OrdinalIgnoreCase) ||
            !MatchProfile(request.Profile, s) ||
            s.Assignment.LoadDuration != s.GoverningDuration ||
            s.Manifest is null || s.Manifest.Count == 0 ||
            string.IsNullOrWhiteSpace(s.CombinationFingerprint))
            return DesignRuleEvaluation.MissingData("NTC profile and full FEM/combination provenance must match.");
        try
        {
            if (s.CombinationFingerprint != Ntc2018TimberCoefficientSelectorG1.Fingerprint(
                    a.AnalysisId, a.TargetId, s.Manifest) ||
                s.Manifest.Where(x => x.CombinationFactor != 0).Max(x => x.Duration) != s.GoverningDuration)
                return DesignRuleEvaluation.MissingData("Combination fingerprint or duration manifest invalid.");
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return DesignRuleEvaluation.MissingData("Invalid combination action manifest.");
        }
        if (!double.IsFinite(request.Member.MemberLengthMetres) || request.Member.MemberLengthMetres <= 0 ||
            !double.IsFinite(a.WidthMm) || !double.IsFinite(a.DepthMm) ||
            a.WidthMm <= 0 || a.DepthMm < a.WidthMm || a.ServiceClass is not (1 or 2 or 3) ||
            string.IsNullOrWhiteSpace(a.GeometryEvidence) || string.IsNullOrWhiteSpace(a.ServiceClassEvidence) ||
            string.IsNullOrWhiteSpace(a.LoadDurationEvidence) ||
            string.IsNullOrWhiteSpace(sec.SectionAndMaterialEvidence) ||
            sec.GrainParallelConfirmed != true || sec.StrongAxisMappingConfirmed != true ||
            sec.PrismaticRectangularBaseSectionConfirmed != true ||
            sec.UnweakenedGrossSectionConfirmed != true || sec.NoBeneficialSizeFactorConfirmed != true ||
            proof.IntactSinglePrismaticMemberConfirmed != true || string.IsNullOrWhiteSpace(proof.MemberEvidence) ||
            proof.CharacteristicStiffnessAndStraightnessConfirmed != true ||
            string.IsNullOrWhiteSpace(proof.StiffnessAndStraightnessEvidence) ||
            proof.NoUnmodelledOutOfPlaneActionsConfirmed != true ||
            string.IsNullOrWhiteSpace(proof.OutOfPlaneEvidence))
            return DesignRuleEvaluation.MissingData("G4 needs intact straight prismatic rectangle, orientation, physical and 5% stiffness evidence.");
        if (e.AxialForceKn is not double n || e.ShearForceYKn is not double v ||
            e.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Concomitant signed N,V,M including zeros are mandatory.");
        TimberStrengthClassM88C grade;
        try { grade = materials.FindTimber(a.StrengthClassId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Timber strength class absent from catalog."); }
        if (grade.Kind is not (StrengthMaterialKindM88C.SolidSoftwood or StrengthMaterialKindM88C.HomogeneousGlulam))
            return DesignRuleEvaluation.NotApplicable("Only solid softwood and homogeneous glulam supported.");
        if (s.Kmod != tables.Kmod(grade.Kind, a.ServiceClass.Value, s.GoverningDuration) ||
            s.GammaM != tables.Gamma(grade.Kind, s.Situation, s.GammaColumn) ||
            !double.IsFinite(grade.ElasticModulusParallel05Mpa) || grade.ElasticModulusParallel05Mpa <= 0 ||
            (s.Kcr.HasValue && s.Kcr.Value !=
                (grade.Kind == StrengthMaterialKindM88C.SolidSoftwood ? 2.0 : 2.5) / grade.ShearStrengthMpa))
            return DesignRuleEvaluation.MissingData("Characteristic 5% modulus or independently checked NTC factors invalid.");
        double area = a.WidthMm * a.DepthMm;
        double w = a.WidthMm * a.DepthMm * a.DepthMm / 6;
        if (!double.IsFinite(area) || !double.IsFinite(w) || area <= 0 || w <= 0 ||
            !double.IsFinite(s.GammaM) || !double.IsFinite(s.Kmod) || s.GammaM <= 0 || s.Kmod <= 0)
            return DesignRuleEvaluation.MissingData("Geometry and resistance parameters invalid.");

        double utilization;
        string details;
        if (Effect == Ntc2018TimberStabilityEffectG4.PureAxialColumn)
        {
            if (n >= 0 || m != 0 || v != 0)
                return DesignRuleEvaluation.NotApplicable("G4 column only covers pure concentric compression with N<0, V=M=0.");
            if (proof.PureConcentricColumnAlongFullMemberConfirmed != true ||
                string.IsNullOrWhiteSpace(proof.PureColumnEvidence) ||
                proof.BothEffectiveLengthsIndependentlyVerified != true ||
                string.IsNullOrWhiteSpace(proof.EffectiveLengthEvidence) ||
                proof.EffectiveLengthStrongAxisMetres is not double ly ||
                proof.EffectiveLengthWeakAxisMetres is not double lz ||
                !Positive(ly) || !Positive(lz))
                return DesignRuleEvaluation.MissingData("Independently established effective lengths on BOTH axes, concentricity and end restraints required.");
            double iy = a.WidthMm * Math.Pow(a.DepthMm, 3) / 12;
            double iz = a.DepthMm * Math.Pow(a.WidthMm, 3) / 12;
            double rY = Math.Sqrt(iy / area);
            double rZ = Math.Sqrt(iz / area);
            double lambdaY = ly * 1000 / rY / Math.PI * Math.Sqrt(grade.CompressionParallelMpa / grade.ElasticModulusParallel05Mpa);
            double lambdaZ = lz * 1000 / rZ / Math.PI * Math.Sqrt(grade.CompressionParallelMpa / grade.ElasticModulusParallel05Mpa);
            double beta = grade.Kind == StrengthMaterialKindM88C.SolidSoftwood ? 0.2 : 0.1;
            double ky = ColumnReduction(lambdaY, beta);
            double kz = ColumnReduction(lambdaZ, beta);
            double governing = Math.Min(ky, kz);
            double fcDesign = s.Kmod * grade.CompressionParallelMpa / s.GammaM;
            utilization = Math.Abs(n) * 1000 / area / (governing * fcDesign);
            details = $"Lcr_y={F(ly)} m; Lcr_z={F(lz)} m; r_y={F(rY)} mm; r_z={F(rZ)} mm; " +
                $"lambda_rel_y={F(lambdaY)}; lambda_rel_z={F(lambdaZ)}; beta_c={F(beta)}; " +
                $"kcrit_c_y={F(ky)}; kcrit_c_z={F(kz)}; governing_kcrit_c={F(governing)}; " +
                $"sigma_c,d={F(Math.Abs(n) * 1000 / area)} MPa; fc0,d={F(fcDesign)} MPa; " +
                $"end_conditions={proof.EffectiveLengthEvidence}; full_member_pure_compression={proof.PureColumnEvidence};";
        }
        else
        {
            if (n != 0 || m == 0)
                return DesignRuleEvaluation.NotApplicable("G4 beam LTB only covers strong-axis bending with N=0 and nonzero M.");
            if (proof.SegmentStartMetres is not double start || proof.SegmentEndMetres is not double end ||
                !double.IsFinite(start) || !double.IsFinite(end) || start < 0 || end <= start ||
                end > request.Member.MemberLengthMetres || e.PositionMetres < start || e.PositionMetres > end ||
                proof.ElasticCriticalBendingStressMpa is not double critical || !Positive(critical) ||
                proof.BothSegmentTorsionalRestraintsVerified != true || string.IsNullOrWhiteSpace(proof.TorsionalRestraintEvidence) ||
                proof.CriticalStressUsesCharacteristic05StiffnessConfirmed != true || string.IsNullOrWhiteSpace(proof.CriticalStressEvidence) ||
                proof.GoverningSegmentMomentAtStationConfirmed != true || string.IsNullOrWhiteSpace(proof.GoverningMomentEvidence) ||
                (v != 0 && (proof.SeparateConcomitantShearCheckPassed != true ||
                    string.IsNullOrWhiteSpace(proof.SeparateShearEvidence))))
                return DesignRuleEvaluation.MissingData("Externally verified torsional supports, segment maximum, 5%-modulus critical stress and separate shear check required.");
            double lambda = Math.Sqrt(grade.BendingStrengthMpa / critical);
            double k = BeamReduction(lambda);
            double fmDesign = s.Kmod * grade.BendingStrengthMpa / s.GammaM;
            double sigma = Math.Abs(m) * 1e6 / w;
            utilization = sigma / (k * fmDesign);
            details = $"segment_start={F(start)} m; segment_end={F(end)} m; " +
                $"sigma_m_crit={F(critical)} MPa EXTERNAL; lambda_rel_m={F(lambda)}; " +
                $"kcrit_m={F(k)}; sigma_m,d={F(sigma)} MPa; fm,d={F(fmDesign)} MPa; " +
                $"torsional_restraints={proof.TorsionalRestraintEvidence}; " +
                $"critical_stress_source={proof.CriticalStressEvidence}; governing_moment={proof.GoverningMomentEvidence}; " +
                $"separate_shear={(v == 0 ? "V=0" : proof.SeparateShearEvidence)};";
        }
        if (!double.IsFinite(utilization) || utilization < 0)
            return DesignRuleEvaluation.MissingData("Invalid computed buckling utilization.");
        var trace = $"G4 PARTIAL NTC 2018 STABILITY ONLY; rule={RuleId}; source={SourceClause}; " +
            $"material={grade.Id} ({grade.ReferenceStandard}; {grade.DataStatus}); " +
            $"member={a.MemberId}; section={a.SectionId}; analysis={e.AnalysisId}; combination={e.CombinationId}; " +
            $"fingerprint={s.CombinationFingerprint}; station={F(e.PositionMetres)} m; side={e.SectionSide}; " +
            $"N={F(n)} kN; V={F(v)} kN; Mz={F(m)} kNm; A={F(area)} mm2; " +
            $"E0,05={F(grade.ElasticModulusParallel05Mpa)} MPa NOT Emean; " +
            $"kmod={F(s.Kmod)}; gammaM={F(s.GammaM)}; kh=1; {details} " +
            $"utilization={F(utilization)}; geometry={proof.MemberEvidence}; " +
            $"straightness_and_stiffness={proof.StiffnessAndStraightnessEvidence}; " +
            $"out_of_plane_actions={proof.OutOfPlaneEvidence}; station_evidence={proof.StationEvidence}. " +
            "NOT VERIFIED: accuracy of external end restraints/critical stress; system sway; " +
            "N-M stability interactions; full-section and connection checks; shear except external assertion; " +
            "bearing; fire; SLE; full combination generation; whole-member or code compliance.";
        return DesignRuleEvaluation.Calculated(utilization, 1.0, "ratio", trace);
    }

    /// <summary>NTC [4.4.15]-[4.4.16]. Pure numerical kernel for independent boundary tests.</summary>
    public static double ColumnReduction(double relativeSlenderness, double betaC)
    {
        if (!Positive(relativeSlenderness) || betaC is not (0.1 or 0.2))
            throw new ArgumentOutOfRangeException(nameof(relativeSlenderness));
        if (relativeSlenderness <= 0.3) return 1;
        double k = 0.5 * (1 + betaC * (relativeSlenderness - 0.3) + relativeSlenderness * relativeSlenderness);
        double radicand = k * k - Math.Pow(relativeSlenderness, 2);
        if (!double.IsFinite(radicand) || radicand < 0) throw new ArgumentException("Invalid stability discriminant.");
        double reduction = 1 / (k + Math.Sqrt(radicand));
        if (!Positive(reduction) || reduction > 1) throw new ArgumentException("Invalid column reduction coefficient.");
        return reduction;
    }

    /// <summary>NTC [4.4.12]. Piecewise continuous, upper bounded at unity.</summary>
    public static double BeamReduction(double relativeSlenderness)
    {
        if (!Positive(relativeSlenderness)) throw new ArgumentOutOfRangeException(nameof(relativeSlenderness));
        double reduction = relativeSlenderness <= 0.75 ? 1 :
            relativeSlenderness <= 1.4 ? 1.56 - 0.75 * relativeSlenderness :
            1 / (relativeSlenderness * relativeSlenderness);
        if (!Positive(reduction) || reduction > 1 + 1e-12)
            throw new ArgumentException("Invalid beam reduction coefficient.");
        return Math.Min(1, reduction);
    }

    private static bool Positive(double value) => double.IsFinite(value) && value > 0;
    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    private static bool MatchProfile(NormativeProfile supplied, Ntc2018TimberSelectedCoefficientsG1 s)
    {
        var expected = s.Profile;
        if (supplied.Code != Ntc2018TimberCoefficientSelectorG1.Edition ||
            supplied.Code != expected.Code || supplied.ProfileId != expected.ProfileId ||
            supplied.Coefficients is null || expected.Coefficients is null ||
            supplied.Coefficients.Count != expected.Coefficients.Count)
            return false;
        foreach (var c in expected.Coefficients)
        {
            var matches = supplied.Coefficients.Where(x => x is not null && x.Id == c.Id).ToArray();
            if (matches.Length != 1 || matches[0] != c) return false;
        }
        return expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.KmodId && c.Value == s.Kmod) &&
            expected.Coefficients.Any(c => c.Id == TimberRectangularSectionRuleM92A.GammaId && c.Value == s.GammaM);
    }
    private static string Key(string member, string analysis, string combination, double station,
        DesignEffectSectionSide side) =>
        $"{member.ToUpperInvariant()}\u001f{analysis.ToUpperInvariant()}\u001f{combination.ToUpperInvariant()}\u001f{BitConverter.DoubleToInt64Bits(station)}\u001f{(int)side}";
}
