using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// A narrowly defined fork-supported, warping-free, doubly symmetric beam segment
/// under CONSTANT end-couple bending (C1=1). Properties in MPa, cm4, cm6 and m.
/// Other moment diagrams, transverse load heights and elastic restraints need an
/// externally established Mcr, NOT this standard-case formula.
/// </summary>
public sealed record SteelUniformMomentMcrInputM91C(
    double? YoungModulusMpa, string? YoungModulusEvidence,
    double? ShearModulusMpa, string? ShearModulusEvidence,
    double? SaintVenantTorsionCm4, string? TorsionPropertyEvidence,
    double? WarpingConstantCm6, string? WarpingPropertyEvidence,
    bool? ForkRestraintsAtBothEndsConfirmed,
    bool? FreeWarpingAtBothEndsConfirmed,
    bool? NoIntermediateRestraintsConfirmed,
    bool? UniformEndCouplesOnlyConfirmed);

/// <summary>
/// A single UNBRACED member segment and one analysed FEM station. Explicit source of
/// Mcr is EITHER an external documented elastic buckling calculation OR the limited
/// uniform-moment analytical case. Non-null inputs are not interpreted as evidence
/// of verified lateral bracing or actual product certification.
/// </summary>
public sealed record SteelLateralTorsionalAssignmentM91C(
    string MemberId, string SectionId, string SteelGrade,
    int? BendingSectionClass, string? ClassificationEvidence,
    double SegmentStartMetres, double SegmentEndMetres,
    string? SegmentRestraintEvidence, string? MomentDiagramAndLoadHeightEvidence,
    double? ExternallyEstablishedMcrKnm, string? ExternalMcrEvidence,
    SteelUniformMomentMcrInputM91C? StandardCase,
    double? PlasticStrongAxisModulusCm3, string? PlasticModulusEvidence,
    bool? MajorAxisAndOrientationConfirmed,
    bool? DoublySymmetricRolledSectionConfirmed,
    bool? PrismaticSectionConfirmed,
    bool? NoRelevantHolesConfirmed,
    bool? GeneralCurveAndNationalAnnexConfirmed,
    string? GeneralCurveAndAnnexEvidence);

public sealed record SteelLtbGeneralCalculationM91C(
    string Curve, double AlphaLt, double LambdaBarLt, double PhiLt,
    double ChiLt, double McrKnm, double SectionReferenceResistanceKnm,
    double BucklingResistanceKnm);

/// <summary>
/// EC3 EN 1993-1-1:2005+A1:2014, 6.3.2.1-6.3.2.2, equations (6.54)-(6.56),
/// recommended rolled I/H general curves of Tables 6.3 and 6.4.
/// NOT the alternative rolled-section curve of 6.3.2.3.
/// Only pure bending of parallel-flange, doubly symmetric rolled I/H, class 1-3.
/// A successful RESULT IS NOT a complete beam/member verification.
/// </summary>
public sealed class SteelLateralTorsionalBucklingRuleM91C : IDesignCheckRule
{
    public const string Id = "EC3-2005-ROLLED-I-STRONG-AXIS-PURE-BENDING-LTB-M91C";
    public const string GammaId = "gamma_M1";
    public const string Clause = "EN 1993-1-1:2005+A1:2014, 6.3.2.1-6.3.2.2, Eqs (6.54)-(6.56), Tables 6.3-6.4 (general method)";

    private readonly CommercialRolledSteelCatalogM88B2 sections;
    private readonly CharacteristicMaterialCatalogM88C materials;
    private readonly IReadOnlyDictionary<string, SteelLateralTorsionalAssignmentM91C> assignments;

    public SteelLateralTorsionalBucklingRuleM91C(
        DesignCodeEdition code, CommercialRolledSteelCatalogM88B2 sections,
        CharacteristicMaterialCatalogM88C materials,
        IEnumerable<SteelLateralTorsionalAssignmentM91C> assignments)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code.DocumentId != "EN 1993-1-1" || code.EditionId != "2005+A1:2014" ||
            string.IsNullOrWhiteSpace(code.NationalAnnexId))
            throw new NotSupportedException("M91-C requires the explicit EC3 2005+A1:2014 edition and an annex identifier.");
        this.sections = sections ?? throw new ArgumentNullException(nameof(sections));
        this.materials = materials ?? throw new ArgumentNullException(nameof(materials));
        ArgumentNullException.ThrowIfNull(assignments);
        var map = new Dictionary<string, SteelLateralTorsionalAssignmentM91C>(StringComparer.OrdinalIgnoreCase);
        foreach (var assignment in assignments)
        {
            if (assignment is null || string.IsNullOrWhiteSpace(assignment.MemberId) ||
                string.IsNullOrWhiteSpace(assignment.SectionId) ||
                string.IsNullOrWhiteSpace(assignment.SteelGrade))
                throw new ArgumentException("Invalid M91-C member assignment.", nameof(assignments));
            if (!map.TryAdd(assignment.MemberId, assignment))
                throw new ArgumentException("Duplicate M91-C member ID.", nameof(assignments));
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
            return DesignRuleEvaluation.MissingData("Missing M91-C member assignment.");
        if (!string.Equals(request.Member.SectionId, a.SectionId, StringComparison.OrdinalIgnoreCase))
            return DesignRuleEvaluation.MissingData("FEM member section differs from documented M91-C section.");
        if (a.MajorAxisAndOrientationConfirmed != true ||
            a.DoublySymmetricRolledSectionConfirmed != true ||
            a.PrismaticSectionConfirmed != true || a.NoRelevantHolesConfirmed != true)
            return DesignRuleEvaluation.MissingData("Strong-axis orientation, doubly symmetric rolled section, prism and gross section must be confirmed.");
        if (a.GeneralCurveAndNationalAnnexConfirmed != true ||
            string.IsNullOrWhiteSpace(a.GeneralCurveAndAnnexEvidence))
            return DesignRuleEvaluation.MissingData("Project-specific acceptance and documented National Annex applicability of general EC3 curves required.");
        if (!FiniteNonnegative(a.SegmentStartMetres) ||
            !ValidPositive(a.SegmentEndMetres) ||
            a.SegmentEndMetres <= a.SegmentStartMetres ||
            a.SegmentEndMetres > request.Member.MemberLengthMetres ||
            string.IsNullOrWhiteSpace(a.SegmentRestraintEvidence) ||
            string.IsNullOrWhiteSpace(a.MomentDiagramAndLoadHeightEvidence))
            return DesignRuleEvaluation.MissingData("Documented valid unbraced segment, restraints and applied moment diagram/load heights required.");

        var effects = request.Effects;
        if (effects.Origin != DesignEffectOrigin.FemAnalysis || effects.TargetKind is null ||
            effects.SectionSide is null ||
            !string.Equals(effects.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(effects.AnalysisId) || string.IsNullOrWhiteSpace(effects.CombinationId) ||
            effects.PositionMetres < a.SegmentStartMetres || effects.PositionMetres > a.SegmentEndMetres)
            return DesignRuleEvaluation.MissingData("Fully traceable FEM station WITHIN the declared lateral-buckling segment required.");
        if (effects.AxialForceKn is not double n || effects.ShearForceYKn is not double v ||
            effects.BendingMomentZKnm is not double m || !double.IsFinite(n) ||
            !double.IsFinite(v) || !double.IsFinite(m))
            return DesignRuleEvaluation.MissingData("Concomitant finite N, V and M required.");
        if (n != 0 || v != 0 || m == 0)
            return DesignRuleEvaluation.NotApplicable("M91-C is restricted to pure strong-axis bending at this station: N=V=0, M!=0.");

        if (a.BendingSectionClass is null || string.IsNullOrWhiteSpace(a.ClassificationEvidence))
            return DesignRuleEvaluation.MissingData("Declared, sourced strong-axis bending section class required.");
        if (a.BendingSectionClass is < 1 or > 3)
            return DesignRuleEvaluation.NotApplicable("Class 4 effective strong-axis modulus and local buckling are not implemented.");

        var gamma = request.Profile.Coefficients?.FirstOrDefault(c =>
            string.Equals(c.Id, GammaId, StringComparison.OrdinalIgnoreCase));
        if (gamma is null || !ValidPositive(gamma.Value) || string.IsNullOrWhiteSpace(gamma.SourceClause))
            return DesignRuleEvaluation.MissingData("gamma_M1 with explicit positive value and source is required.");

        PublishedRolledSteelSectionM88B2 p;
        try { p = sections.Find(a.SectionId).Definition; }
        catch (KeyNotFoundException) { return DesignRuleEvaluation.MissingData("Unknown commercial section in M88-B2."); }
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            return DesignRuleEvaluation.NotApplicable("Only hot-rolled parallel-flange doubly symmetric IPE/HEA/HEB, not INP.");
        if (a.SteelGrade is not ("S235" or "S275" or "S355"))
            return DesignRuleEvaluation.NotApplicable("M91-C supports documented M88-C S235/S275/S355 only.");
        double fy;
        try { fy = materials.GetSteelYieldMpa(a.SteelGrade, Math.Max(p.FlangeThicknessMm, p.WebThicknessMm)); }
        catch (Exception ex) when (ex is KeyNotFoundException or NotSupportedException)
        { return DesignRuleEvaluation.MissingData("Steel strength or thickness outside catalog-supported range."); }
        if (!ValidPositive(fy) || !ValidPositive(p.StrongElasticModulusCm3) ||
            !ValidPositive(p.WeakInertiaCm4) || !ValidPositive(p.AreaCm2) ||
            !ValidPositive(p.HeightMm) || !ValidPositive(p.WidthMm))
            return DesignRuleEvaluation.MissingData("Invalid section geometry or characteristic yield.");

        SteelISectionClassificationM90A classification;
        try
        {
            classification = SteelISectionClassifierM90A.ClassifyCatalogProfile(Code, sections, materials,
                a.SectionId, a.SteelGrade, SteelISectionActionM90A.PureStrongAxisBending);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or KeyNotFoundException)
        { return DesignRuleEvaluation.MissingData("M90-A independent strong-axis bending classification unavailable."); }
        if (classification.Class == 4)
            return DesignRuleEvaluation.NotApplicable("Independent M90-A reports class 4; W_eff is not supported.");
        if (classification.Class != a.BendingSectionClass)
            return DesignRuleEvaluation.MissingData("Declared class differs from independently calculated M90-A strong-axis bending class.");

        double wy;
        string wEvidence;
        if (classification.Class == 3)
        {
            wy = p.StrongElasticModulusCm3;
            wEvidence = $"W_el,strong from M88-B2 OPPO indicative table: {p.SourcePageUrl}";
        }
        else
        {
            if (a.PlasticStrongAxisModulusCm3 is not double wpl ||
                !ValidPositive(wpl) || string.IsNullOrWhiteSpace(a.PlasticModulusEvidence))
                return DesignRuleEvaluation.MissingData("Class 1-2 requires externally documented W_pl,strong; M88-B2 only contains W_el.");
            double maximumPhysicalCm3 = p.AreaCm2 * (p.HeightMm / 10d) / 2d;
            if (wpl < p.StrongElasticModulusCm3 || wpl > maximumPhysicalCm3 * (1 + 1e-9))
                return DesignRuleEvaluation.MissingData("Declared W_pl,strong fails basic section-property bounds; independent source needed.");
            wy = wpl;
            wEvidence = a.PlasticModulusEvidence;
        }

        bool external = a.ExternallyEstablishedMcrKnm.HasValue;
        if (external == (a.StandardCase is not null))
            return DesignRuleEvaluation.MissingData("Specify EXACTLY ONE Mcr source: external calculation OR restricted uniform-moment standard case.");
        double mcr;
        string mcrEvidence;
        if (external)
        {
            if (a.ExternallyEstablishedMcrKnm is not double supplied || !ValidPositive(supplied) ||
                string.IsNullOrWhiteSpace(a.ExternalMcrEvidence))
                return DesignRuleEvaluation.MissingData("Finite positive externally established Mcr, method and source required.");
            mcr = supplied;
            mcrEvidence = a.ExternalMcrEvidence;
        }
        else
        {
            var s = a.StandardCase;
            if (s is null) return DesignRuleEvaluation.MissingData("No Mcr source.");
            if (s.ForkRestraintsAtBothEndsConfirmed != true ||
                s.FreeWarpingAtBothEndsConfirmed != true ||
                s.NoIntermediateRestraintsConfirmed != true ||
                s.UniformEndCouplesOnlyConfirmed != true ||
                string.IsNullOrWhiteSpace(s.YoungModulusEvidence) ||
                string.IsNullOrWhiteSpace(s.ShearModulusEvidence) ||
                string.IsNullOrWhiteSpace(s.TorsionPropertyEvidence) ||
                string.IsNullOrWhiteSpace(s.WarpingPropertyEvidence) ||
                s.YoungModulusMpa is not double e || s.ShearModulusMpa is not double g ||
                s.SaintVenantTorsionCm4 is not double it || s.WarpingConstantCm6 is not double iw)
                return DesignRuleEvaluation.MissingData("Document E,G,It,Iw and every fork/warping/uniform-moment standard-case assumption.");
            var standardMcr = CalculateForkUniformMomentMcr(e, g, p.WeakInertiaCm4, it, iw,
                a.SegmentEndMetres - a.SegmentStartMetres);
            if (standardMcr is null)
                return DesignRuleEvaluation.MissingData("Invalid/nonfinite standard-case elastic critical moment.");
            mcr = standardMcr.Value;
            mcrEvidence = $"JRC standard fork-supported uniform end couples, C1=1; E={F(e)}MPa [{s.YoungModulusEvidence}], " +
                $"G={F(g)}MPa [{s.ShearModulusEvidence}], Iz={F(p.WeakInertiaCm4)}cm4 [M88-B2], " +
                $"It={F(it)}cm4 [{s.TorsionPropertyEvidence}], Iw={F(iw)}cm6 [{s.WarpingPropertyEvidence}], " +
                "free end warping; no intermediate restraint; no transverse loads";
        }
        string curve = SelectGeneralRolledICurve(p.HeightMm, p.WidthMm);
        var value = CalculateGeneral(curve, wy, fy, mcr, gamma.Value);
        if (value is null)
            return DesignRuleEvaluation.MissingData("Nonfinite EC3 LTB reduction or member resistance.");
        var trace = $"M91-C SCOPED LTB CHECK (NOT complete member compliance) | {Clause} | " +
            $"member={a.MemberId} section={a.SectionId} grade={a.SteelGrade} " +
            $"class={classification.Class} classSource={a.ClassificationEvidence}, M90A independently confirmed | " +
            $"geometry={p.SourcePageUrl} status={p.DataStatus} (indicative, NOT certified) | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId} " +
            $"generalCurveAnnex={a.GeneralCurveAndAnnexEvidence} | " +
            $"segment=[{F(a.SegmentStartMetres)},{F(a.SegmentEndMetres)}]m supports={a.SegmentRestraintEvidence} " +
            $"loadPattern={a.MomentDiagramAndLoadHeightEvidence} | " +
            $"W_y={F(wy)}cm3 W_source={wEvidence} fy={F(fy)}MPa " +
            $"gamma_M1={F(gamma.Value)} source={gamma.SourceClause} | " +
            $"Mcr={F(mcr)}kNm source={mcrEvidence} | " +
            $"curve={curve} alphaLT={F(value.AlphaLt)} lambdaLT={F(value.LambdaBarLt)} " +
            $"phiLT={F(value.PhiLt)} chiLT={F(value.ChiLt)} " +
            $"MbRd={F(value.BucklingResistanceKnm)}kNm | " +
            $"MEd={F(m)}kNm target={effects.CombinationId} analysis={effects.AnalysisId} " +
            $"station={F(effects.PositionMetres)}m side={effects.SectionSide} | " +
            "NOT CHECKED: full moment envelope, shear/axial interactions, local shear buckling, " +
            "lateral restraint/connection capacity, erection stages, torsional beam deformations, " +
            "NTC compliance, code alternatives 6.3.2.3, class 4, whole-member approval.";
        return DesignRuleEvaluation.Calculated(Math.Abs(m), value.BucklingResistanceKnm, "kNm", trace);
    }

    /// <summary>EC3 6.3.2.2 Table 6.4, ROLLED doubly symmetric I, GENERAL curve only.</summary>
    public static string SelectGeneralRolledICurve(double heightMm, double widthMm)
    {
        if (!ValidPositive(heightMm) || !ValidPositive(widthMm))
            throw new ArgumentOutOfRangeException(nameof(heightMm), "Positive finite height/width required.");
        return heightMm / widthMm <= 2d ? "a" : "b";
    }

    /// <summary>
    /// General EC3 6.3.2.2 reduction (not the alternative 6.3.2.3):
    /// lambda=sqrt(Wy*fy/Mcr), phi=(1+alpha(lambda-0.2)+lambda^2)/2,
    /// chi=min(1,1/(phi+sqrt(phi^2-lambda^2))). W in cm3, fy in MPa, M in kNm.
    /// Null when the computation is outside finite positive valid inputs.
    /// </summary>
    public static SteelLtbGeneralCalculationM91C? CalculateGeneral(
        string curve, double strongSectionModulusCm3, double fyMpa,
        double mcrKnm, double gammaM1)
    {
        if (!ValidPositive(strongSectionModulusCm3) || !ValidPositive(fyMpa) ||
            !ValidPositive(mcrKnm) || !ValidPositive(gammaM1)) return null;
        double alpha = curve switch { "a" => 0.21, "b" => 0.34, "c" => 0.49, "d" => 0.76, _ => double.NaN };
        if (!double.IsFinite(alpha)) return null;
        double mSection = strongSectionModulusCm3 * fyMpa / 1000d;
        if (!ValidPositive(mSection)) return null;
        double slenderness = Math.Sqrt(mSection / mcrKnm);
        double phi = 0.5d * (1 + alpha * (slenderness - 0.2d) + slenderness * slenderness);
        double radicand = phi * phi - slenderness * slenderness;
        if (!double.IsFinite(slenderness) || !double.IsFinite(phi) || !double.IsFinite(radicand)) return null;
        if (radicand < 0 && radicand > -1e-12) radicand = 0d;
        if (radicand < 0) return null;
        double denominator = phi + Math.Sqrt(radicand);
        if (!ValidPositive(denominator)) return null;
        double chi = Math.Min(1d, 1d / denominator);
        double capacity = chi * mSection / gammaM1;
        if (!ValidPositive(chi) || !ValidPositive(capacity)) return null;
        return new SteelLtbGeneralCalculationM91C(curve, alpha, slenderness,
            phi, chi, mcrKnm, mSection / gammaM1, capacity);
    }

    /// <summary>
    /// Simply/fork supported doubly symmetric I beam with free warping at both ends,
    /// no intermediate braces, uniform major-axis end couples, C1=1, k=kw=1.
    /// Mcr = (pi/L)*sqrt(E*Iz*(G*It + pi^2*E*Iw/L^2)).
    /// Units: MPa=N/mm2; Iz,It cm4; Iw cm6; L m; result kNm.
    /// Verified against the JRC Eurocodes workshop (2014) standard case.
    /// </summary>
    public static double? CalculateForkUniformMomentMcr(
        double youngMpa, double shearMpa, double weakInertiaCm4,
        double torsionCm4, double warpingCm6, double unbracedLengthMetres)
    {
        if (!ValidPositive(youngMpa) || !ValidPositive(shearMpa) ||
            !ValidPositive(weakInertiaCm4) || !ValidPositive(torsionCm4) ||
            !ValidPositive(warpingCm6) || !ValidPositive(unbracedLengthMetres)) return null;
        double Lmm = unbracedLengthMetres * 1e3;
        double eiz = youngMpa * weakInertiaCm4 * 1e4;
        double git = shearMpa * torsionCm4 * 1e4;
        double eiw = youngMpa * warpingCm6 * 1e6;
        double secondTerm = Math.PI * Math.PI * eiw / (Lmm * Lmm);
        double mcrNmm = (Math.PI / Lmm) * Math.Sqrt(eiz * (git + secondTerm));
        double mcrKnm = mcrNmm / 1e6;
        return ValidPositive(mcrKnm) ? mcrKnm : null;
    }

    private static bool ValidPositive(double x) => double.IsFinite(x) && x > 0;
    private static bool FiniteNonnegative(double x) => double.IsFinite(x) && x >= 0;
    private static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
}
