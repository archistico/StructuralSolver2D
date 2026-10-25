using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Location of a NOMINAL plastic neutral axis on an I/H cross-section.
/// These are geometry regions, not NTC section classes or resistance states.
/// </summary>
public enum Ntc2018SteelPlasticNeutralAxisRegionD0M6
{
    CompressionSideFlange,
    CompressionSideRootFillet,
    StraightWeb,
    TensionSideRootFillet,
    TensionSideFlange
}

/// <summary>
/// Nominal rolled I/H geometry construction at ONE concomitant FEM N/M section.
/// The fillets are modeled as quarter-circular concave roots of the declared r.
/// It is NOT a certified product shape, a stress analysis or a resistance check.
/// </summary>
public sealed record Ntc2018SteelPlasticNeutralAxisD0M6(
    DesignCodeEdition Code, string MemberId, string SectionId, string SteelGrade,
    string AnalysisId, string CombinationId, double PositionMetres,
    DesignEffectSectionSide Side, double SignedAxialForceKn,
    double SignedStrongAxisMomentKnm, double YieldStrengthMpa,
    double PublishedGrossAreaMm2, double NominalModeledAreaMm2,
    double NominalAreaRelativeDiscrepancy,
    double NeutralAxisDepthFromCompressionFaceMm,
    double NeutralAxisShiftTowardTensionFaceMm,
    double CompressedNominalAreaMm2,
    double PlasticCompressedFlatWebFractionAlpha,
    Ntc2018SteelPlasticNeutralAxisRegionD0M6 Region,
    double FlatWebDepthMm, double WebThicknessMm,
    double NominalGrossAxialYieldKn, string Trace)
{
    public bool EstablishesFinalNtcSectionClass => false;
    public bool EstablishesPlasticMomentCapacity => false;
    public bool EstablishesSectionResistance => false;
    public bool EstablishesNmOrShearInteraction => false;
    public bool EstablishesCompleteMemberCompliance => false;
    public bool EstablishesWholeStructureCompliance => false;
}

/// <summary>
/// P0-D0M6: plastic-NA NOMINAL GEOMETRY diagnostic beyond the D0M4 straight
/// web. The gross area is recomputed from explicit rectangular flange/web
/// material and four circular concave-root fillets:
/// A = 2*b*tf + tw*(h-2*tf) + 4*r^2*(1-pi/4).
/// At a depth d from the compression-side face, solve
/// A_compressed(d) = [A - 1000*N/fyk]/2 via monotone bisection.
/// N is positive in tension. The compressed flat-web fraction alpha is
/// clamped to [0,1] ONLY as a diagnostic: D0M1/D0M5 expressly require
/// 0&lt;alpha&lt;1, and no new normative class is inferred at either endpoint.
/// </summary>
public static class Ntc2018SteelPlasticNeutralAxisCalculatorD0M6
{
    public static DesignCodeEdition Code => Ntc2018SteelISectionClassifierD0.Code;

    // Indicative tables round their areas. This guard is against an incorrect
    // geometric model, NOT a manufacturing-tolerance acceptance criterion.
    public const double MaximumNominalAreaRelativeDiscrepancy = 0.001;

    public static Ntc2018SteelPlasticNeutralAxisD0M6 CalculateCatalogProfile(
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
            throw new NotSupportedException("Only doubly symmetric parallel-flange rolled IPE/HEA/HEB; INP, fabricated, other profiles unsupported.");
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

    public static Ntc2018SteelPlasticNeutralAxisD0M6 CalculateGeometry(
        DesignCheckRequest request, Ntc2018SteelISectionDimensionsD0 dimensions,
        Ntc2018SteelGrossPlasticAreaD0M4 publishedGross,
        string steelGrade, double yieldStrengthMpa,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence)
    {
        ValidateFem(request);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(publishedGross);
        ArgumentNullException.ThrowIfNull(evidence);
        if (string.IsNullOrWhiteSpace(steelGrade) ||
            evidence.UnperforatedConfirmed != true ||
            evidence.HotRolledParallelFlangeDoublySymmetricConfirmed != true ||
            evidence.StrongAxisMappedToFemMomentConfirmed != true ||
            string.IsNullOrWhiteSpace(evidence.GeometryEvidence) ||
            string.IsNullOrWhiteSpace(evidence.GradeAndYieldEvidence) ||
            string.IsNullOrWhiteSpace(publishedGross.GrossAreaEvidence))
            throw new NotSupportedException("Independent geometry, holes, axis, material and gross-area evidence required.");

        Positive(dimensions.HeightMm, nameof(dimensions.HeightMm));
        Positive(dimensions.WidthMm, nameof(dimensions.WidthMm));
        Positive(dimensions.WebThicknessMm, nameof(dimensions.WebThicknessMm));
        Positive(dimensions.FlangeThicknessMm, nameof(dimensions.FlangeThicknessMm));
        Positive(dimensions.RootRadiusMm, nameof(dimensions.RootRadiusMm));
        Positive(publishedGross.GrossAreaMm2, nameof(publishedGross.GrossAreaMm2));
        Positive(yieldStrengthMpa, nameof(yieldStrengthMpa));

        double h = dimensions.HeightMm;
        double b = dimensions.WidthMm;
        double tw = dimensions.WebThicknessMm;
        double tf = dimensions.FlangeThicknessMm;
        double r = dimensions.RootRadiusMm;
        double c = h - 2 * (tf + r);
        double outstandFlat = (b - tw - 2 * r) / 2;
        double area = 2 * b * tf + tw * (h - 2 * tf) +
            4 * (1 - Math.PI / 4) * r * r;
        double discrepancy = Math.Abs(area - publishedGross.GrossAreaMm2) / area;
        if (!double.IsFinite(c) || c <= 0 ||
            !double.IsFinite(outstandFlat) || outstandFlat <= 0 ||
            !double.IsFinite(area) || area <= 0 || area >= h * b ||
            !double.IsFinite(discrepancy) ||
            discrepancy > MaximumNominalAreaRelativeDiscrepancy)
            throw new NotSupportedException("Nominal quarter-circular root-fillets do not match independently sourced area/valid I/H geometry within 0.1%; do not extrapolate.");

        double n = request.Effects.AxialForceKn!.Value;
        double m = request.Effects.BendingMomentZKnm!.Value;
        double nYield = yieldStrengthMpa * area / 1000;
        double compressedArea = (area - n * 1000 / yieldStrengthMpa) / 2;
        if (!double.IsFinite(nYield) || !double.IsFinite(compressedArea) ||
            nYield <= 0 || Math.Abs(n) >= nYield * (1 - 1e-10) ||
            !(compressedArea > 0 && compressedArea < area))
            throw new NotSupportedException("Nominal gross plastic axial limit reached or invalid; no internal neutral axis can be established.");

        // The integration below uses one HALF of the doubly symmetric profile;
        // the other half is obtained exactly by complementary symmetry.
        double AreaToDepth(double depth)
        {
            if (depth > h / 2)
                return area - AreaToDepth(h - depth);
            if (depth <= tf)
                return b * depth;
            if (depth <= tf + r)
            {
                double u = depth - tf;
                double t = r - u;
                double circlePrimitive = 0.5 *
                    (t * Math.Sqrt(Math.Max(0, r * r - t * t)) +
                     r * r * Math.Asin(Math.Clamp(t / r, -1, 1)));
                double filletAreaEachSide = r * u - Math.PI * r * r / 4 + circlePrimitive;
                return b * tf + tw * u + 2 * filletAreaEachSide;
            }
            return b * tf + tw * (depth - tf) +
                2 * (1 - Math.PI / 4) * r * r;
        }

        double lo = 0, hi = h;
        for (int iteration = 0; iteration < 96; iteration++)
        {
            double mid = (lo + hi) / 2;
            if (AreaToDepth(mid) < compressedArea)
                lo = mid;
            else
                hi = mid;
        }
        double depth = (lo + hi) / 2;
        double residual = Math.Abs(AreaToDepth(depth) - compressedArea);
        if (!double.IsFinite(depth) || !double.IsFinite(residual) ||
            residual > Math.Max(1e-8, 1e-11 * area))
            throw new NotSupportedException("Plastic axial-area equilibrium did not converge to the required tolerance.");

        double alpha = Math.Clamp((depth - tf - r) / c, 0, 1);
        var region = depth < tf ? Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideFlange :
            depth < tf + r ? Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideRootFillet :
            depth <= h - tf - r ? Ntc2018SteelPlasticNeutralAxisRegionD0M6.StraightWeb :
            depth <= h - tf ? Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideRootFillet :
            Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideFlange;

        // Exact flat-web D0M4 algebra is an independent internal consistency
        // cross-check. In external regions D0M4 intentionally remains UNSUPPORTED.
        if (region == Ntc2018SteelPlasticNeutralAxisRegionD0M6.StraightWeb &&
            Math.Abs(n) < yieldStrengthMpa * tw * c / 1000 * (1 - 1e-10))
        {
            var prior = Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4.CalculateGeometry(
                request, dimensions, publishedGross, steelGrade, yieldStrengthMpa, evidence);
            if (Math.Abs(prior.PlasticCompressedFractionAlpha - alpha) > 1e-9)
                throw new NotSupportedException("D0M6 plastic neutral axis disagrees with established D0M4 flat-web equilibrium.");
        }

        var e = request.Effects;
        string trace = $"NTC2018 D0M6 NOMINAL PLASTIC-NA GEOMETRY ONLY; " +
            $"member={request.Member.MemberId} section={request.Member.SectionId} " +
            $"grade={steelGrade} FEM={e.AnalysisId}/{e.CombinationId} " +
            $"x={F(e.PositionMetres)}m side={e.SectionSide}; " +
            $"N={F(n)}kN (tension+) M={F(m)}kNm V=0kN fyk={F(yieldStrengthMpa)}MPa; " +
            $"h={F(h)} b={F(b)} tw={F(tw)} tf={F(tf)} r={F(r)}mm " +
            $"nominalQuarterCircleFillets=true grossPublished={F(publishedGross.GrossAreaMm2)}mm2 " +
            $"nominalArea={F(area)}mm2 relativeDiscrepancy={F(discrepancy)} " +
            $"depthFromCompressionFace={F(depth)}mm shiftTowardTension={F(depth - h / 2)}mm " +
            $"region={region} flatWebAlpha={F(alpha)} " +
            $"compressedArea={F(compressedArea)}mm2 axialAreaResidual={F(residual)}mm2; " +
            $"geometry={evidence.GeometryEvidence}; gradeEvidence={evidence.GradeAndYieldEvidence}; " +
            $"areaSource={publishedGross.GrossAreaEvidence}; " +
            "alpha=0/1 outside straight web is a diagnostic boundary value, " +
            "NOT a D0M1/D0M5 normative classification. " +
            "NO actual Mpl, N/M/V interaction, section resistance, member stability, " +
            "certified geometry, whole-member/station/combination coverage or structural compliance.";

        return new Ntc2018SteelPlasticNeutralAxisD0M6(
            Code, request.Member.MemberId, request.Member.SectionId,
            steelGrade, e.AnalysisId, e.CombinationId, e.PositionMetres,
            e.SectionSide!.Value, n, m, yieldStrengthMpa,
            publishedGross.GrossAreaMm2, area, discrepancy,
            depth, depth - h / 2, compressedArea, alpha, region,
            c, tw, nYield, trace);
    }

    private static void ValidateFem(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Profile?.Code != Code)
            throw new NotSupportedException("Native NTC2018 edition identity required; EC3 is not NTC2018.");
        if (request.Member is null || request.Member.Material != DesignMaterial.Steel ||
            request.LimitState != DesignLimitState.Ultimate ||
            string.IsNullOrWhiteSpace(request.Member.MemberId) ||
            string.IsNullOrWhiteSpace(request.Member.SectionId) ||
            !double.IsFinite(request.Member.MemberLengthMetres) ||
            request.Member.MemberLengthMetres <= 0)
            throw new ArgumentException("Documented steel SLU member and positive length required.");
        var e = request.Effects;
        if (e is null || e.Origin != DesignEffectOrigin.FemAnalysis ||
            !string.Equals(e.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            e.TargetKind != DesignEffectTargetKind.LoadCombination ||
            e.SectionSide is null || string.IsNullOrWhiteSpace(e.AnalysisId) ||
            string.IsNullOrWhiteSpace(e.CombinationId) ||
            !double.IsFinite(e.PositionMetres) || e.PositionMetres < 0 ||
            e.PositionMetres > request.Member.MemberLengthMetres)
            throw new ArgumentException("Exact documented FEM member/combination/station/side required.");
        if (e.AxialForceKn is not double n || e.BendingMomentZKnm is not double m ||
            e.ShearForceYKn is not double v || !double.IsFinite(n) ||
            !double.IsFinite(m) || !double.IsFinite(v) ||
            n == 0 || m == 0 || v != 0)
            throw new NotSupportedException("Only concomitant signed nonzero N/M and V=0 supported.");
    }

    private static void Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, value, "Finite positive number required.");
    }

    private static string F(double value) =>
        value.ToString("G17", CultureInfo.InvariantCulture);
}
