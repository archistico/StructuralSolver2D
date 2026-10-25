using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>Designer-provided hole state: an unknown state cannot select the gross-area branch.</summary>
public enum Ntc2018SteelHoleStateD1 { Unknown, NoRelevantHoles, RelevantConnectionHoles }

/// <summary>
/// Independently documented sectional evidence, never inferred from planar FEM results.
/// Net area already accounts for the relevant hole pattern; its geometric derivation
/// and applicability at the selected station remain the designer's responsibility.
/// </summary>
public sealed record Ntc2018SteelTensionEvidenceD1(
    string MemberId,
    string SectionId,
    string SteelGrade,
    string AnalysisId,
    string CombinationId,
    Ntc2018SteelHoleStateD1 HoleState,
    string? HoleStateEvidence,
    string? SectionGeometryEvidence,
    string? MaterialYieldEvidence,
    bool? ConcentricTensionConfirmed,
    bool? OutOfPlaneAndTorsionExcludedConfirmed,
    double? VerifiedNetAreaMm2 = null,
    string? NetAreaEvidence = null,
    double? CharacteristicUltimateStrengthMpa = null,
    string? UltimateStrengthEvidence = null,
    bool? CapacityHierarchyRequired = null,
    string? CapacityHierarchyApplicabilityEvidence = null);

/// <summary>
/// Locked NTC 2018 steel values for ordinary building members (not bridges).
/// This profile is not an EN 1993 national-annex profile.
/// </summary>
public static class Ntc2018SteelTensionProfileD1
{
    public static readonly DesignCodeEdition Edition = new(
        "DM 17 gennaio 2018", "NTC2018", "IT-NTC2018-NATIONAL-CODE");
    public const string FactorClause = "NTC 2018, 4.2.4.1.1, Tabella 4.2.VII (membrature ordinarie)";
    public const double GammaM0 = 1.05;
    public const double GammaM1 = 1.05;
    public const double GammaM2 = 1.25;

    public static NormativeProfile Create() => new("NTC2018-STEEL-ORDINARY-MEMBERS-D1", Edition,
        new[]
        {
            new NormativeCoefficient("gamma_M0", GammaM0, FactorClause, "Snervamento della sezione lorda"),
            new NormativeCoefficient("gamma_M1", GammaM1, FactorClause, "Instabilita: tracciato, non usato in D1"),
            new NormativeCoefficient("gamma_M2", GammaM2, FactorClause, "Rottura della sezione netta")
        });

    internal static bool IsExact(NormativeProfile? p)
    {
        if (p is null || p.Code != Edition ||
            p.ProfileId != "NTC2018-STEEL-ORDINARY-MEMBERS-D1" ||
            p.Coefficients is null || p.Coefficients.Count != 3)
            return false;
        return Has(p, "gamma_M0", GammaM0) && Has(p, "gamma_M1", GammaM1) && Has(p, "gamma_M2", GammaM2);
    }

    private static bool Has(NormativeProfile p, string id, double expected) =>
        p.Coefficients.Count(c => c.Id == id && c.Value == expected && c.SourceClause == FactorClause) == 1;
}

/// <summary>
/// Shared guarded evaluation of NTC 2018 §4.2.4.1.2.1 at one section, pure positive N.
/// It is intentionally not a check of member buckling, connections, fatigue or full compliance.
/// </summary>
public abstract class Ntc2018SteelTensionRuleD1 : IDesignCheckRule
{
    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, Ntc2018SteelTensionEvidenceD1> evidence;

    protected Ntc2018SteelTensionRuleD1(CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<Ntc2018SteelTensionEvidenceD1> evidence)
    {
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(evidence);
        var index = new Dictionary<string, Ntc2018SteelTensionEvidenceD1>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in evidence)
        {
            if (item is null || string.IsNullOrWhiteSpace(item.MemberId) ||
                string.IsNullOrWhiteSpace(item.SectionId) || string.IsNullOrWhiteSpace(item.SteelGrade) ||
                string.IsNullOrWhiteSpace(item.AnalysisId) || string.IsNullOrWhiteSpace(item.CombinationId))
                throw new ArgumentException("Evidence requires member, section, grade, analysis and combination.", nameof(evidence));
            var key = Key(item.MemberId, item.AnalysisId, item.CombinationId);
            if (!index.TryAdd(key, item))
                throw new ArgumentException("Duplicate NTC tension evidence target.", nameof(evidence));
        }
        this.evidence = index;
    }

    public abstract string RuleId { get; }
    public DesignCodeEdition Code => Ntc2018SteelTensionProfileD1.Edition;
    public DesignMaterial Material => DesignMaterial.Steel;
    public DesignLimitState LimitState => DesignLimitState.Ultimate;
    public abstract string SourceClause { get; }
    public bool EstablishesCompleteMemberCompliance => false;

    public DesignRuleEvaluation Evaluate(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Profile?.Code != Code)
            return DesignRuleEvaluation.MissingData("Incorrect NTC 2018 identity; no EC3 fallback.");
        if (!Ntc2018SteelTensionProfileD1.IsExact(request.Profile))
            return DesignRuleEvaluation.MissingData("Required immutable ordinary-member NTC 2018 gamma profile is missing or modified.");
        if (request.Member.Material != Material || request.LimitState != LimitState)
            return DesignRuleEvaluation.NotApplicable("Only steel ultimate-limit-state section checks are implemented.");
        if (request.Effects.Origin != DesignEffectOrigin.FemAnalysis ||
            request.Effects.TargetKind != DesignEffectTargetKind.LoadCombination ||
            request.Effects.SectionSide is null ||
            string.IsNullOrWhiteSpace(request.Effects.MemberId) ||
            !string.Equals(request.Member.MemberId, request.Effects.MemberId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Explicit FEM member, combination and one-sided section provenance required.");
        if (request.Effects.AxialForceKn is not double n ||
            request.Effects.ShearForceYKn is not double v ||
            request.Effects.BendingMomentZKnm is not double m ||
            !double.IsFinite(n) || !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Signed concomitant N/V/M at one FEM station required.");
        if (n <= 0 || v != 0 || m != 0)
            return DesignRuleEvaluation.NotApplicable("Only concentric tensile force N>0 with precisely V=M=0 is supported.");

        if (!evidence.TryGetValue(Key(request.Member.MemberId, request.Effects.AnalysisId,
                request.Effects.CombinationId), out var e))
            return DesignRuleEvaluation.MissingData("Missing evidence for this exact member, analysis and combination.");
        if (!string.Equals(e.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Catalog profile differs from the documented member section.");
        if (string.IsNullOrWhiteSpace(e.SectionGeometryEvidence) ||
            string.IsNullOrWhiteSpace(e.MaterialYieldEvidence) ||
            string.IsNullOrWhiteSpace(e.HoleStateEvidence) ||
            e.ConcentricTensionConfirmed != true || e.OutOfPlaneAndTorsionExcludedConfirmed != true)
            return DesignRuleEvaluation.MissingData("Independent profile, grade, hole, concentricity and spatial/torsional evidence required.");

        ResolvedRolledSteelSectionM88B2 section;
        try { section = sections.Find(e.SectionId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown commercial rolled steel profile."); }
        var s = section.Definition;
        double t = Math.Max(s.FlangeThicknessMm, s.WebThicknessMm);
        double fy;
        try { fy = materials.GetSteelYieldMpa(e.SteelGrade, t); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown steel grade."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.MissingData("No catalog yield band for the declared steel thickness."); }
        double area = s.AreaCm2 * 100d;
        if (!double.IsFinite(area) || area <= 0 || !double.IsFinite(fy) || fy <= 0)
            return DesignRuleEvaluation.MissingData("Invalid gross area or grade yield strength.");
        double gross = area * fy / Ntc2018SteelTensionProfileD1.GammaM0 / 1000d;
        if (!double.IsFinite(gross) || gross <= 0)
            return DesignRuleEvaluation.MissingData("Gross resistance is outside the supported numerical range.");

        double? net = null;
        switch (e.HoleState)
        {
            case Ntc2018SteelHoleStateD1.NoRelevantHoles:
                if (e.VerifiedNetAreaMm2 is not null || e.CharacteristicUltimateStrengthMpa is not null ||
                    !string.IsNullOrWhiteSpace(e.NetAreaEvidence) || !string.IsNullOrWhiteSpace(e.UltimateStrengthEvidence))
                    return DesignRuleEvaluation.MissingData("Contradictory no-hole branch with net-section data.");
                break;
            case Ntc2018SteelHoleStateD1.RelevantConnectionHoles:
                if (e.VerifiedNetAreaMm2 is not double aNet || !double.IsFinite(aNet) ||
                    aNet <= 0 || aNet >= area || string.IsNullOrWhiteSpace(e.NetAreaEvidence) ||
                    e.CharacteristicUltimateStrengthMpa is not double fu || !double.IsFinite(fu) ||
                    fu <= fy || string.IsNullOrWhiteSpace(e.UltimateStrengthEvidence))
                    return DesignRuleEvaluation.MissingData("Holes require documented 0<Anet<A, ftk>fyk and individual source evidence; no defaults.");
                net = 0.9 * aNet * fu / Ntc2018SteelTensionProfileD1.GammaM2 / 1000d;
                if (!double.IsFinite(net.Value) || net.Value <= 0)
                    return DesignRuleEvaluation.MissingData("Net resistance is outside the supported numerical range.");
                break;
            default:
                return DesignRuleEvaluation.MissingData("Unknown or unsupported hole state: cannot select gross-only tension resistance.");
        }

        string trace = $"SECTION ONLY | {SourceClause} | member={e.MemberId} section={s.Id} " +
            $"grade={e.SteelGrade}; OPPO={s.SourcePageUrl} ({s.DataStatus}; NON CERTIFICATO) | " +
            $"A={F(area)} mm2 fy={F(fy)} MPa t={F(t)} mm; gammaM0=1.05 gammaM2=1.25 " +
            $"source={Ntc2018SteelTensionProfileD1.FactorClause}; gross Npl,Rd={F(gross)} kN; " +
            (net.HasValue ? $"Anet={F(e.VerifiedNetAreaMm2!.Value)} mm2 ftk={F(e.CharacteristicUltimateStrengthMpa!.Value)} MPa " +
                $"net Nu,Rd={F(net.Value)} kN; netEvidence={e.NetAreaEvidence}; ftkEvidence={e.UltimateStrengthEvidence}; " :
                "holes=NONE CONFIRMED; ") +
            $"geometryEvidence={e.SectionGeometryEvidence}; fyEvidence={e.MaterialYieldEvidence}; " +
            $"holeEvidence={e.HoleStateEvidence}; N={F(n)} kN V={F(v)} kN M={F(m)} kNm; " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(request.Effects.PositionMetres)} m side={request.Effects.SectionSide}; " +
            "NOT COMPLETE MEMBER COMPLIANCE: no bolts/joints, spatial buckling, seismic capacity design, fatigue, or other sections checked.";
        return EvaluateResolved(request, e, gross, net, trace);
    }

    protected abstract DesignRuleEvaluation EvaluateResolved(DesignCheckRequest request,
        Ntc2018SteelTensionEvidenceD1 e, double gross, double? net, string trace);

    private static string Key(string member, string analysis, string combination) =>
        $"{member.Length}:{member}|{analysis.Length}:{analysis}|{combination.Length}:{combination}";
    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}

/// <summary>NTC 2018 [4.2.5]-[4.2.7]: min(gross yield, net rupture) with explicitly declared holes.</summary>
public sealed class Ntc2018SteelPureTensionResistanceRuleD1 : Ntc2018SteelTensionRuleD1
{
    public const string Id = "NTC2018-STEEL-PURE-TENSION-GROSS-NET-D1";
    public const string Clause = "NTC 2018, 4.2.4.1.2.1, Eqs. [4.2.5]-[4.2.7]";
    public Ntc2018SteelPureTensionResistanceRuleD1(CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials, IEnumerable<Ntc2018SteelTensionEvidenceD1> evidence)
        : base(sections, materials, evidence) { }
    public override string RuleId => Id;
    public override string SourceClause => Clause;

    protected override DesignRuleEvaluation EvaluateResolved(DesignCheckRequest request,
        Ntc2018SteelTensionEvidenceD1 e, double gross, double? net, string trace) =>
        DesignRuleEvaluation.Calculated(request.Effects.AxialForceKn!.Value,
            net.HasValue ? Math.Min(gross, net.Value) : gross, "kN",
            $"{trace} governing={(net.HasValue && net.Value < gross ? "NET_RUPTURE" : "GROSS_YIELD")}; " +
            (e.CapacityHierarchyRequired is null ? "CAPACITY HIERARCHY APPLICABILITY NOT DOCUMENTED." :
             "Capacity hierarchy must be evaluated via separate rule where applicable."));
}

/// <summary>NTC 2018 [4.2.8] capacity hierarchy; separate obligation where required.</summary>
public sealed class Ntc2018SteelTensionCapacityHierarchyRuleD1 : Ntc2018SteelTensionRuleD1
{
    public const string Id = "NTC2018-STEEL-TENSION-GROSS-NET-HIERARCHY-D1";
    public const string Clause = "NTC 2018, 4.2.4.1.2.1, Eq. [4.2.8]";
    public Ntc2018SteelTensionCapacityHierarchyRuleD1(CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials, IEnumerable<Ntc2018SteelTensionEvidenceD1> evidence)
        : base(sections, materials, evidence) { }
    public override string RuleId => Id;
    public override string SourceClause => Clause;

    protected override DesignRuleEvaluation EvaluateResolved(DesignCheckRequest request,
        Ntc2018SteelTensionEvidenceD1 e, double gross, double? net, string trace)
    {
        if (e.CapacityHierarchyRequired is null || string.IsNullOrWhiteSpace(e.CapacityHierarchyApplicabilityEvidence))
            return DesignRuleEvaluation.MissingData("Capacity hierarchy applicability must be documented, including justified exclusion.");
        if (e.CapacityHierarchyRequired == false)
            return DesignRuleEvaluation.NotApplicable("Independently documented project decision: [4.2.8] hierarchy not required; " +
                e.CapacityHierarchyApplicabilityEvidence);
        if (net is null)
            return DesignRuleEvaluation.MissingData("Hierarchy required but no net rupture capacity is available.");
        return DesignRuleEvaluation.Calculated(gross, net.Value, "kN", trace +
            $" Capacity hierarchy REQUIRED; evidence={e.CapacityHierarchyApplicabilityEvidence}; " +
            $"check Npl,Rd <= Nu,Rd: {gross.ToString("G17", CultureInfo.InvariantCulture)} <= " +
            $"{net.Value.ToString("G17", CultureInfo.InvariantCulture)} kN.");
    }
}
