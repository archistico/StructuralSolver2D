using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Independent physical and nominal-area evidence for ONE exact FEM station.
/// Av has units of mm2. This record is not a product certificate or an
/// automatic assertion of shear-buckling, connection or global compliance.
/// </summary>
public sealed record Ntc2018SteelPureShearEvidenceD2C1(
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
    bool? FemLocalVyActsInWebPlaneConfirmed,
    bool? UnstiffenedWebConfirmed,
    bool? OtherSpatialActionsAndTorsionExcludedConfirmed,
    string? SectionGeometryEvidence,
    string? MaterialYieldEvidence,
    string? PureActionEvidence,
    string? ShearAreaEvidence,
    double? DeclaredWebPlaneShearAreaMm2);

/// <summary>
/// NTC 2018 §4.2.4.1.2.4 Eqs [4.2.16]-[4.2.18] native pure-web-shear
/// sectional resistance, only if the conservative UNSTIFFENED web screen
/// Eq [4.2.27], eta=1, does not trigger shear-buckling assessment.
/// It does not check shear buckling, torsion, moment/shear interaction or
/// member compliance. The web screen is an applicability gate, not a new
/// structural resistance formula.
/// </summary>
public sealed class Ntc2018SteelPureShearResistanceRuleD2C1 : IDesignCheckRule
{
    public const string Id = "NTC2018-STEEL-PURE-WEB-SHEAR-SECTION-D2C1";
    public const string Clause = "NTC 2018, 4.2.4.1.2.4, Eqs. [4.2.16]-[4.2.18], [4.2.27]";
    private const double ConservativeEta = 1d;

    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, Ntc2018SteelPureShearEvidenceD2C1> evidence;

    public Ntc2018SteelPureShearResistanceRuleD2C1(
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<Ntc2018SteelPureShearEvidenceD2C1> evidence)
    {
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(evidence);
        var index = new Dictionary<string, Ntc2018SteelPureShearEvidenceD2C1>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in evidence)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.MemberId) ||
                string.IsNullOrWhiteSpace(item.SectionId) || string.IsNullOrWhiteSpace(item.SteelGrade) ||
                string.IsNullOrWhiteSpace(item.AnalysisId) || string.IsNullOrWhiteSpace(item.CombinationId) ||
                !double.IsFinite(item.PositionMetres) || item.PositionMetres < 0 ||
                !Enum.IsDefined(item.SectionSide))
                throw new ArgumentException("D2C1 proof requires exact FEM identity and finite nonnegative station.", nameof(evidence));
            if (!index.TryAdd(Key(item.MemberId, item.AnalysisId, item.CombinationId,
                    item.PositionMetres, item.SectionSide), item))
                throw new ArgumentException("Duplicate shear proof for the same one-sided FEM station.", nameof(evidence));
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
            return DesignRuleEvaluation.MissingData("Exact NTC 2018 ordinary-member gamma factors and clauses required; no EC3 substitution.");
        if (request.Member.Material != Material || request.LimitState != LimitState)
            return DesignRuleEvaluation.NotApplicable("Only NTC 2018 steel sections at SLU.");

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
            return DesignRuleEvaluation.MissingData("Exact signed, one-sided FEM combination and station are mandatory.");
        if (s.AxialForceKn is not double n || s.ShearForceYKn is not double v ||
            s.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Signed simultaneous N/V/M required; null does not mean zero.");
        if (n != 0d || m != 0d || v == 0d)
            return DesignRuleEvaluation.NotApplicable("D2C1 only evaluates strictly pure Vy, N=Mz=0 at one FEM station.");

        if (!evidence.TryGetValue(Key(request.Member.MemberId, s.AnalysisId, s.CombinationId,
                s.PositionMetres, side), out var e))
            return DesignRuleEvaluation.MissingData("No independently documented proof for this member/analysis/combination/station/side.");
        if (!string.Equals(e.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Declared physical section differs from selected FEM profile.");
        if (e.HoleState != Ntc2018SteelHoleStateD1.NoRelevantHoles ||
            string.IsNullOrWhiteSpace(e.HoleStateEvidence) ||
            e.HotRolledParallelFlangeAndDoublySymmetricConfirmed != true ||
            e.FemLocalVyActsInWebPlaneConfirmed != true ||
            e.UnstiffenedWebConfirmed != true ||
            e.OtherSpatialActionsAndTorsionExcludedConfirmed != true ||
            string.IsNullOrWhiteSpace(e.SectionGeometryEvidence) ||
            string.IsNullOrWhiteSpace(e.MaterialYieldEvidence) ||
            string.IsNullOrWhiteSpace(e.PureActionEvidence) ||
            string.IsNullOrWhiteSpace(e.ShearAreaEvidence))
            return DesignRuleEvaluation.MissingData("Require documented unperforated hot-rolled I/H, unstiffened web, correct local Vy/web axis, grade, area and excluded torsion/spatial actions.");

        ResolvedRolledSteelSectionM88B2 section;
        try { section = sections.Find(e.SectionId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Declared section absent from nominal OPPO catalog."); }
        var p = section.Definition;
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            return DesignRuleEvaluation.NotApplicable("Only hot-rolled parallel-flange IPE/HEA/HEB; tapered INP excluded.");

        double fy;
        try { fy = materials.GetSteelYieldMpa(e.SteelGrade, Math.Max(p.WebThicknessMm, p.FlangeThicknessMm)); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Declared material grade absent from nominal strength catalog."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.NotApplicable("Material/yield thickness band unsupported."); }
        catch (ArgumentException) { return DesignRuleEvaluation.MissingData("Material thickness is invalid."); }

        double totalArea = p.AreaCm2 * 100d;
        // Literal NTC [4.2.18] for web-plane shear in rolled I/H sections.
        double avNtc = totalArea - 2d * p.WidthMm * p.FlangeThicknessMm +
            (p.WebThicknessMm + 2d * p.RootRadiusMm) * p.FlangeThicknessMm;
        if (!double.IsFinite(avNtc) || avNtc <= 0 || avNtc > totalArea ||
            !double.IsFinite(fy) || fy <= 0)
            return DesignRuleEvaluation.MissingData("Invalid nominal NTC shear area or material strength.");
        if (e.DeclaredWebPlaneShearAreaMm2 is not double declared || !double.IsFinite(declared) ||
            declared <= 0 || declared > totalArea ||
            Math.Abs(declared - avNtc) > avNtc * 0.001d)
            return DesignRuleEvaluation.MissingData("Independently stated Av,web [mm2] must match NTC [4.2.18] within 0.1%; no silent substitution.");

        // Conservative bounding web-panel height for unstiffened rolled I/H:
        // h-2*tf encloses the actual flat web and all root fillet regions.
        // Avoid the 72/eta shear-buckling trigger [4.2.27] with eta=1.00.
        double hwUpper = p.HeightMm - 2d * p.FlangeThicknessMm;
        double webSlendernessUpper = hwUpper / p.WebThicknessMm;
        double epsilon = Math.Sqrt(235d / fy);
        double trigger = 72d / ConservativeEta * epsilon;
        if (!double.IsFinite(webSlendernessUpper) || !double.IsFinite(trigger) ||
            hwUpper <= 0 || webSlendernessUpper <= 0 || trigger <= 0)
            return DesignRuleEvaluation.MissingData("Unsupported web geometry or NTC shear-buckling screen values.");
        if (webSlendernessUpper > trigger)
            return DesignRuleEvaluation.NotApplicable("Unstiffened-web shear-buckling assessment NTC [4.2.27]/4.2.4.1.3.4 is triggered; D2C1 does not calculate it.");

        // Independent declared area may be rounded; use the smaller nominal Av.
        double avUsed = Math.Min(avNtc, declared);
        double capacity = avUsed * fy / (Math.Sqrt(3d) * Ntc2018SteelTensionProfileD1.GammaM0) / 1000d;
        double demand = Math.Abs(v);
        if (!double.IsFinite(capacity) || capacity <= 0 || !double.IsFinite(demand))
            return DesignRuleEvaluation.MissingData("Finite positive Vc,Rd and finite demand are mandatory.");

        string trace = $"SECTION ONLY; {SourceClause}; member={e.MemberId} section={e.SectionId} grade={e.SteelGrade}; " +
            $"A_OPPO={F(totalArea)} mm2; b={F(p.WidthMm)} mm tf={F(p.FlangeThicknessMm)} mm " +
            $"tw={F(p.WebThicknessMm)} mm r={F(p.RootRadiusMm)} mm; " +
            $"Av_NTC={F(avNtc)} mm2 Av_declared={F(declared)} mm2 Av_used_min={F(avUsed)} mm2; " +
            $"fy={F(fy)} MPa gammaM0={F(Ntc2018SteelTensionProfileD1.GammaM0)} " +
            $"({Ntc2018SteelTensionProfileD1.FactorClause}); " +
            $"Vc,Rd={F(capacity)} kN; FEM: N={F(n)} kN Vy={F(v)} kN Mz={F(m)} kNm; " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(s.PositionMetres)} m side={side}; " +
            $"shearBucklingScreen: hw_upper=h-2tf={F(hwUpper)} mm, hw_upper/tw={F(webSlendernessUpper)}, " +
            $"eta={F(ConservativeEta)}, epsilon={F(epsilon)}, threshold_72eps={F(trigger)}, " +
            $"NOT TRIGGERED under NTC [4.2.27]; " +
            $"holeEvidence={e.HoleStateEvidence}; geometryEvidence={e.SectionGeometryEvidence}; " +
            $"fyEvidence={e.MaterialYieldEvidence}; actionEvidence={e.PureActionEvidence}; " +
            $"shearAreaEvidence={e.ShearAreaEvidence}; OPPO={p.SourcePageUrl}; " +
            "OPPO NOMINAL, NON CERTIFICATO; NO TORSION OR SHEAR BUCKLING SOLUTION, NO M-V OR N-V INTERACTION, " +
            "NO HOLE/CONNECTION/WEB-PANEL EFFECTS, NO OUT-OF-PLANE OR MEMBER INSTABILITY, " +
            "NO OTHER STATIONS/COMBINATIONS OR STRUCTURE COMPLIANCE; " +
            "NOT COMPLETE MEMBER OR STRUCTURE COMPLIANCE.";
        return DesignRuleEvaluation.Calculated(demand, capacity, "kN", trace);
    }

    private static string Key(string member, string analysis, string combination,
        double x, DesignEffectSectionSide side) =>
        $"{member.Length}:{member}|{analysis.Length}:{analysis}|{combination.Length}:{combination}|" +
        $"{BitConverter.DoubleToInt64Bits(x == 0 ? 0d : x):X16}|{(int)side}";

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
