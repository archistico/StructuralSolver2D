using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Physical and geometric assertions by the designer for ONE FEM section side.
/// Wel and Wpl are independent, externally documented nominal section properties
/// in mm3; neither is manufactured from the FEM or calculated from a catalog sketch.
/// </summary>
public sealed record Ntc2018SteelPureBendingEvidenceD2B(
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
    bool? FemLocalMzMatchesStrongAxisConfirmed,
    bool? OtherSpatialActionsAndTorsionExcludedConfirmed,
    string? SectionGeometryEvidence,
    string? MaterialYieldEvidence,
    string? PureActionEvidence,
    string? ElasticModulusEvidence,
    double? DeclaredStrongElasticModulusMm3,
    string? PlasticModulusEvidence,
    double? DeclaredStrongPlasticModulusMm3);

/// <summary>
/// NTC 2018 4.2.4.1.2.3 [4.2.11]-[4.2.14]: pure major-axis sectional bending.
/// Class 1/2: externally documented Wpl; class 3: documented Wel,min; class 4:
/// NotApplicable without Weff. No lateral-torsional buckling or member compliance.
/// </summary>
public sealed class Ntc2018SteelPureBendingResistanceRuleD2B : IDesignCheckRule
{
    public const string Id = "NTC2018-STEEL-PURE-STRONG-BENDING-SECTION-D2B";
    public const string Clause = "NTC 2018, 4.2.4.1.2.3, Eqs. [4.2.11]-[4.2.14]";

    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, Ntc2018SteelPureBendingEvidenceD2B> evidence;

    public Ntc2018SteelPureBendingResistanceRuleD2B(
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<Ntc2018SteelPureBendingEvidenceD2B> evidence)
    {
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(evidence);
        var index = new Dictionary<string, Ntc2018SteelPureBendingEvidenceD2B>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in evidence)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.MemberId) ||
                string.IsNullOrWhiteSpace(item.SectionId) || string.IsNullOrWhiteSpace(item.SteelGrade) ||
                string.IsNullOrWhiteSpace(item.AnalysisId) || string.IsNullOrWhiteSpace(item.CombinationId) ||
                !double.IsFinite(item.PositionMetres) || item.PositionMetres < 0 ||
                !Enum.IsDefined(item.SectionSide))
                throw new ArgumentException("D2B evidence requires exact FEM identity and a finite nonnegative station.", nameof(evidence));
            if (!index.TryAdd(Key(item.MemberId, item.AnalysisId, item.CombinationId,
                    item.PositionMetres, item.SectionSide), item))
                throw new ArgumentException("Duplicate bending proof for the same one-sided FEM station.", nameof(evidence));
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
            return DesignRuleEvaluation.MissingData("Exact ordinary-member NTC 2018 gamma factors and sources are mandatory; no EC3 fallback.");
        if (request.Member.Material != Material || request.LimitState != LimitState)
            return DesignRuleEvaluation.NotApplicable("Only one NTC steel section at SLU is covered.");

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
            s.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Signed concomitant N/V/M required; null is not zero.");
        if (n != 0d || v != 0d || m == 0d)
            return DesignRuleEvaluation.NotApplicable("Only nonzero Mz with strictly zero simultaneous N and Vy at one station.");

        if (!evidence.TryGetValue(Key(request.Member.MemberId, s.AnalysisId, s.CombinationId,
                s.PositionMetres, side), out var e))
            return DesignRuleEvaluation.MissingData("No independent proof for the exact FEM member/analysis/combination/station/side.");
        if (!string.Equals(e.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Declared profile differs from the selected FEM section.");
        if (e.HoleState != Ntc2018SteelHoleStateD1.NoRelevantHoles ||
            string.IsNullOrWhiteSpace(e.HoleStateEvidence) ||
            e.HotRolledParallelFlangeAndDoublySymmetricConfirmed != true ||
            e.FemLocalMzMatchesStrongAxisConfirmed != true ||
            e.OtherSpatialActionsAndTorsionExcludedConfirmed != true ||
            string.IsNullOrWhiteSpace(e.SectionGeometryEvidence) ||
            string.IsNullOrWhiteSpace(e.MaterialYieldEvidence) ||
            string.IsNullOrWhiteSpace(e.PureActionEvidence) ||
            string.IsNullOrWhiteSpace(e.ElasticModulusEvidence))
            return DesignRuleEvaluation.MissingData("Document unperforated I/H geometry, steel grade, pure major-axis bending, excluded spatial actions and Wel provenance.");

        Ntc2018SteelISectionClassificationD0 classified;
        try
        {
            classified = Ntc2018SteelISectionClassifierD0.ClassifyCatalogProfile(Code,
                sections, materials, e.SectionId, e.SteelGrade,
                Ntc2018SteelPureActionD0.StrongAxisBending,
                new Ntc2018SteelClassificationEvidenceD0(
                    UnperforatedSectionConfirmed: true,
                    HotRolledParallelFlangeAndDoublySymmetricConfirmed: true,
                    StrongAxisOfBendingConfirmed: true,
                    SectionGeometryEvidence: e.SectionGeometryEvidence,
                    MaterialYieldEvidence: e.MaterialYieldEvidence,
                    PureActionEvidence: e.PureActionEvidence));
        }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Catalog profile or steel grade not found."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.NotApplicable("Only catalog IPE/HEA/HEB hot-rolled parallel-flange geometry and supported grade bands."); }
        catch (ArgumentException) { return DesignRuleEvaluation.MissingData("Catalog dimensions or material strength are invalid."); }

        if (classified.Class == 4)
            return DesignRuleEvaluation.NotApplicable("Class 4 requires NTC Weff,min: no Wel/Wpl substitution or automatic effective-width model.");
        if (classified.Class is < 1 or > 3 ||
            classified.Action != Ntc2018SteelPureActionD0.StrongAxisBending)
            return DesignRuleEvaluation.MissingData("Classification is outside the D2B pure-bending scope.");

        ResolvedRolledSteelSectionM88B2 section;
        try { section = sections.Find(e.SectionId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Catalog section no longer present."); }
        var p = section.Definition;
        double welCatalog = p.StrongElasticModulusCm3 * 1000d;
        if (!double.IsFinite(welCatalog) || welCatalog <= 0 ||
            e.DeclaredStrongElasticModulusMm3 is not double welDeclared ||
            !double.IsFinite(welDeclared) || welDeclared <= 0 ||
            Math.Abs(welDeclared - welCatalog) > welCatalog * 0.001d)
            return DesignRuleEvaluation.MissingData("Independent Wel,strong [mm3] must match nominal OPPO Wx within 0.1%; no silent substitute.");

        double welUsed = Math.Min(welCatalog, welDeclared);
        double modulus;
        string branch;
        if (classified.Class is 1 or 2)
        {
            if (string.IsNullOrWhiteSpace(e.PlasticModulusEvidence) ||
                e.DeclaredStrongPlasticModulusMm3 is not double wpl ||
                !double.IsFinite(wpl) || wpl < welUsed)
                return DesignRuleEvaluation.MissingData("Class 1/2 requires independently sourced Wpl,strong [mm3] >= Wel; OPPO supplies only Wel.");
            // A completely loose but mathematically necessary geometrical bound:
            // integral |y| dA <= A*h/2 for a section wholly within its height.
            // NOT a product validation or a replacement for certified Wpl.
            double upperBound = p.AreaCm2 * 100d * p.HeightMm / 2d;
            if (!double.IsFinite(upperBound) || upperBound <= 0 || wpl > upperBound)
                return DesignRuleEvaluation.MissingData("Declared Wpl exceeds the gross-section A*h/2 physical bound.");
            modulus = wpl;
            branch = "MplRd-Wpl";
        }
        else
        {
            modulus = welUsed;
            branch = "MelRd-WelMin";
        }

        double fy = classified.YieldStrengthMpa;
        double capacity = modulus * fy / Ntc2018SteelTensionProfileD1.GammaM0 / 1_000_000d;
        double demand = Math.Abs(m);
        if (!double.IsFinite(capacity) || capacity <= 0 || !double.IsFinite(demand))
            return DesignRuleEvaluation.MissingData("Bending resistance or FEM demand outside finite numerical limits.");

        string trace = $"SECTION ONLY; {SourceClause}; member={e.MemberId} section={e.SectionId} grade={e.SteelGrade}; " +
            $"class={classified.Class} flange={classified.Flange.Class} web={classified.Web.Class} branch={branch}; " +
            $"Wel_OPPO={F(welCatalog)} mm3 Wel_declared={F(welDeclared)} mm3 Wel_used_min={F(welUsed)} mm3; " +
            $"Wpl_declared={(e.DeclaredStrongPlasticModulusMm3 is { } wp ? F(wp) : "not supplied")} mm3; " +
            $"W_design={F(modulus)} mm3; fy={F(fy)} MPa; " +
            $"gammaM0={F(Ntc2018SteelTensionProfileD1.GammaM0)} ({Ntc2018SteelTensionProfileD1.FactorClause}); " +
            $"M_c,Rd={F(capacity)} kNm; N={F(n)} kN V={F(v)} kN Mz_FEM={F(m)} kNm; " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(s.PositionMetres)} m side={side}; " +
            $"holeEvidence={e.HoleStateEvidence}; geometryEvidence={e.SectionGeometryEvidence}; " +
            $"fyEvidence={e.MaterialYieldEvidence}; actionEvidence={e.PureActionEvidence}; " +
            $"WelEvidence={e.ElasticModulusEvidence}; WplEvidence={e.PlasticModulusEvidence ?? "not required for class 3"}; " +
            $"classSource={Ntc2018SteelISectionClassifierD0.Clause}; " +
            $"OPPO={p.SourcePageUrl}; OPPO NOMINAL, NON CERTIFICATO; " +
            "NO LATERAL-TORSIONAL BUCKLING (NTC 4.2.4.1.3.2), NO MEMBER INSTABILITY, NO SPATIAL ACTIONS, " +
            "NO N/M/V INTERACTION, CONNECTIONS, HOLES, SECOND ORDER OR OTHER STATIONS/COMBINATIONS; " +
            "NOT COMPLETE MEMBER OR STRUCTURE COMPLIANCE.";
        return DesignRuleEvaluation.Calculated(demand, capacity, "kNm", trace);
    }

    private static string Key(string member, string analysis, string combination,
        double x, DesignEffectSectionSide side) =>
        $"{member.Length}:{member}|{analysis.Length}:{analysis}|{combination.Length}:{combination}|" +
        $"{BitConverter.DoubleToInt64Bits(x == 0 ? 0d : x):X16}|{(int)side}";

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
