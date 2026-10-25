using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Independent physical evidence for ONE signed FEM N=0, Vy+Mz section side.
/// A_v and A_w [mm2] are distinct. W_el and W_pl [mm3] are also distinct.
/// No string/evidence flag is itself proof of actual manufactured dimensions.
/// </summary>
public sealed record Ntc2018SteelBendingShearEvidenceD2C2(
    string MemberId,
    string SectionId,
    string SteelGrade,
    string AnalysisId,
    string CombinationId,
    double PositionMetres,
    DesignEffectSectionSide SectionSide,
    Ntc2018SteelHoleStateD1 HoleState,
    string? HoleStateEvidence,
    bool? HotRolledParallelFlangeAndDoublySymmetricConfirmed,
    bool? FemMzStrongAxisAndVyWebPlaneConfirmed,
    bool? UnstiffenedWebConfirmed,
    bool? OtherSpatialActionsAndTorsionExcludedConfirmed,
    string? SectionGeometryEvidence,
    string? MaterialYieldEvidence,
    string? CombinedActionEvidence,
    string? WebShearAreaEvidence,
    double? DeclaredWebShearAreaMm2,
    string? ElasticModulusEvidence,
    double? DeclaredStrongElasticModulusMm3,
    string? PlasticModulusEvidence,
    double? DeclaredStrongPlasticModulusMm3,
    string? WebAreaEvidence,
    double? DeclaredGeometricWebAreaMm2);

/// <summary>
/// NTC 2018 §4.2.4.1.2.6 Eqs [4.2.30]-[4.2.32], including native shear
/// [4.2.16]-[4.2.18] and the conservative unstiffened-web screen [4.2.27].
/// Classes 1/2 allow a verified high-V reduction, class 3 LOW-V only.
/// Returns a DIMENSIONLESS max(V/Vc,Rd, M/M_V,Rd): both demands MUST pass.
/// Never establishes whole-member or whole-structure compliance.
/// </summary>
public sealed class Ntc2018SteelBendingShearInteractionRuleD2C2 : IDesignCheckRule
{
    public const string Id = "NTC2018-STEEL-STRONG-BENDING-WEB-SHEAR-D2C2";
    public const string Clause = "NTC 2018, 4.2.4.1.2.6 [4.2.30]-[4.2.32]; 4.2.4.1.2.3; 4.2.4.1.2.4 [4.2.16]-[4.2.18], [4.2.27]";

    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, Ntc2018SteelBendingShearEvidenceD2C2> evidence;

    public Ntc2018SteelBendingShearInteractionRuleD2C2(
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<Ntc2018SteelBendingShearEvidenceD2C2> evidence)
    {
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(evidence);
        var index = new Dictionary<string, Ntc2018SteelBendingShearEvidenceD2C2>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in evidence)
        {
            if (e is null || string.IsNullOrWhiteSpace(e.MemberId) ||
                string.IsNullOrWhiteSpace(e.SectionId) || string.IsNullOrWhiteSpace(e.SteelGrade) ||
                string.IsNullOrWhiteSpace(e.AnalysisId) || string.IsNullOrWhiteSpace(e.CombinationId) ||
                !double.IsFinite(e.PositionMetres) || e.PositionMetres < 0 ||
                !Enum.IsDefined(e.SectionSide))
                throw new ArgumentException("Each D2C2 evidence must identify one valid FEM station/side.", nameof(evidence));
            if (!index.TryAdd(Key(e.MemberId, e.AnalysisId, e.CombinationId, e.PositionMetres, e.SectionSide), e))
                throw new ArgumentException("Duplicate D2C2 evidence for the same FEM station/side.", nameof(evidence));
        }
        this.evidence = index;
    }

    public string RuleId => Id;
    public DesignCodeEdition Code => Ntc2018SteelTensionProfileD1.Edition;
    public DesignMaterial Material => DesignMaterial.Steel;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public string SourceClause => Clause;
    public bool EstablishesCompleteMemberCompliance => false;

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Ntc2018SteelTensionProfileD1.IsExact(request.Profile))
            return DesignRuleEvaluation.MissingData("Exact NTC 2018 ordinary-member gamma factors and edition required; no EC3 substitution.");
        if (request.Member.Material != Material || request.LimitState != LimitState)
            return DesignRuleEvaluation.NotApplicable("Only NTC 2018 steel at SLU is covered.");

        var s = request.Effects;
        if (s.Origin != DesignEffectOrigin.FemAnalysis ||
            s.TargetKind != DesignEffectTargetKind.LoadCombination ||
            s.SectionSide is not { } side || !Enum.IsDefined(side) ||
            string.IsNullOrWhiteSpace(s.MemberId) ||
            !string.Equals(s.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(s.AnalysisId) || string.IsNullOrWhiteSpace(s.CombinationId) ||
            !double.IsFinite(s.PositionMetres) || s.PositionMetres < 0 ||
            !double.IsFinite(request.Member.MemberLengthMetres) ||
            s.PositionMetres > request.Member.MemberLengthMetres)
            return DesignRuleEvaluation.MissingData("Exact signed FEM member/combination/station/side is mandatory.");
        if (s.AxialForceKn is not double n || s.ShearForceYKn is not double v ||
            s.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("All concurrent signed N/Vy/Mz must be known and finite.");
        if (n != 0d || v == 0d || m == 0d)
            return DesignRuleEvaluation.NotApplicable("D2C2 requires simultaneous Mz!=0, Vy!=0, strictly N=0; use separate pure-action rules otherwise.");

        if (!evidence.TryGetValue(Key(request.Member.MemberId, s.AnalysisId, s.CombinationId,
                s.PositionMetres, side), out var e))
            return DesignRuleEvaluation.MissingData("Missing physical D2C2 proof for exact FEM target and section side.");
        if (!string.Equals(e.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Declared section does not match selected FEM section.");
        if (e.HoleState != Ntc2018SteelHoleStateD1.NoRelevantHoles ||
            string.IsNullOrWhiteSpace(e.HoleStateEvidence) ||
            e.HotRolledParallelFlangeAndDoublySymmetricConfirmed != true ||
            e.FemMzStrongAxisAndVyWebPlaneConfirmed != true ||
            e.UnstiffenedWebConfirmed != true ||
            e.OtherSpatialActionsAndTorsionExcludedConfirmed != true ||
            string.IsNullOrWhiteSpace(e.SectionGeometryEvidence) ||
            string.IsNullOrWhiteSpace(e.MaterialYieldEvidence) ||
            string.IsNullOrWhiteSpace(e.CombinedActionEvidence) ||
            string.IsNullOrWhiteSpace(e.WebShearAreaEvidence) ||
            string.IsNullOrWhiteSpace(e.ElasticModulusEvidence))
            return DesignRuleEvaluation.MissingData("Document I/H geometry, holes, grade, simultaneous local Mz/Vy axes, Av, Wel, unstiffened web and excluded torsion/spatial effects.");

        Ntc2018SteelISectionClassificationD0 classified;
        try
        {
            // D0 classifies the bending NORMAL-STRESS pattern; N=0 makes that
            // pattern pure strong-axis bending even when shear acts separately.
            classified = Ntc2018SteelISectionClassifierD0.ClassifyCatalogProfile(Code,
                sections, materials, e.SectionId, e.SteelGrade,
                Ntc2018SteelPureActionD0.StrongAxisBending,
                new Ntc2018SteelClassificationEvidenceD0(true, true, true,
                    e.SectionGeometryEvidence, e.MaterialYieldEvidence,
                    "Strong-axis normal-stress bending pattern; shear Vy verified separately: " +
                    e.CombinedActionEvidence));
        }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Section/steel grade missing from nominal catalog."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.NotApplicable("Only catalog IPE/HEA/HEB and supported grade thickness bands."); }
        catch (ArgumentException) { return DesignRuleEvaluation.MissingData("Invalid material, classification or nominal geometry."); }

        if (classified.Class == 4)
            return DesignRuleEvaluation.NotApplicable("Class 4 needs effective section properties and a specific interaction treatment; D2C2 does not supply them.");
        if (classified.Class is < 1 or > 3)
            return DesignRuleEvaluation.MissingData("Unsupported NTC strong-bending section class.");

        var p = sections.Find(e.SectionId).Definition;
        double area = p.AreaCm2 * 100d;
        double avNtc = area - 2d * p.WidthMm * p.FlangeThicknessMm +
            (p.WebThicknessMm + 2d * p.RootRadiusMm) * p.FlangeThicknessMm;
        if (!double.IsFinite(avNtc) || avNtc <= 0 || avNtc > area ||
            e.DeclaredWebShearAreaMm2 is not double avDeclared ||
            !double.IsFinite(avDeclared) || avDeclared <= 0 || avDeclared > area ||
            Math.Abs(avDeclared - avNtc) > avNtc * 0.001d)
            return DesignRuleEvaluation.MissingData("Independently sourced Av [mm2] must agree with NTC [4.2.18] within 0.1%.");

        double fy = classified.YieldStrengthMpa;
        double hwUpper = p.HeightMm - 2d * p.FlangeThicknessMm;
        double webRatioUpper = hwUpper / p.WebThicknessMm;
        double bucklingThreshold = 72d * Math.Sqrt(235d / fy); // eta=1 conservatively
        if (!double.IsFinite(webRatioUpper) || !double.IsFinite(bucklingThreshold) ||
            hwUpper <= 0 || webRatioUpper <= 0 || bucklingThreshold <= 0)
            return DesignRuleEvaluation.MissingData("Unusable web geometry for NTC [4.2.27] shear-buckling screen.");
        if (webRatioUpper > bucklingThreshold)
            return DesignRuleEvaluation.NotApplicable("NTC [4.2.27] shear-buckling verification required: D2C2 does not calculate web buckling or its M/V coupling.");

        double avUsed = Math.Min(avNtc, avDeclared);
        double vcRd = avUsed * fy / (Math.Sqrt(3d) * Ntc2018SteelTensionProfileD1.GammaM0) / 1000d;
        double welCatalog = p.StrongElasticModulusCm3 * 1000d;
        if (!double.IsFinite(vcRd) || vcRd <= 0 || !double.IsFinite(welCatalog) || welCatalog <= 0 ||
            e.DeclaredStrongElasticModulusMm3 is not double welDeclared ||
            !double.IsFinite(welDeclared) || welDeclared <= 0 ||
            Math.Abs(welDeclared - welCatalog) > welCatalog * 0.001d)
            return DesignRuleEvaluation.MissingData("Vc,Rd or independently sourced Wel,strong [mm3] invalid; OPPO Wel difference maximum 0.1%.");

        double welUsed = Math.Min(welCatalog, welDeclared);
        double momentModulus;
        if (classified.Class is 1 or 2)
        {
            if (string.IsNullOrWhiteSpace(e.PlasticModulusEvidence) ||
                e.DeclaredStrongPlasticModulusMm3 is not double wpl ||
                !double.IsFinite(wpl) || wpl < welUsed ||
                wpl > area * p.HeightMm / 2d)
                return DesignRuleEvaluation.MissingData("Class 1/2 needs an independently sourced physical Wpl,strong >= Wel and <= A*h/2; OPPO does not contain Wpl.");
            momentModulus = wpl;
        }
        else
        {
            momentModulus = welUsed;
        }

        double pureMomentRd = momentModulus * fy / Ntc2018SteelTensionProfileD1.GammaM0 / 1_000_000d;
        double vEd = Math.Abs(v);
        double mEd = Math.Abs(m);
        double vRatio = vEd / vcRd;
        if (!double.IsFinite(vEd) || !double.IsFinite(mEd) ||
            !double.IsFinite(pureMomentRd) || pureMomentRd <= 0 || !double.IsFinite(vRatio))
            return DesignRuleEvaluation.MissingData("Invalid simultaneous FEM loads or positive nominal resistances.");

        // Shear can fail by itself. Do NOT extrapolate rho beyond Vc,Rd.
        if (vRatio > 1d)
        {
            string shearFailure = $"SECTION ONLY; NTC shear [4.2.16], bending/shear [4.2.30]; " +
                $"VEd={F(vEd)} kN > VcRd={F(vcRd)} kN; V/VcRd={F(vRatio)}; " +
                $"Mz={F(m)} kNm; rho/M_VRd NOT evaluated outside V<=VcRd; " +
                $"FEM={e.MemberId}/{e.AnalysisId}/{e.CombinationId} x={F(s.PositionMetres)} side={side}; " +
                "SECTION FAILS SHEAR, NO MEMBER/STRUCTURE COMPLIANCE.";
            return DesignRuleEvaluation.Calculated(vRatio, 1d, "ratio", shearFailure);
        }

        double rho = 0d;
        double momentRd = pureMomentRd;
        double? awUsed = null;
        string branch = "LowShear-NoMomentReduction-[4.2.30]";

        if (vRatio > 0.5d)
        {
            if (classified.Class == 3)
                return DesignRuleEvaluation.NotApplicable("High V>0.5 Vc,Rd for class 3: NTC [4.2.32] is limited to class 1/2; no unproven reduction.");
            if (string.IsNullOrWhiteSpace(e.WebAreaEvidence) ||
                e.DeclaredGeometricWebAreaMm2 is not double awDeclared ||
                !double.IsFinite(awDeclared))
                return DesignRuleEvaluation.MissingData("High-shear class 1/2 requires independently documented A_w web area [mm2], distinct from A_v.");

            double awFlat = (p.HeightMm - 2d * (p.FlangeThicknessMm + p.RootRadiusMm)) * p.WebThicknessMm;
            double awNominalUpper = area - 2d * p.WidthMm * p.FlangeThicknessMm;
            if (!double.IsFinite(awFlat) || !double.IsFinite(awNominalUpper) ||
                awFlat <= 0 || awNominalUpper < awFlat || awNominalUpper >= area ||
                awDeclared < awFlat * 0.999d || awDeclared > awNominalUpper * 1.001d)
                return DesignRuleEvaluation.MissingData("A_w must be independently credible between flat-web area and upper nominal web+fillets area; OPPO nominal data are not certified.");

            // Conservative web-area upper bound includes the root material that
            // is outside the two rectangular flanges. Using MORE Aw produces
            // LESS reduced plastic bending resistance in [4.2.32]. This is NOT Av.
            awUsed = Math.Max(awDeclared, awNominalUpper);
            rho = Math.Pow(2d * vRatio - 1d, 2d);
            double reducedModulus = momentModulus - rho * awUsed.Value * awUsed.Value /
                (4d * p.WebThicknessMm);
            momentRd = Math.Min(pureMomentRd,
                reducedModulus * fy / Ntc2018SteelTensionProfileD1.GammaM0 / 1_000_000d);
            branch = "HighShear-PlasticMomentReduction-[4.2.31]-[4.2.32]";
            if (!double.IsFinite(rho) || rho < 0 || rho > 1 ||
                !double.IsFinite(momentRd) || momentRd <= 0)
                return DesignRuleEvaluation.NotApplicable("Reduced plastic modulus becomes nonphysical; no extrapolated M-V capacity.");
        }

        double mRatio = mEd / momentRd;
        double combinedRatio = Math.Max(vRatio, mRatio);
        if (!double.IsFinite(mRatio) || !double.IsFinite(combinedRatio))
            return DesignRuleEvaluation.MissingData("Nonfinite ratio after evaluating M/V interaction.");

        string trace = $"SECTION ONLY; {SourceClause}; {branch}; " +
            $"member={e.MemberId} section={e.SectionId} grade={e.SteelGrade}; " +
            $"class={classified.Class}; fyk={F(fy)} MPa gammaM0={F(Ntc2018SteelTensionProfileD1.GammaM0)}; " +
            $"A_OPPO={F(area)} mm2 Av_NTC={F(avNtc)} mm2 Av_declared={F(avDeclared)} mm2 " +
            $"Av_used_min={F(avUsed)} mm2; VEd={F(vEd)} kN VcRd={F(vcRd)} kN " +
            $"Vutil={F(vRatio)}; hw_upper={F(hwUpper)} mm hw/tw={F(webRatioUpper)} " +
            $"bucklingThreshold_eta1={F(bucklingThreshold)} NOT TRIGGERED; " +
            $"Wel_OPPO={F(welCatalog)} mm3 Wel_declared={F(welDeclared)} mm3 " +
            $"Wpl_declared={(e.DeclaredStrongPlasticModulusMm3 is { } wp ? F(wp) : "not required for class 3")}; " +
            $"Aw_web_bound_used={(awUsed is { } aw ? F(aw) : "not needed for low V")} mm2 " +
            $"Aw_web_declared={(e.DeclaredGeometricWebAreaMm2 is { } ad ? F(ad) : "not needed for low V")} mm2; " +
            $"rho={F(rho)}; McRd_pure={F(pureMomentRd)} kNm " +
            $"MV_Rd={F(momentRd)} kNm MEd={F(mEd)} kNm Mutil={F(mRatio)}; " +
            $"combined=max(Vutil,Mutil)={F(combinedRatio)} ratio; " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(s.PositionMetres)} m side={side} " +
            $"signed_N={F(n)} kN signed_Vy={F(v)} kN signed_Mz={F(m)} kNm; " +
            $"holeEvidence={e.HoleStateEvidence}; geometryEvidence={e.SectionGeometryEvidence}; " +
            $"fyEvidence={e.MaterialYieldEvidence}; actionEvidence={e.CombinedActionEvidence}; " +
            $"AvEvidence={e.WebShearAreaEvidence}; WelEvidence={e.ElasticModulusEvidence}; " +
            $"WplEvidence={e.PlasticModulusEvidence ?? "not required"}; " +
            $"AwEvidence={e.WebAreaEvidence ?? "not required for low V"}; OPPO={p.SourcePageUrl}; " +
            "OPPO NOMINAL, NON CERTIFICATO; NO MEMBER/LATERAL INSTABILITY OR TORSION, " +
            "NO WEB SHEAR BUCKLING RESISTANCE, N-V OR N-M-V, CONNECTIONS/HOLES, " +
            "NO OTHER STATIONS/COMBINATIONS OR WHOLE STRUCTURE COMPLIANCE.";

        return DesignRuleEvaluation.Calculated(combinedRatio, 1d, "ratio", trace);
    }

    private static string Key(string member, string analysis, string combination,
        double x, DesignEffectSectionSide side) =>
        $"{member.Length}:{member}|{analysis.Length}:{analysis}|{combination.Length}:{combination}|" +
        $"{BitConverter.DoubleToInt64Bits(x == 0 ? 0d : x):X16}|{(int)side}";

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
