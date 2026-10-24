using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Explicit, independently substantiated assumptions for ONE section of a rolled I/H member.
/// Passing this single check NEVER establishes complete EC3/NTC/member compliance.
/// </summary>
public sealed record SteelClass3NmvAssignmentM90C(
    string MemberId, string SectionId, string SteelGrade,
    int? SectionClass, string? ClassificationEvidence,
    bool? StrongAxisMappingConfirmed,
    bool? NoRelevantHolesConfirmed,
    bool? NoTorsionConfirmed,
    double? ShearAreaCm2 = null,
    string? ShearAreaEvidence = null,
    bool? InPlaneShearAxisMappingConfirmed = null,
    bool? ShearBucklingExcludedConfirmed = null);

/// <summary>
/// EC3 EN 1993-1-1:2005+A1:2014 §6.2.9.2 Eq (6.42), extended ONLY to
/// V_Ed <= 0.5 V_pl,Rd under §6.2.10(2). Class-3 strong-axis single-station N/M
/// elastic stress: sigma_max = |N|/A + |M|/W_el <= fy/gamma_M0.
/// Uses concomitant signed FEM N,V,M and checks the conservative M90-B elastic class
/// bound. Shear above half Vpl, torsion, holes, EN 1993-1-5 shear buckling,
/// plastic N-M, class 4 effective properties, net fracture, all member buckling,
/// transverse loads, connections and complete code compliance remain OUT OF SCOPE.
/// </summary>
public sealed class SteelClass3ElasticNmvRuleM90C : IDesignCheckRule
{
    public const string Id = "EC3-2005-CLASS3-STRONG-AXIS-ELASTIC-NMV-M90C";
    public const string GammaId = "gamma_M0";
    public const string Clause = "EN 1993-1-1:2005+A1:2014, §6.2.9.2(1) Eq (6.42), §6.2.10(2), §6.2.6(2) Eq (6.18)";
    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, SteelClass3NmvAssignmentM90C> assignments;

    public SteelClass3ElasticNmvRuleM90C(
        DesignCodeEdition code,
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<SteelClass3NmvAssignmentM90C> assignments)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.DocumentId != "EN 1993-1-1" || code.EditionId != "2005+A1:2014" ||
            string.IsNullOrWhiteSpace(code.NationalAnnexId))
            throw new NotSupportedException("M90-C only supports EC3 2005+A1:2014 with explicit annex ID; not 2022 or NTC.");
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(assignments);
        var map = new Dictionary<string, SteelClass3NmvAssignmentM90C>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in assignments)
        {
            if (a is null || string.IsNullOrWhiteSpace(a.MemberId) ||
                string.IsNullOrWhiteSpace(a.SectionId) || string.IsNullOrWhiteSpace(a.SteelGrade))
                throw new ArgumentException("Incomplete M90-C member assignment.", nameof(assignments));
            if (!map.TryAdd(a.MemberId, a))
                throw new ArgumentException("Duplicate M90-C member assignment.", nameof(assignments));
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
            return DesignRuleEvaluation.MissingData("M90-C assignment absent for this member.");
        if (!string.Equals(a.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Assignment/FEM section ID mismatch.");
        if (a.SectionClass is null || string.IsNullOrWhiteSpace(a.ClassificationEvidence))
            return DesignRuleEvaluation.MissingData("Documented class-3 classification required.");
        if (a.SectionClass != 3)
            return DesignRuleEvaluation.NotApplicable("M90-C is limited to class 3. Classes 1/2 plastic and class 4 effective properties are excluded.");
        if (a.StrongAxisMappingConfirmed != true || a.NoRelevantHolesConfirmed != true ||
            a.NoTorsionConfirmed != true)
            return DesignRuleEvaluation.MissingData("Explicit strong-axis mapping, no relevant holes, and no torsion confirmations required.");

        var e = request.Effects;
        if (e is null || e.Origin != DesignEffectOrigin.FemAnalysis ||
            !string.Equals(e.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            e.TargetKind is null || e.SectionSide is null ||
            string.IsNullOrWhiteSpace(e.AnalysisId) || string.IsNullOrWhiteSpace(e.CombinationId))
            return DesignRuleEvaluation.MissingData("A fully traceable, single-target FEM N/V/M station is required; no mixed envelopes.");
        if (e.AxialForceKn is not double n || e.BendingMomentZKnm is not double m ||
            e.ShearForceYKn is not double v ||
            !double.IsFinite(n) || !double.IsFinite(m) || !double.IsFinite(v))
            return DesignRuleEvaluation.MissingData("Finite concomitant N, V and M are required.");
        if (n == 0d || m == 0d)
            return DesignRuleEvaluation.NotApplicable("M90-C requires both N and M nonzero; pure actions remain M89-A/B checks.");

        var gammaEntry = request.Profile.Coefficients?.FirstOrDefault(c =>
            string.Equals(c.Id, GammaId, StringComparison.OrdinalIgnoreCase));
        if (gammaEntry is null || !double.IsFinite(gammaEntry.Value) || gammaEntry.Value <= 0 ||
            string.IsNullOrWhiteSpace(gammaEntry.SourceClause))
            return DesignRuleEvaluation.MissingData("Explicit positive gamma_M0 with source required; no default.");
        double gamma = gammaEntry.Value;

        PublishedRolledSteelSectionM88B2 p;
        try { p = sections.Find(a.SectionId).Definition; }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown rolled profile in M88-B2."); }
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            return DesignRuleEvaluation.NotApplicable("Only hot-rolled parallel-flange IPE/HEA/HEB sections; INP inclined flanges excluded.");
        double thickness = Math.Max(p.WebThicknessMm, p.FlangeThicknessMm);
        double fy;
        try { fy = materials.GetSteelYieldMpa(a.SteelGrade, thickness); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Steel grade unknown in M88-C."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.MissingData("Unspecified yield-strength thickness range."); }
        if (!(p.AreaCm2 > 0) || !(p.StrongElasticModulusCm3 > 0) ||
            !double.IsFinite(p.AreaCm2) || !double.IsFinite(p.StrongElasticModulusCm3) ||
            !double.IsFinite(fy) || fy <= 0)
            return DesignRuleEvaluation.MissingData("Invalid area, strong-axis Wel, or steel yield strength.");

        // Does the elastic stress field remain within class 3? M90-B may conservatively
        // reject admissible situations, but must never upgrade a class-4 section.
        SteelMixedElasticScreenM90B screen;
        try { screen = SteelMixedISectionClassifierM90B.ScreenConcomitantElasticFEM(
            request, sections, materials, a.SteelGrade); }
        catch (NotSupportedException) { return DesignRuleEvaluation.NotApplicable("M90-B elastic-screen assumptions are not met for this N/M field."); }
        catch (ArgumentException) { return DesignRuleEvaluation.MissingData("M90-B elastic screen lacks valid traceable inputs."); }
        if (screen.ConservativeClassUpperBound > 3)
            return DesignRuleEvaluation.NotApplicable("M90-B conservative class-4 indication; effective-section interaction not implemented.");

        double halfShearRatio = 0;
        double shearCapacity = 0;
        if (v != 0d)
        {
            if (a.ShearAreaCm2 is not double av || !double.IsFinite(av) || av <= 0 ||
                av > p.AreaCm2 || string.IsNullOrWhiteSpace(a.ShearAreaEvidence))
                return DesignRuleEvaluation.MissingData("Shear requires justified 0<Av<=A in cm2; no automatic Av inference.");
            if (a.InPlaneShearAxisMappingConfirmed != true || a.ShearBucklingExcludedConfirmed != true)
                return DesignRuleEvaluation.MissingData("Shear axis and external shear-buckling exclusion must be documented.");
            shearCapacity = av * fy / (10 * Math.Sqrt(3) * gamma); // cm2*MPa -> kN
            if (!double.IsFinite(shearCapacity) || shearCapacity <= 0)
                return DesignRuleEvaluation.MissingData("Invalid Vpl,Rd.");
            halfShearRatio = Math.Abs(v) / shearCapacity;
            if (halfShearRatio > 0.5d)
                return DesignRuleEvaluation.NotApplicable("V_Ed exceeds 0.5 Vpl,Rd; reduced-yield N-M-V interaction not implemented in M90-C.");
        }

        // For a doubly-symmetric section: max(|N/A + M/W|, |N/A - M/W|)
        // equals |N|/A+|M|/W. kN/cm2 -> 10 MPa; kNm/cm3 -> 1000 MPa.
        double axialStress = Math.Abs(n) * 10 / p.AreaCm2;
        double bendingStress = Math.Abs(m) * 1000 / p.StrongElasticModulusCm3;
        double demandMpa = axialStress + bendingStress;
        double resistanceMpa = fy / gamma;
        if (!double.IsFinite(demandMpa) || !double.IsFinite(resistanceMpa) || resistanceMpa <= 0)
            return DesignRuleEvaluation.MissingData("Non-finite or non-positive section resistance data.");

        static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
        string trace = $"CLASS3 SECTION-ONLY elastic N-M-V M90-C | {Clause} | " +
            $"member={request.Member.MemberId}, section={p.Id}, grade={a.SteelGrade} | " +
            $"profile={p.SourcePageUrl} status={p.DataStatus} (indicative, not certified) | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId} | " +
            $"gamma_M0={F(gamma)} source={gammaEntry.SourceClause} | " +
            $"A={F(p.AreaCm2)}cm2 Wel,strong={F(p.StrongElasticModulusCm3)}cm3 " +
            $"t={F(thickness)}mm fy={F(fy)}MPa | " +
            $"N={F(n)}kN V={F(v)}kN M={F(m)}kNm " +
            $"sigmaN={F(axialStress)}MPa sigmaM={F(bendingStress)}MPa " +
            $"sigmaMax={F(demandMpa)}MPa fy/gamma={F(resistanceMpa)}MPa | " +
            $"Vpl,Rd={F(shearCapacity)}kN V/Vpl={F(halfShearRatio)} " +
            $"AvEvidence={a.ShearAreaEvidence ?? "V=0; Av not required"} | " +
            $"classEvidence={a.ClassificationEvidence} M90B-upper={screen.ConservativeClassUpperBound} | " +
            $"analysis={e.AnalysisId} target={e.CombinationId} kind={e.TargetKind} " +
            $"x={F(e.PositionMetres)}m side={e.SectionSide} | " +
            "NOT CHECKED: global/in-plane/out-of-plane buckling, lateral-torsional buckling, " +
            "local transverse loads, concentrated forces, connections, net fracture, high shear, " +
            "torsion or entire EC3/NTC/member compliance.";
        return DesignRuleEvaluation.Calculated(demandMpa, resistanceMpa, "MPa", trace);
    }
}
