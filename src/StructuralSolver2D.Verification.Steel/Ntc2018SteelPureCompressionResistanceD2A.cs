using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Designer's independent evidence for one exact FEM station. A nominal OPPO
/// catalog section is not a certified product; the physical member and grade
/// cannot be inferred from its FEM member ID.
/// </summary>
public sealed record Ntc2018SteelPureCompressionEvidenceD2A(
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
    bool? ConcentricCompressionConfirmed,
    bool? OtherSpatialActionsAndTorsionExcludedConfirmed,
    string? SectionGeometryEvidence,
    string? GrossAreaEvidence,
    double? DeclaredGrossAreaMm2,
    string? MaterialYieldEvidence,
    string? PureActionEvidence);

/// <summary>
/// NTC2018 [4.2.9]-[4.2.10] compression resistance of ONE unperforated
/// IPE/HEA/HEB section of class 1, 2 or 3. NO buckling check and NO member
/// compliance; gamma_M1 must never be substituted for gamma_M0 here.
/// </summary>
public sealed class Ntc2018SteelPureCompressionResistanceRuleD2A : IDesignCheckRule
{
    public const string Id = "NTC2018-STEEL-PURE-COMPRESSION-SECTION-D2A";
    public const string Clause = "NTC 2018, 4.2.4.1.2.2, Eqs. [4.2.9]-[4.2.10] (classi 1-3)";

    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, Ntc2018SteelPureCompressionEvidenceD2A> evidence;

    public Ntc2018SteelPureCompressionResistanceRuleD2A(
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<Ntc2018SteelPureCompressionEvidenceD2A> evidence)
    {
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(evidence);
        var index = new Dictionary<string, Ntc2018SteelPureCompressionEvidenceD2A>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in evidence)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.MemberId) ||
                string.IsNullOrWhiteSpace(item.SectionId) || string.IsNullOrWhiteSpace(item.SteelGrade) ||
                string.IsNullOrWhiteSpace(item.AnalysisId) || string.IsNullOrWhiteSpace(item.CombinationId) ||
                !double.IsFinite(item.PositionMetres) || item.PositionMetres < 0 ||
                !Enum.IsDefined(item.SectionSide))
                throw new ArgumentException("Evidence requires a finite nonnegative station and exact FEM/section identity.", nameof(evidence));
            if (!index.TryAdd(Key(item.MemberId, item.AnalysisId, item.CombinationId,
                    item.PositionMetres, item.SectionSide), item))
                throw new ArgumentException("Duplicate NTC compression evidence for one FEM member/station/side.", nameof(evidence));
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
            return DesignRuleEvaluation.MissingData("The exact NTC 2018 ordinary-member coefficients and source are required; no EC3 fallback.");
        if (request.Member.Material != Material || request.LimitState != LimitState)
            return DesignRuleEvaluation.NotApplicable("Only an NTC steel section at SLU is covered.");
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
            return DesignRuleEvaluation.MissingData("Exact one-sided FEM combination, member and station are required.");
        if (s.AxialForceKn is not double n || s.ShearForceYKn is not double v ||
            s.BendingMomentZKnm is not double m || !double.IsFinite(n) ||
            !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Signed concomitant N/V/M required; null is not zero.");
        if (!(n < 0) || v != 0d || m != 0d)
            return DesignRuleEvaluation.NotApplicable("Pure concentric compression requires N<0, V=0, M=0 at the same station.");

        if (!evidence.TryGetValue(Key(request.Member.MemberId, s.AnalysisId, s.CombinationId,
                s.PositionMetres, side), out var e))
            return DesignRuleEvaluation.MissingData("No independent proof for this exact member/analysis/combination/station/side.");
        if (!string.Equals(e.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Declared section differs from the selected FEM member.");
        if (e.HoleState != Ntc2018SteelHoleStateD1.NoRelevantHoles ||
            string.IsNullOrWhiteSpace(e.HoleStateEvidence) ||
            e.HotRolledParallelFlangeAndDoublySymmetricConfirmed != true ||
            e.ConcentricCompressionConfirmed != true ||
            e.OtherSpatialActionsAndTorsionExcludedConfirmed != true ||
            string.IsNullOrWhiteSpace(e.SectionGeometryEvidence) ||
            string.IsNullOrWhiteSpace(e.GrossAreaEvidence) ||
            string.IsNullOrWhiteSpace(e.MaterialYieldEvidence) ||
            string.IsNullOrWhiteSpace(e.PureActionEvidence))
            return DesignRuleEvaluation.MissingData("Unperforated nominal I/H geometry, area, strength, concentricity and excluded spatial actions must be independently documented.");

        Ntc2018SteelISectionClassificationD0 classified;
        try
        {
            classified = Ntc2018SteelISectionClassifierD0.ClassifyCatalogProfile(Code,
                sections, materials, e.SectionId, e.SteelGrade,
                Ntc2018SteelPureActionD0.UniformCompression,
                new Ntc2018SteelClassificationEvidenceD0(
                    UnperforatedSectionConfirmed: true,
                    HotRolledParallelFlangeAndDoublySymmetricConfirmed: true,
                    StrongAxisOfBendingConfirmed: null,
                    SectionGeometryEvidence: e.SectionGeometryEvidence,
                    MaterialYieldEvidence: e.MaterialYieldEvidence,
                    PureActionEvidence: e.PureActionEvidence));
        }
        catch (KeyNotFoundException)
        {
            return DesignRuleEvaluation.MissingData("Unknown catalog profile or material grade.");
        }
        catch (NotSupportedException)
        {
            return DesignRuleEvaluation.NotApplicable("Only supported hot-rolled parallel-flange IPE, HEA, HEB with catalog material bands.");
        }
        catch (ArgumentException)
        {
            return DesignRuleEvaluation.MissingData("Catalog geometry or material input is invalid.");
        }

        if (classified.Class == 4)
            return DesignRuleEvaluation.NotApplicable("Class 4: NTC [4.2.10] requires Aeff, which D2A does not calculate; no gross-area substitute.");
        if (classified.Class is < 1 or > 3 || classified.Action != Ntc2018SteelPureActionD0.UniformCompression)
            return DesignRuleEvaluation.MissingData("Classification result outside the D2A pure-compression scope.");

        ResolvedRolledSteelSectionM88B2 section;
        try { section = sections.Find(e.SectionId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Catalog section no longer found."); }
        double area = section.Definition.AreaCm2 * 100d;
        double fy = classified.YieldStrengthMpa;
        if (!double.IsFinite(area) || area <= 0 || !double.IsFinite(fy) || fy <= 0)
            return DesignRuleEvaluation.MissingData("Nonpositive/nonfinite gross area or yield strength.");
        if (e.DeclaredGrossAreaMm2 is not double declared || !double.IsFinite(declared) || declared <= 0 ||
            Math.Abs(declared - area) > 0.001d * area)
            return DesignRuleEvaluation.MissingData("Independent nominal gross area in mm2 must agree with the indicative OPPO catalog to 0.1%; no silent area substitution.");
        // Where minor documented rounding differs, select the smaller nominal
        // area. This is not a substitute for a certified product section.
        double areaUsed = Math.Min(area, declared);
        double capacity = areaUsed * fy / Ntc2018SteelTensionProfileD1.GammaM0 / 1000d;
        double demand = -n;
        if (!double.IsFinite(capacity) || capacity <= 0 || !double.IsFinite(demand) || demand < 0)
            return DesignRuleEvaluation.MissingData("Axial resistance or demand is outside supported numerical limits.");

        string trace = $"SECTION ONLY; {SourceClause}; member={e.MemberId} section={e.SectionId} grade={e.SteelGrade}; " +
            $"class={classified.Class} flange={classified.Flange.Class} web={classified.Web.Class}; " +
            $"A_OPPO={F(area)} mm2 A_declared={F(declared)} mm2 A_used_min={F(areaUsed)} mm2 " +
            $"fy={F(fy)} MPa; gammaM0={F(Ntc2018SteelTensionProfileD1.GammaM0)} " +
            $"({Ntc2018SteelTensionProfileD1.FactorClause}); N_c,Rd={F(capacity)} kN; " +
            $"N_FEM={F(n)} kN (negative=compression) V={F(v)} kN M={F(m)} kNm; " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(s.PositionMetres)} m side={side}; " +
            $"holeEvidence={e.HoleStateEvidence}; geometryEvidence={e.SectionGeometryEvidence}; " +
            $"grossAreaEvidence={e.GrossAreaEvidence}; fyEvidence={e.MaterialYieldEvidence}; " +
            $"actionEvidence={e.PureActionEvidence}; classSource={Ntc2018SteelISectionClassifierD0.Clause}; " +
            $"web_c/t={F(classified.Web.WidthThicknessRatio)} " +
            $"flange_c/t={F(classified.Flange.WidthThicknessRatio)} " +
            $"epsilon={F(classified.Epsilon)}; " +
            $"OPPO NOMINAL, NON CERTIFICATO; NO MEMBER BUCKLING (NTC 4.2.4.1.3), NO P-DELTA, " +
            "NO SPATIAL INSTABILITY, CONNECTIONS, INTERACTIONS OR OTHER SECTIONS/COMBINATIONS; " +
            "NOT COMPLETE MEMBER OR STRUCTURE COMPLIANCE.";
        return DesignRuleEvaluation.Calculated(demand, capacity, "kN", trace);
    }

    private static string Key(string member, string analysis, string combination,
        double x, DesignEffectSectionSide side) =>
        $"{member.Length}:{member}|{analysis.Length}:{analysis}|{combination.Length}:{combination}|" +
        $"{BitConverter.DoubleToInt64Bits(x == 0 ? 0d : x):X16}|{(int)side}";

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
