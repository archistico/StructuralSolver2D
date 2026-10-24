using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Information declared by the designer, NOT inferred from a 2D FEM model.
/// All resistance rules in M89-B are isolated PURE-action cross-section checks.
/// "NoRelevantHolesConfirmed" is not a replacement for net-section fracture or
/// a connection check. "ShearBucklingExcludedConfirmed" is external evidence,
/// not an implementation of EN 1993-1-5.
/// </summary>
public sealed record SteelBasicSectionAssignmentM89B(
    string MemberId,
    string SectionId,
    string SteelGrade,
    int? SectionClass = null,
    string? ClassificationEvidence = null,
    bool? NoRelevantHolesConfirmed = null,
    double? ShearAreaCm2 = null,
    string? ShearAreaEvidence = null,
    bool? InPlaneShearAxisMappingConfirmed = null,
    bool? ShearBucklingExcludedConfirmed = null,
    bool? NoTorsionConfirmed = null);

/// <summary>
/// Shared catalog and provenance safeguards for three separately identifiable
/// EN 1993-1-1:2005+A1:2014, single-station PURE-action checks.
/// They never establish complete member or code compliance.
/// </summary>
public abstract class SteelBasicSectionRuleM89B : IDesignCheckRule
{
    public const string SupportedEdition = "2005+A1:2014";
    public const string GammaId = "gamma_M0";

    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, SteelBasicSectionAssignmentM89B> assignments;

    protected SteelBasicSectionRuleM89B(
        DesignCodeEdition code,
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<SteelBasicSectionAssignmentM89B> assignments)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.DocumentId != "EN 1993-1-1" || code.EditionId != SupportedEdition ||
            string.IsNullOrWhiteSpace(code.NationalAnnexId))
            throw new NotSupportedException("M89-B supports only EN 1993-1-1:2005+A1:2014 with an explicitly identified national annex; not 2022 or NTC.");
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(assignments);
        var map = new Dictionary<string, SteelBasicSectionAssignmentM89B>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in assignments)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.MemberId) ||
                string.IsNullOrWhiteSpace(item.SectionId) || string.IsNullOrWhiteSpace(item.SteelGrade))
                throw new ArgumentException("Member, section and steel grade are required.", nameof(assignments));
            if (!map.TryAdd(item.MemberId, item))
                throw new ArgumentException("Duplicate member assignment.", nameof(assignments));
        }
        this.assignments = map;
        Code = code;
    }

    public abstract string RuleId { get; }
    public DesignCodeEdition Code { get; }
    public DesignMaterial Material => DesignMaterial.Steel;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public abstract string SourceClause { get; }
    public bool EstablishesCompleteMemberCompliance => false;

    protected abstract DesignRuleEvaluation EvaluateAction(
        DesignCheckRequest request, SteelBasicSectionAssignmentM89B assignment,
        PublishedRolledSteelSectionM88B2 section, double fyMpa, double gammaM0);

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!assignments.TryGetValue(request.Member.MemberId, out var a))
            return DesignRuleEvaluation.MissingData("No M89-B steel member assignment.");
        if (!string.Equals(a.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Member section does not match the declared steel assignment.");
        if (request.Effects.AxialForceKn is not double n ||
            request.Effects.ShearForceYKn is not double v ||
            request.Effects.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Signed concomitant N, V, M at one station are required.");
        var gammaEntry = request.Profile.Coefficients?.SingleOrDefault(c =>
            string.Equals(c.Id, GammaId, StringComparison.OrdinalIgnoreCase));
        if (gammaEntry is null || !double.IsFinite(gammaEntry.Value) || gammaEntry.Value <= 0 ||
            string.IsNullOrWhiteSpace(gammaEntry.SourceClause))
            return DesignRuleEvaluation.MissingData("gamma_M0 must be positive and have an explicit national-annex/source reference; no default.");
        ResolvedRolledSteelSectionM88B2 section;
        try { section = sections.Find(a.SectionId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown M88-B2 rolled profile."); }
        double thickness = Math.Max(section.Definition.FlangeThicknessMm, section.Definition.WebThicknessMm);
        double fy;
        try { fy = materials.GetSteelYieldMpa(a.SteelGrade, thickness); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown M88-C steel grade."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.MissingData("Undocumented steel nominal-thickness band."); }
        if (!(section.Definition.AreaCm2 > 0) || !double.IsFinite(section.Definition.AreaCm2) ||
            !(fy > 0) || !double.IsFinite(fy))
            return DesignRuleEvaluation.MissingData("Invalid catalog steel area or yield strength.");
        return EvaluateAction(request, a, section.Definition, fy, gammaEntry.Value);
    }

    protected static bool PureAction(DesignCheckRequest request, bool axial) => axial
        ? request.Effects.ShearForceYKn == 0d && request.Effects.BendingMomentZKnm == 0d
        : request.Effects.AxialForceKn == 0d && request.Effects.BendingMomentZKnm == 0d;

    protected string Trace(DesignCheckRequest request, SteelBasicSectionAssignmentM89B assignment,
        PublishedRolledSteelSectionM88B2 section, double fy, double gamma, string data)
    {
        static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
        string gammaSource = request.Profile.Coefficients.Single(c =>
            string.Equals(c.Id, GammaId, StringComparison.OrdinalIgnoreCase)).SourceClause;
        return $"SINGLE cross-section / partial resistance only | {SourceClause} | " +
            $"member={request.Member.MemberId} section={section.Id} grade={assignment.SteelGrade} | " +
            $"OPPO={section.SourcePageUrl} status={section.DataStatus} (NOT certified) | " +
            $"A={F(section.AreaCm2)} cm2; t=max(tf,tw)={F(Math.Max(section.FlangeThicknessMm, section.WebThicknessMm))} mm; " +
            $"fy={F(fy)} MPa (M88-C, EN 10025-2:2019); gamma_M0={F(gamma)} source={gammaSource} | {data} | " +
            $"N={F(request.Effects.AxialForceKn!.Value)} kN V={F(request.Effects.ShearForceYKn!.Value)} kN " +
            $"M={F(request.Effects.BendingMomentZKnm!.Value)} kNm | " +
            $"analysis={request.Effects.AnalysisId} target={request.Effects.CombinationId} " +
            $"x={F(request.Effects.PositionMetres)} m side={request.Effects.SectionSide} | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId} | " +
            "NOT checked: combined actions, net-section fracture, member buckling, lateral buckling, connections, torsion or overall code compliance.";
    }
}

/// <summary>
/// EN 1993-1-1:2005, §6.2.3(2)(a), Eq. 6.6. ONLY gross-area yield in
/// pure tension. Nt,Rd=min(Npl,Rd,Nu,Rd) is NOT evaluated because fu, Anet,
/// gamma_M2 and connection details are outside this milestone.
/// </summary>
public sealed class SteelGrossTensionYieldRuleM89B : SteelBasicSectionRuleM89B
{
    public const string Id = "EC3-2005-PURE-TENSION-GROSS-YIELD-M89B";
    public const string Clause = "EN 1993-1-1:2005+A1:2014, 6.2.3(2)(a), Eq. (6.6)";
    public SteelGrossTensionYieldRuleM89B(DesignCodeEdition code, CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials, IEnumerable<SteelBasicSectionAssignmentM89B> assignments)
        : base(code, sections, materials, assignments) { }
    public override string RuleId => Id;
    public override string SourceClause => Clause;
    protected override DesignRuleEvaluation EvaluateAction(DesignCheckRequest r,
        SteelBasicSectionAssignmentM89B a, PublishedRolledSteelSectionM88B2 s, double fy, double gamma)
    {
        if (!PureAction(r, axial: true))
            return DesignRuleEvaluation.NotApplicable("M89-B gross-tension-yield requires pure axial action: V=M=0.");
        double n = r.Effects.AxialForceKn!.Value;
        if (n <= 0) return DesignRuleEvaluation.NotApplicable("No positive (tensile) axial action at this station.");
        // Yielding of the GROSS area is only one of the required tensile checks.
        double nr = s.AreaCm2 * fy * 0.1 / gamma; // cm2 * MPa -> 0.1 kN
        return DesignRuleEvaluation.Calculated(n, nr, "kN",
            Trace(r, a, s, fy, gamma, $"Npl,Rd={nr.ToString("G17", CultureInfo.InvariantCulture)} kN (GROSS YIELD ONLY; Nu,Rd NET RUPTURE NOT checked)"));
    }
}

/// <summary>
/// EN 1993-1-1:2005, §6.2.4(2), Eq. 6.10, gross cross-section compression
/// only, classes 1-3. Global in-/out-of-plane member buckling is NOT evaluated.
/// </summary>
public sealed class SteelGrossCompressionRuleM89B : SteelBasicSectionRuleM89B
{
    public const string Id = "EC3-2005-PURE-COMPRESSION-GROSS-SECTION-M89B";
    public const string Clause = "EN 1993-1-1:2005+A1:2014, 6.2.4(2), Eq. (6.10)";
    public SteelGrossCompressionRuleM89B(DesignCodeEdition code, CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials, IEnumerable<SteelBasicSectionAssignmentM89B> assignments)
        : base(code, sections, materials, assignments) { }
    public override string RuleId => Id;
    public override string SourceClause => Clause;
    protected override DesignRuleEvaluation EvaluateAction(DesignCheckRequest r,
        SteelBasicSectionAssignmentM89B a, PublishedRolledSteelSectionM88B2 s, double fy, double gamma)
    {
        if (!PureAction(r, axial: true))
            return DesignRuleEvaluation.NotApplicable("M89-B compression requires pure axial action: V=M=0.");
        double n = r.Effects.AxialForceKn!.Value;
        if (n >= 0) return DesignRuleEvaluation.NotApplicable("No negative (compressive) axial action at this station.");
        if (a.SectionClass is null || string.IsNullOrWhiteSpace(a.ClassificationEvidence))
            return DesignRuleEvaluation.MissingData("Independent section-class evidence required for compression.");
        if (a.SectionClass is < 1 or > 3)
            return DesignRuleEvaluation.NotApplicable("Only section classes 1-3 supported; class 4 requires Aeff.");
        if (a.NoRelevantHolesConfirmed != true)
            return DesignRuleEvaluation.MissingData("Gross section requires explicit confirmation of no relevant holes; filled-hole exceptions not implemented.");
        double nr = s.AreaCm2 * fy * 0.1 / gamma;
        return DesignRuleEvaluation.Calculated(-n, nr, "kN",
            Trace(r, a, s, fy, gamma, $"Nc,Rd={nr.ToString("G17", CultureInfo.InvariantCulture)} kN; " +
                $"class={a.SectionClass}; classEvidence={a.ClassificationEvidence}; NO member buckling check"));
    }
}

/// <summary>
/// EN 1993-1-1:2005, §6.2.6(2), Eq. 6.18, plastic PURE shear with declared,
/// independently justified Av. No automatic Av, eta, EN 1993-1-5 shear buckling
/// or torsion model. This result NEVER checks the complete member.
/// </summary>
public sealed class SteelPureShearRuleM89B : SteelBasicSectionRuleM89B
{
    public const string Id = "EC3-2005-PURE-SHEAR-PLASTIC-M89B";
    public const string Clause = "EN 1993-1-1:2005+A1:2014, 6.2.6(2), Eq. (6.18)";
    public SteelPureShearRuleM89B(DesignCodeEdition code, CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials, IEnumerable<SteelBasicSectionAssignmentM89B> assignments)
        : base(code, sections, materials, assignments) { }
    public override string RuleId => Id;
    public override string SourceClause => Clause;
    protected override DesignRuleEvaluation EvaluateAction(DesignCheckRequest r,
        SteelBasicSectionAssignmentM89B a, PublishedRolledSteelSectionM88B2 s, double fy, double gamma)
    {
        if (!PureAction(r, axial: false))
            return DesignRuleEvaluation.NotApplicable("M89-B shear requires pure shear: N=M=0.");
        if (a.ShearAreaCm2 is not double av || !double.IsFinite(av) || av <= 0 || av > s.AreaCm2 ||
            string.IsNullOrWhiteSpace(a.ShearAreaEvidence))
            return DesignRuleEvaluation.MissingData("Positive, independently justified Av (cm2) <= A and evidence required; no derived/default area.");
        if (a.InPlaneShearAxisMappingConfirmed != true)
            return DesignRuleEvaluation.MissingData("FEM local Vy direction versus section shear area must be confirmed.");
        if (a.NoTorsionConfirmed != true)
            return DesignRuleEvaluation.MissingData("Absence of torsion cannot be inferred from a 2D model.");
        if (a.ShearBucklingExcludedConfirmed != true)
            return DesignRuleEvaluation.MissingData("Shear buckling must be excluded by external evidence (EN 1993-1-5 not implemented).");
        if (a.NoRelevantHolesConfirmed != true)
            return DesignRuleEvaluation.MissingData("Relevant holes and net shear-area reductions not supported.");
        double vr = av * 0.1 * fy / (Math.Sqrt(3) * gamma);
        return DesignRuleEvaluation.Calculated(Math.Abs(r.Effects.ShearForceYKn!.Value), vr, "kN",
            Trace(r, a, s, fy, gamma,
                $"Av={av.ToString("G17", CultureInfo.InvariantCulture)} cm2; AvEvidence={a.ShearAreaEvidence}; " +
                $"Vpl,Rd={vr.ToString("G17", CultureInfo.InvariantCulture)} kN; " +
                "pure shear, no torsion and shear buckling externally excluded"));
    }
}
