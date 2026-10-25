using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// A location of nominal FIRST-ORDER ELASTIC compression when the independent
/// D0M6 PLASTIC area equilibrium proves alpha=0 (tension-dominant N/M).
/// Neither the elastic location nor alpha=0 is an NTC section class.
/// </summary>
public enum Ntc2018SteelTensionDominantElasticRegionD0M8
{
    NoCompressedElasticFibres,
    CompressionConfinedToFlangeOrRoot,
    CompressionReachesFlatWeb
}

/// <summary>
/// D0M8 conservatively separates physically distinct alpha=0 states; it does
/// not assign section classes where NTC2018 table limits need interpretation.
/// </summary>
public sealed record Ntc2018SteelTensionDominantElasticZoneScreenD0M8(
    Ntc2018SteelPlasticNeutralAxisD0M6 Plastic,
    Ntc2018SteelTensionDominantElasticRegionD0M8 ElasticRegion,
    Ntc2018SteelMixedElasticWebScreenD0M3? WebClassThreeScreen,
    double GrossAreaMm2, double StrongAxisInertiaMm4,
    double UniformElasticStressMpa,
    double MomentStressAtExtremeFibreMpa,
    double MaxSignedExtremeFibreStressMpa,
    double MaxSignedFlatWebEdgeStressMpa,
    double GuardBandStressMpa, string Trace)
{
    public bool EstablishesDefinitiveNtcSectionClass => false;
    public bool EstablishesPlasticClassOneOrTwo => false;
    public bool EstablishesSectionResistance => false;
    public bool EstablishesNmOrShearInteraction => false;
    public bool EstablishesCompleteMemberCompliance => false;
    public bool EstablishesWholeStructureCompliance => false;
}

/// <summary>
/// P0-D0M8. NTC2018 §4.2.3.1 Table 4.2.III mixed web class-1/2
/// expressions for alpha<=0.5 have alpha in the denominator. alpha=0 is not
/// blindly promoted to a class through division by zero or an infinity.
/// D0M6 establishes the physical plastic endpoint independently; the elastic
/// stress extrema from the SAME signed N/M FEM station and documented gross
/// A/I establish which plates may be compressed. If the flat web has a
/// compressed edge, the EXISTING D0M3 class-3 web screen is required,
/// including its psi<-1 footnote conditions. All outcomes remain diagnostic.
/// </summary>
public static class Ntc2018SteelTensionDominantElasticZonesD0M8
{
    public static DesignCodeEdition Code => Ntc2018SteelISectionClassifierD0.Code;

    public static Ntc2018SteelTensionDominantElasticZoneScreenD0M8 ScreenCatalogProfile(
        DesignCheckRequest request, CommercialRolledSteelCatalogM88B2 profiles,
        CharacteristicMaterialCatalogM88C materials, string steelGrade,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence,
        Ntc2018SteelTensileStrainEvidenceD0M3? tensileStrainEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Member);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(materials);
        if (string.IsNullOrWhiteSpace(steelGrade))
            throw new ArgumentException("Documented steel grade required.", nameof(steelGrade));
        var profile = profiles.Find(request.Member.SectionId).Definition;
        if (profile.Family is not ("IPE" or "HEA" or "HEB"))
            throw new NotSupportedException("Only nominal rolled IPE/HEA/HEB; INP, welded and asymmetric profiles unsupported.");
        double fy = materials.GetSteelYieldMpa(steelGrade,
            Math.Max(profile.FlangeThicknessMm, profile.WebThicknessMm));
        return ScreenGeometry(request,
            new Ntc2018SteelISectionDimensionsD0(profile.HeightMm,
                profile.WidthMm, profile.WebThicknessMm,
                profile.FlangeThicknessMm, profile.RootRadiusMm),
            new Ntc2018SteelGrossElasticPropertiesD0M3(
                profile.AreaCm2 * 100d, profile.StrongInertiaCm4 * 10000d,
                $"OPPO {profile.SourcePageUrl}; {profile.DataStatus} (NOT CERTIFIED)"),
            steelGrade, fy, evidence, tensileStrainEvidence);
    }

    public static Ntc2018SteelTensionDominantElasticZoneScreenD0M8 ScreenGeometry(
        DesignCheckRequest request, Ntc2018SteelISectionDimensionsD0 dimensions,
        Ntc2018SteelGrossElasticPropertiesD0M3 grossProperties,
        string steelGrade, double yieldStrengthMpa,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence,
        Ntc2018SteelTensileStrainEvidenceD0M3? tensileStrainEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(grossProperties);
        if (request.Effects?.AxialForceKn is not double n ||
            !double.IsFinite(n) || n <= 0)
            throw new NotSupportedException("D0M8 covers ONLY FEM tension N>0 and alpha=0.");

        // D0M6 validates FEM station, N/M/V, code edition, material, rolled
        // unperforated symmetric dimensions, grade, gross area and physical PNA.
        var plastic = Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateGeometry(
            request, dimensions,
            new Ntc2018SteelGrossPlasticAreaD0M4(
                grossProperties.GrossAreaMm2, grossProperties.PropertiesEvidence),
            steelGrade, yieldStrengthMpa, evidence);
        double nFlatLimit = yieldStrengthMpa * plastic.WebThicknessMm *
            plastic.FlatWebDepthMm / 1000;
        if (n <= nFlatLimit * (1 + 1e-10) ||
            plastic.Region is not (
                Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideRootFillet or
                Ntc2018SteelPlasticNeutralAxisRegionD0M6.CompressionSideFlange) ||
            plastic.PlasticCompressedFlatWebFractionAlpha != 0)
            throw new NotSupportedException("Only alpha=0 with plastic PNA CLEARLY beyond the compressed-side flat web is supported.");

        // D0M3 cannot be called when the elastic flat web has no compressed
        // edge. Validate gross A/I independently for those valid scenarios.
        double area = grossProperties.GrossAreaMm2;
        double inertia = grossProperties.StrongAxisInertiaMm4;
        double h = dimensions.HeightMm;
        double c = plastic.FlatWebDepthMm;
        double inertiaUpperPhysicalBound = area * (h / 2) * (h / 2);
        // The two full rectangular flanges and central straight web alone
        // contribute at least this strong-axis inertia. The nominal circular
        // roots add positive inertia, so physically sourced I cannot be below
        // this bound (allow 0.1% merely for published rounding).
        double b = dimensions.WidthMm;
        double tf = dimensions.FlangeThicknessMm;
        double tw = dimensions.WebThicknessMm;
        double flangeCentroidDistance = h / 2 - tf / 2;
        double rectWebHeight = h - 2 * tf;
        double inertiaLowerPhysicalBound =
            2 * (b * tf * tf * tf / 12 + b * tf * flangeCentroidDistance * flangeCentroidDistance) +
            tw * rectWebHeight * rectWebHeight * rectWebHeight / 12;
        if (string.IsNullOrWhiteSpace(grossProperties.PropertiesEvidence) ||
            !double.IsFinite(area) || area <= 0 ||
            !double.IsFinite(inertia) || inertia <= 0 ||
            !double.IsFinite(inertiaUpperPhysicalBound) ||
            !double.IsFinite(inertiaLowerPhysicalBound) ||
            inertia < inertiaLowerPhysicalBound * (1 - 0.001) ||
            inertia >= inertiaUpperPhysicalBound)
            throw new NotSupportedException("Documented, finite and physically plausible gross A/I are required.");

        double m = request.Effects!.BendingMomentZKnm!.Value;
        double uniform = -1000 * n / area; // compressive stresses positive
        double momentExtreme = Math.Abs(m) * 1e6 * (h / 2) / inertia;
        double momentWeb = Math.Abs(m) * 1e6 * (c / 2) / inertia;
        double highExtreme = uniform + momentExtreme;
        double highWeb = uniform + momentWeb;
        double guard = 1e-10 * Math.Max(1, Math.Max(yieldStrengthMpa,
            Math.Max(Math.Abs(uniform), momentExtreme)));
        if (!double.IsFinite(uniform) || !double.IsFinite(momentExtreme) ||
            !double.IsFinite(momentWeb) || !double.IsFinite(highExtreme) ||
            !double.IsFinite(highWeb) || !double.IsFinite(guard) || guard <= 0 ||
            momentExtreme <= 0 ||
            Math.Abs(highExtreme) <= guard || Math.Abs(highWeb) <= guard)
            throw new NotSupportedException("First-order elastic compression boundary ambiguous or nonfinite.");

        Ntc2018SteelMixedElasticWebScreenD0M3? web = null;
        Ntc2018SteelTensionDominantElasticRegionD0M8 region;
        if (highExtreme < -guard)
        {
            region = Ntc2018SteelTensionDominantElasticRegionD0M8.NoCompressedElasticFibres;
        }
        else if (highWeb < -guard)
        {
            region = Ntc2018SteelTensionDominantElasticRegionD0M8.CompressionConfinedToFlangeOrRoot;
        }
        else
        {
            region = Ntc2018SteelTensionDominantElasticRegionD0M8.CompressionReachesFlatWeb;
            web = Ntc2018SteelMixedElasticWebScreenerD0M3.ScreenGeometry(
                request, dimensions, grossProperties, steelGrade,
                yieldStrengthMpa, evidence, tensileStrainEvidence);
            if (web.Code != Code || web.MemberId != plastic.MemberId ||
                web.SectionId != plastic.SectionId || web.SteelGrade != plastic.SteelGrade ||
                web.AnalysisId != plastic.AnalysisId ||
                web.CombinationId != plastic.CombinationId ||
                web.PositionMetres != plastic.PositionMetres ||
                web.Side != plastic.Side ||
                web.SignedAxialForceKn != plastic.SignedAxialForceKn ||
                web.SignedMomentKnm != plastic.SignedStrongAxisMomentKnm ||
                web.YieldStrengthMpa != plastic.YieldStrengthMpa ||
                !(web.Psi < -1) ||
                Math.Abs(web.CompressiveEdgeStressMpa - highWeb) > guard)
                throw new NotSupportedException("The independently checked elastic web conflicts with D0M8 FEM/geometry evidence.");
        }

        string trace = $"NTC2018 §4.2.3.1 D0M8 ALPHA-ZERO ELASTIC-ZONE DIAGNOSTIC ONLY; " +
            $"member={plastic.MemberId} section={plastic.SectionId} grade={plastic.SteelGrade} " +
            $"FEM={plastic.AnalysisId}/{plastic.CombinationId} x={F(plastic.PositionMetres)} side={plastic.Side}; " +
            $"N={F(n)}kN(tension+) M={F(m)}kNm V=0; " +
            $"plasticPNA={plastic.Region} alpha=0 [D0M6 nominal geometry]; " +
            $"grossA={F(area)}mm2 Iy={F(inertia)}mm4 source={grossProperties.PropertiesEvidence}; " +
            $"elasticUniformStress={F(uniform)}MPa elasticMomentExtreme={F(momentExtreme)}MPa " +
            $"maxSignedExtremeStress={F(highExtreme)}MPa maxSignedFlatWebStress={F(highWeb)}MPa " +
            $"elasticRegion={region} boundaryGuard={F(guard)}MPa; " +
            $"webClassThreeScreen={(web is null ? "NOT APPLICABLE: web not elastically compressed" : web.ConservativeWebClassUpperBound.ToString(CultureInfo.InvariantCulture))}; " +
            "alpha=0 is NOT substituted into NTC plastic c/t formulas (division by alpha); " +
            "NO definitive class 1-4, flange class, resistance, N/M/V interaction, " +
            "instability, whole-member/station/combination coverage or structure compliance. " +
            "Nominal elastic stresses omit residual stresses and imperfections.";
        return new Ntc2018SteelTensionDominantElasticZoneScreenD0M8(
            plastic, region, web, area, inertia, uniform, momentExtreme,
            highExtreme, highWeb, guard, trace);
    }

    private static string F(double value) =>
        value.ToString("G17", CultureInfo.InvariantCulture);
}
