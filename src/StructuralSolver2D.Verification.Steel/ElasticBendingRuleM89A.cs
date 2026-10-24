using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Explicit evidence supplied by a competent designer. This is not a section-classification
/// algorithm, a restraint check, a hole check, or a product conformity certificate.
/// </summary>
public sealed record SteelElasticBendingAssignmentM89A(
    string MemberId,
    string SectionId,
    string SteelGrade,
    int? SectionClass,
    string? ClassificationEvidence,
    bool? StrongAxisMappingConfirmed,
    bool? NoRelevantHolesConfirmed);

/// <summary>
/// EN 1993-1-1:2005+A1:2014, 6.2.5(2), expression (6.14):
/// M_el,Rd = W_el,min * fy / gamma_M0 for cross-section class 3, bending about ONE axis.
/// Only pure bending with zero concurrent axial and shear is supported in M89-A.
/// This is a SINGLE cross-section resistance check, never a complete beam/member approval.
/// Lateral-torsional buckling, shear buckling, interaction, holes and connections are NOT checked.
/// Tabulated profiles from OPPO are indicative; field measurements/certification stay external.
/// </summary>
public sealed class ElasticBendingRuleM89A : IDesignCheckRule
{
    public const string Id = "EC3-2005-CLASS3-ELASTIC-STRONG-AXIS-BENDING-M89A";
    public const string GammaId = "gamma_M0";
    public const string Clause = "EN 1993-1-1:2005+A1:2014, 6.2.5(2), Eq. (6.14)";
    public const string SupportedEdition = "2005+A1:2014";
    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, SteelElasticBendingAssignmentM89A> assignments;

    public ElasticBendingRuleM89A(
        DesignCodeEdition code,
        CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<SteelElasticBendingAssignmentM89A> assignments)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.DocumentId != "EN 1993-1-1" || code.EditionId != SupportedEdition ||
            string.IsNullOrWhiteSpace(code.NationalAnnexId))
            throw new NotSupportedException("M89-A implements only EN 1993-1-1:2005+A1:2014 with an explicit annex identifier; not 2022 or NTC.");
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(assignments);
        var map = new Dictionary<string, SteelElasticBendingAssignmentM89A>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in assignments)
        {
            if (a is null || string.IsNullOrWhiteSpace(a.MemberId) ||
                string.IsNullOrWhiteSpace(a.SectionId) || string.IsNullOrWhiteSpace(a.SteelGrade))
                throw new ArgumentException("An assignment must identify a member, section and grade.", nameof(assignments));
            if (!map.TryAdd(a.MemberId, a))
                throw new ArgumentException("Duplicate member assignment.", nameof(assignments));
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
        if (!assignments.TryGetValue(request.Member.MemberId, out var assignment))
            return DesignRuleEvaluation.MissingData("No M89-A member-to-section-and-steel-grade assignment.");
        if (!string.Equals(assignment.SectionId, request.Member.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("Assignment and design member section identifiers differ.");
        if (assignment.SectionClass is null || string.IsNullOrWhiteSpace(assignment.ClassificationEvidence))
            return DesignRuleEvaluation.MissingData("Explicit, independently justified cross-section classification required.");
        if (assignment.SectionClass != 3)
            return DesignRuleEvaluation.NotApplicable("M89-A implements only class-3 elastic bending: no class 1/2 plastic or class-4 effective section check.");
        if (assignment.StrongAxisMappingConfirmed != true)
            return DesignRuleEvaluation.MissingData("The FEM local Mz to OPPO strong-axis Wx correspondence must be explicitly confirmed.");
        if (assignment.NoRelevantHolesConfirmed != true)
            return DesignRuleEvaluation.MissingData("Relevant flange/web hole assumptions have not been confirmed.");
        if (request.Effects.AxialForceKn is not double axial ||
            request.Effects.ShearForceYKn is not double shear ||
            request.Effects.BendingMomentZKnm is not double moment)
            return DesignRuleEvaluation.MissingData("Concomitant N, V and M are all required, even for pure bending.");
        if (!double.IsFinite(axial) || !double.IsFinite(shear) || !double.IsFinite(moment))
            return DesignRuleEvaluation.MissingData("Non-finite structural action.");
        // This narrow test is deliberately not silently extended to mixed N/V/M states.
        if (axial != 0d || shear != 0d)
            return DesignRuleEvaluation.NotApplicable("Nonzero concomitant N or V: interaction or shear-reduction checks are outside M89-A.");
        var coeff = request.Profile.Coefficients;
        var gamma = coeff?.SingleOrDefault(c => string.Equals(c.Id, GammaId, StringComparison.OrdinalIgnoreCase));
        if (gamma is null || !double.IsFinite(gamma.Value) || gamma.Value <= 0 ||
            string.IsNullOrWhiteSpace(gamma.SourceClause))
            return DesignRuleEvaluation.MissingData("Positive, cited gamma_M0 must be supplied for the selected national annex; no default.");

        ResolvedRolledSteelSectionM88B2 section;
        try { section = sections.Find(assignment.SectionId); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown rolled-section ID in the declared M88-B2 catalog."); }
        var d = section.Definition;
        // For rolled I profiles, the larger of flange/web nominal thickness controls the table yield band.
        double thickness = Math.Max(d.FlangeThicknessMm, d.WebThicknessMm);
        double fy;
        try { fy = materials.GetSteelYieldMpa(assignment.SteelGrade, thickness); }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown steel grade in M88-C."); }
        catch (NotSupportedException) { return DesignRuleEvaluation.MissingData("Thickness lies outside the documented steel-yield bands."); }
        if (!double.IsFinite(d.StrongElasticModulusCm3) || d.StrongElasticModulusCm3 <= 0 ||
            !double.IsFinite(fy) || fy <= 0)
            return DesignRuleEvaluation.MissingData("Non-positive or non-finite elastic modulus or yield strength.");
        // [cm3] * [MPa] = [1e-6 m3] * [1e3 kN/m2] = [1e-3 kNm].
        double capacityKnm = d.StrongElasticModulusCm3 * fy / gamma.Value * 1e-3;
        double demandKnm = Math.Abs(moment);
        string f(double v) => v.ToString("G17", CultureInfo.InvariantCulture);
        var audit = $"Single-station CLASS-3 pure bending only | member={request.Member.MemberId} | " +
            $"section={d.Id} OPPO ({d.DataStatus}, {d.SourcePageUrl}) | " +
            $"steel={assignment.SteelGrade} EN 10025-2:2019 (reference-only) | " +
            $"thickness=max(tf,tw)={f(thickness)} mm | fy={f(fy)} MPa | " +
            $"Wx={f(d.StrongElasticModulusCm3)} cm3 | gamma_M0={f(gamma.Value)} ({gamma.SourceClause}) | " +
            $"M_Ed={f(moment)} kNm | M_el,Rd={f(capacityKnm)} kNm | " +
            $"classEvidence={assignment.ClassificationEvidence} | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId} | " +
            $"analysis={request.Effects.AnalysisId}, target={request.Effects.CombinationId}, x={f(request.Effects.PositionMetres)} m | " +
            "NOT checked: lateral-torsional buckling, member instability, shear, N-M interactions, connections or overall compliance.";
        return DesignRuleEvaluation.Calculated(demandKnm, capacityKnm, "kNm", audit);
    }
}
