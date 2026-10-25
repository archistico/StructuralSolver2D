using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Published gross area, independent of the web force-balance computation.
/// It checks section plausibility and axial yield feasibility; it does not
/// replace a certificate of the actual hot-rolled geometry.
/// </summary>
public sealed record Ntc2018SteelGrossPlasticAreaD0M4(
    double GrossAreaMm2, string? GrossAreaEvidence);

/// <summary>
/// A plastic stress-block GEOMETRY diagnostic for the flat web only.
/// The yielded stress block is an auxiliary classification construction, NOT
/// the actual elastic FEM stress field or a proof of applied N/M resistance.
/// </summary>
public sealed record Ntc2018SteelMixedPlasticWebAlphaD0M4(
    DesignCodeEdition Code, string MemberId, string SectionId, string SteelGrade,
    string AnalysisId, string CombinationId, double PositionMetres,
    DesignEffectSectionSide Side, double SignedAxialForceKn,
    double SignedStrongAxisMomentKnm, double YieldStrengthMpa,
    double GrossAreaMm2, double FlatWebDepthMm, double WebThicknessMm,
    double PlasticNeutralAxisShiftTowardTensionEdgeMm,
    double PlasticCompressedFractionAlpha,
    double AxialForceLimitForPlasticNeutralAxisInFlatWebKn,
    double GrossAxialYieldForceKn, string Trace)
{
    public bool EstablishesActualMomentResistance => false;
    public bool EstablishesElasticPsi => false;
    public bool EstablishesDefinitiveSectionClass => false;
    public bool EstablishesCompleteMemberCompliance => false;
}

/// <summary>
/// NTC 2018 §4.2.3.1, Table 4.2.III: construct the plastic compressed web
/// fraction alpha from axial-force equilibrium of a doubly symmetric rolled
/// I/H section, ONLY while the plastic neutral axis is inside the straight
/// portion of the web (between both root fillets). Under these assumptions
/// symmetric flange/fillet areas cancel in the difference A_tension-A_compression:
/// N[N] = fy * (A_tension-A_compression) = -2*fy*tw*delta,
/// alpha = 1/2 + delta/c = 1/2 - 1000*N[kN]/(2*fy*tw*c).
/// The FEM bending moment chooses which web edge is the compression edge;
/// its magnitude is NOT claimed to equal the moment of this fully yielded block.
/// </summary>
public static class Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4
{
    public static DesignCodeEdition Code => Ntc2018SteelISectionClassifierD0.Code;

    public static Ntc2018SteelMixedPlasticWebAlphaD0M4 CalculateCatalogProfile(
        DesignCheckRequest request, CommercialRolledSteelCatalogM88B2 profiles,
        CharacteristicMaterialCatalogM88C materials, string steelGrade,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Member);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(materials);
        if (string.IsNullOrWhiteSpace(steelGrade))
            throw new ArgumentException("Documented steel grade required.", nameof(steelGrade));
        var profile = profiles.Find(request.Member.SectionId).Definition;
        if (profile.Family is not ("IPE" or "HEA" or "HEB"))
            throw new NotSupportedException("Only hot-rolled parallel-flange IPE/HEA/HEB; tapered INP, fabricated and asymmetric sections unsupported.");
        double fy = materials.GetSteelYieldMpa(steelGrade,
            Math.Max(profile.FlangeThicknessMm, profile.WebThicknessMm));
        return CalculateGeometry(request,
            new Ntc2018SteelISectionDimensionsD0(profile.HeightMm,
                profile.WidthMm, profile.WebThicknessMm,
                profile.FlangeThicknessMm, profile.RootRadiusMm),
            new Ntc2018SteelGrossPlasticAreaD0M4(profile.AreaCm2 * 100d,
                $"OPPO {profile.SourcePageUrl}; {profile.DataStatus} (NOT CERTIFIED)"),
            steelGrade, fy, evidence);
    }

    /// <summary>
    /// The declared gross area must be compatible with rectangular flange/web
    /// material and additional positive fillet material within the r-by-r
    /// root boxes. A 1% nominal-data allowance is used ONLY for this secondary
    /// plausibility screen, never in the alpha force balance.
    /// </summary>
    public static Ntc2018SteelMixedPlasticWebAlphaD0M4 CalculateGeometry(
        DesignCheckRequest request, Ntc2018SteelISectionDimensionsD0 dimensions,
        Ntc2018SteelGrossPlasticAreaD0M4 gross,
        string steelGrade, double yieldStrengthMpa,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence)
    {
        ValidateFem(request);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(gross);
        ArgumentNullException.ThrowIfNull(evidence);
        if (string.IsNullOrWhiteSpace(steelGrade) ||
            evidence.UnperforatedConfirmed != true ||
            evidence.HotRolledParallelFlangeDoublySymmetricConfirmed != true ||
            evidence.StrongAxisMappedToFemMomentConfirmed != true ||
            string.IsNullOrWhiteSpace(evidence.GeometryEvidence) ||
            string.IsNullOrWhiteSpace(evidence.GradeAndYieldEvidence) ||
            string.IsNullOrWhiteSpace(gross.GrossAreaEvidence))
            throw new NotSupportedException("Documented symmetric, unperforated hot-rolled geometry, strong-axis mapping, yield and sourced gross area required.");

        Positive(dimensions.HeightMm, nameof(dimensions.HeightMm));
        Positive(dimensions.WidthMm, nameof(dimensions.WidthMm));
        Positive(dimensions.WebThicknessMm, nameof(dimensions.WebThicknessMm));
        Positive(dimensions.FlangeThicknessMm, nameof(dimensions.FlangeThicknessMm));
        Positive(dimensions.RootRadiusMm, nameof(dimensions.RootRadiusMm));
        Positive(gross.GrossAreaMm2, nameof(gross.GrossAreaMm2));
        Positive(yieldStrengthMpa, nameof(yieldStrengthMpa));

        double c = dimensions.HeightMm - 2 *
            (dimensions.FlangeThicknessMm + dimensions.RootRadiusMm);
        double flangeFlat = (dimensions.WidthMm - dimensions.WebThicknessMm -
            2 * dimensions.RootRadiusMm) / 2d;
        double rectangularArea = 2 * dimensions.WidthMm * dimensions.FlangeThicknessMm +
            dimensions.WebThicknessMm * (dimensions.HeightMm - 2 * dimensions.FlangeThicknessMm);
        double possibleRootArea = 4 * dimensions.RootRadiusMm * dimensions.RootRadiusMm;
        double areaAllowance = 0.01 * rectangularArea;
        if (!double.IsFinite(c) || c <= 0 ||
            !double.IsFinite(flangeFlat) || flangeFlat <= 0 ||
            !double.IsFinite(rectangularArea) || rectangularArea <= 0 ||
            !double.IsFinite(possibleRootArea) || !double.IsFinite(areaAllowance) ||
            !double.IsFinite(rectangularArea + possibleRootArea + areaAllowance) ||
            gross.GrossAreaMm2 < rectangularArea - areaAllowance ||
            gross.GrossAreaMm2 > rectangularArea + possibleRootArea + areaAllowance ||
            gross.GrossAreaMm2 >= dimensions.HeightMm * dimensions.WidthMm)
            throw new ArgumentException("Unphysical or incompatible rolled I/H flange, flat web, fillet and gross-area dimensions.");

        double n = request.Effects.AxialForceKn!.Value;
        double m = request.Effects.BendingMomentZKnm!.Value;
        double webStripArea = dimensions.WebThicknessMm * c;
        double nFlatLimitKn = yieldStrengthMpa * webStripArea / 1000d;
        double grossYieldKn = yieldStrengthMpa * gross.GrossAreaMm2 / 1000d;
        double delta = -1000d * n / (2 * yieldStrengthMpa * dimensions.WebThicknessMm);
        double alpha = 0.5 + delta / c;

        // Strict interior is necessary: once the plastic neutral axis enters
        // either root region, cross-sectional areas depend on its actual curved
        // geometry and this exact cancellation formula no longer applies.
        // A small numerical guard rejects ambiguous tangent-boundary states.
        const double boundaryGuard = 1e-10;
        if (!double.IsFinite(webStripArea) || !double.IsFinite(nFlatLimitKn) ||
            !double.IsFinite(grossYieldKn) || !double.IsFinite(delta) ||
            !double.IsFinite(alpha) || nFlatLimitKn <= 0 || grossYieldKn <= 0 ||
            Math.Abs(n) >= nFlatLimitKn * (1 - boundaryGuard) ||
            Math.Abs(n) >= grossYieldKn || !(alpha > 0 && alpha < 1) ||
            (n < 0 && alpha <= 0.5) || (n > 0 && alpha >= 0.5))
            throw new NotSupportedException("Plastic neutral axis must be strictly inside the straight web; flanges/fillets, yield-capacity exceedance and boundary states require a different method.");

        var e = request.Effects;
        string trace = $"NTC2018 §4.2.3.1 Tab.4.2.III PLASTIC WEB ALPHA GEOMETRY ONLY | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId}; " +
            $"member={request.Member.MemberId} section={request.Member.SectionId} grade={steelGrade} " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(e.PositionMetres)} side={e.SectionSide}; " +
            $"N={F(n)}kN(tension+) M={F(m)}kNm V={F(e.ShearForceYKn!.Value)}kN; " +
            $"fyk={F(yieldStrengthMpa)}MPa grossA={F(gross.GrossAreaMm2)}mm2; " +
            $"flatWebC={F(c)}mm tw={F(dimensions.WebThicknessMm)}mm " +
            $"N_flat_limit={F(nFlatLimitKn)}kN N_gross_yield={F(grossYieldKn)}kN " +
            $"plasticNA_shift_toward_tension_edge={F(delta)}mm alpha={F(alpha)}; " +
            $"grossSource={gross.GrossAreaEvidence}; geometry={evidence.GeometryEvidence}; grade={evidence.GradeAndYieldEvidence}; " +
            "Fully yielded plastic stress block is an auxiliary CLASSIFICATION construct with axial equilibrium, " +
            "NOT proof that actual FEM M is equal to its plastic moment or that N/M interaction passes. " +
            "NOT elastic psi, plastic class 1/2, whole section class, resistance, stability, effective section or member compliance.";
        return new Ntc2018SteelMixedPlasticWebAlphaD0M4(Code,
            request.Member.MemberId, request.Member.SectionId, steelGrade,
            e.AnalysisId, e.CombinationId, e.PositionMetres, e.SectionSide!.Value,
            n, m, yieldStrengthMpa, gross.GrossAreaMm2, c,
            dimensions.WebThicknessMm, delta, alpha, nFlatLimitKn,
            grossYieldKn, trace);
    }

    private static void ValidateFem(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Profile?.Code != Code)
            throw new NotSupportedException("Native NTC2018 edition identity required: EC3 is not NTC2018.");
        if (request.Member is null || request.Member.Material != DesignMaterial.Steel ||
            request.LimitState != DesignLimitState.Ultimate ||
            string.IsNullOrWhiteSpace(request.Member.MemberId) ||
            string.IsNullOrWhiteSpace(request.Member.SectionId) ||
            !double.IsFinite(request.Member.MemberLengthMetres) ||
            request.Member.MemberLengthMetres <= 0)
            throw new ArgumentException("Documented steel SLU member required.");
        var e = request.Effects;
        if (e is null || e.Origin != DesignEffectOrigin.FemAnalysis ||
            !string.Equals(e.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            e.TargetKind != DesignEffectTargetKind.LoadCombination || e.SectionSide is null ||
            string.IsNullOrWhiteSpace(e.AnalysisId) || string.IsNullOrWhiteSpace(e.CombinationId) ||
            !double.IsFinite(e.PositionMetres) || e.PositionMetres < 0 ||
            e.PositionMetres > request.Member.MemberLengthMetres)
            throw new ArgumentException("Exact same FEM member, combination, station and side required.");
        if (e.AxialForceKn is not double n || e.BendingMomentZKnm is not double m ||
            e.ShearForceYKn is not double v || !double.IsFinite(n) ||
            !double.IsFinite(m) || !double.IsFinite(v) || n == 0 || m == 0 || v != 0)
            throw new NotSupportedException("Only concomitant signed nonzero N/M strong-axis with V=0 is supported.");
    }

    private static void Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, value, "Finite positive number required.");
    }

    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
