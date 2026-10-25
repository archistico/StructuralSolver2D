using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Native NTC2018 class 1..4 of ONE rolled I/H cross-section in the
/// COMPRESSION-DOMINANT mixed N/M branch where the nominal plastic neutral
/// axis is strictly beyond the straight web TOWARD THE TENSION FACE.
/// Never a section resistance, stability, interaction or member check.
/// </summary>
public sealed record Ntc2018SteelMixedCompressionDominantClassD0M7(
    Ntc2018SteelPlasticNeutralAxisD0M6 Plastic,
    Ntc2018SteelMixedElasticWebScreenD0M3 Elastic,
    Ntc2018SteelPlateClassificationD0 Flange,
    Ntc2018SteelPlateClassificationD0 Web,
    double Epsilon, string Trace)
{
    public int ClassWithinDeclaredScope => Math.Max(Flange.Class, Web.Class);
    public string GoverningPart => Flange.Class == Web.Class ? "FlangeAndWeb" :
        Flange.Class > Web.Class ? "Flange" : "Web";
    public bool RequiresEffectiveSectionProperties => ClassWithinDeclaredScope == 4;
    public bool EstablishesSectionResistance => false;
    public bool EstablishesNmOrShearInteraction => false;
    public bool EstablishesCompleteMemberCompliance => false;
    public bool EstablishesWholeStructureCompliance => false;
}

/// <summary>
/// P0-D0M7, restricted endpoint alpha=1 of NTC2018 §4.2.3.1 Table 4.2.III.
/// Since the plastic compressed zone covers the ENTIRE flat web, the mixed
/// alpha>0.5 limits reduce algebraically to:
/// class1: 396*eps/(13*1-1)=33*eps;
/// class2: 456*eps/(13*1-1)=38*eps.
/// Class3 still depends on the INDEPENDENT elastic psi from D0M3:
/// class3: 42*eps/(0.67+0.33*psi), strictly -1<psi<1, FEM N<0.
/// Rolled outstand flange limits are 9/10/14*eps (Table 4.2.IV).
/// Unlike D0M1's externally evidenced 0<alpha<1 branch, this endpoint MUST
/// be physically produced and proven by D0M6 geometry, not supplied by hand.
/// The tension-dominant alpha=0 and other regimes remain Unsupported.
/// </summary>
public static class Ntc2018SteelMixedCompressionDominantClassifierD0M7
{
    public static DesignCodeEdition Code => Ntc2018SteelISectionClassifierD0.Code;

    public static Ntc2018SteelMixedCompressionDominantClassD0M7 ClassifyCatalogProfile(
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
            throw new NotSupportedException("D0M7 covers only nominal rolled IPE/HEA/HEB; INP, fabricated or asymmetric profiles unsupported.");
        double fy = materials.GetSteelYieldMpa(steelGrade,
            Math.Max(profile.FlangeThicknessMm, profile.WebThicknessMm));
        return ClassifyGeometry(request,
            new Ntc2018SteelISectionDimensionsD0(profile.HeightMm,
                profile.WidthMm, profile.WebThicknessMm,
                profile.FlangeThicknessMm, profile.RootRadiusMm),
            new Ntc2018SteelGrossElasticPropertiesD0M3(
                profile.AreaCm2 * 100d, profile.StrongInertiaCm4 * 10000d,
                $"OPPO {profile.SourcePageUrl}; {profile.DataStatus} (NOT CERTIFIED)"),
            steelGrade, fy, evidence);
    }

    public static Ntc2018SteelMixedCompressionDominantClassD0M7 ClassifyGeometry(
        DesignCheckRequest request, Ntc2018SteelISectionDimensionsD0 dimensions,
        Ntc2018SteelGrossElasticPropertiesD0M3 grossProperties,
        string steelGrade, double yieldStrengthMpa,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Effects);
        ArgumentNullException.ThrowIfNull(grossProperties);
        if (request.Effects.AxialForceKn is not double n || !double.IsFinite(n) || n >= 0)
            throw new NotSupportedException("D0M7 accepts ONLY axial COMPRESSION (FEM N<0), never alpha=0/tension.");

        // D0M6 and D0M3 independently validate all geometry, source/edition,
        // real FEM station, same-section force signs and their own domain.
        var plastic = Ntc2018SteelPlasticNeutralAxisCalculatorD0M6.CalculateGeometry(
            request, dimensions,
            new Ntc2018SteelGrossPlasticAreaD0M4(
                grossProperties.GrossAreaMm2, grossProperties.PropertiesEvidence),
            steelGrade, yieldStrengthMpa, evidence);
        var elastic = Ntc2018SteelMixedElasticWebScreenerD0M3.ScreenGeometry(
            request, dimensions, grossProperties, steelGrade, yieldStrengthMpa,
            evidence);

        double c = plastic.FlatWebDepthMm;
        double nFlatLimit = yieldStrengthMpa * plastic.WebThicknessMm * c / 1000;
        // The nominal PNA must be CLEARLY outside the flat web; avoid a
        // floating-point tie at the D0M4/D0M5 interface.
        if (n >= -nFlatLimit * (1 + 1e-10) ||
            plastic.Region is not (
                Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideRootFillet or
                Ntc2018SteelPlasticNeutralAxisRegionD0M6.TensionSideFlange) ||
            plastic.PlasticCompressedFlatWebFractionAlpha != 1 ||
            !(elastic.Psi > -1 && elastic.Psi < 1))
            throw new NotSupportedException("Only compression-dominant alpha=1 with PNA beyond the straight web and -1<psi<1 is supported.");

        if (plastic.Code != Code || elastic.Code != Code ||
            plastic.MemberId != elastic.MemberId ||
            plastic.SectionId != elastic.SectionId ||
            plastic.SteelGrade != elastic.SteelGrade ||
            plastic.AnalysisId != elastic.AnalysisId ||
            plastic.CombinationId != elastic.CombinationId ||
            plastic.PositionMetres != elastic.PositionMetres ||
            plastic.Side != elastic.Side ||
            plastic.SignedAxialForceKn != elastic.SignedAxialForceKn ||
            plastic.SignedStrongAxisMomentKnm != elastic.SignedMomentKnm ||
            plastic.YieldStrengthMpa != elastic.YieldStrengthMpa ||
            plastic.PublishedGrossAreaMm2 != grossProperties.GrossAreaMm2 ||
            !double.IsFinite(c / plastic.WebThicknessMm) ||
            Math.Abs(c / plastic.WebThicknessMm - elastic.WebSlenderness) >
                1e-12 * Math.Max(1, elastic.WebSlenderness))
            throw new NotSupportedException("D0M6 plastic and D0M3 elastic results do not share exactly the same code/geometry/grade/FEM station/effects.");

        double epsilon = Math.Sqrt(235d / yieldStrengthMpa);
        double webRatio = c / dimensions.WebThicknessMm;
        double flangeFlat = (dimensions.WidthMm - dimensions.WebThicknessMm -
            2 * dimensions.RootRadiusMm) / 2;
        double flangeRatio = flangeFlat / dimensions.FlangeThicknessMm;

        double webLimit1 = 33 * epsilon;
        double webLimit2 = 38 * epsilon;
        double webLimit3 = 42 * epsilon / (0.67 + 0.33 * elastic.Psi);
        if (!double.IsFinite(epsilon) || !double.IsFinite(webRatio) ||
            !double.IsFinite(flangeRatio) || !double.IsFinite(webLimit3) ||
            !(webLimit1 > 0 && webLimit2 > webLimit1 && webLimit3 >= webLimit2) ||
            !(flangeRatio > 0))
            throw new NotSupportedException("Invalid or nonordered native NTC2018 endpoint plate classification limits.");

        var web = Plate("InternalWeb", c, dimensions.WebThicknessMm,
            webRatio, webLimit1, webLimit2, webLimit3);
        var flange = Plate("OutstandFlange", flangeFlat, dimensions.FlangeThicknessMm,
            flangeRatio, 9 * epsilon, 10 * epsilon, 14 * epsilon);
        if ((web.Class == 4) != (elastic.ConservativeWebClassUpperBound == 4))
            throw new NotSupportedException("Endpoint class-3 boundary contradicts independent elastic D0M3 web screen.");

        string trace = $"NATIVE NTC2018 §4.2.3.1 Tab.4.2.III/IV, compression-dominant alpha=1 ENDPOINT ONLY; " +
            $"member={plastic.MemberId} section={plastic.SectionId} steel={plastic.SteelGrade} " +
            $"FEM={plastic.AnalysisId}/{plastic.CombinationId} x={F(plastic.PositionMetres)}m side={plastic.Side}; " +
            $"N={F(n)}kN(tension+) M={F(plastic.SignedStrongAxisMomentKnm)}kNm V=0; " +
            $"PNA={plastic.Region} d={F(plastic.NeutralAxisDepthFromCompressionFaceMm)}mm alpha=1 [D0M6]; " +
            $"psi={F(elastic.Psi)} [D0M3 independent gross A/I]; eps={F(epsilon)}; " +
            $"web c/t={F(webRatio)} class limits {F(webLimit1)}/{F(webLimit2)}/{F(webLimit3)} => {web.Class}; " +
            $"flange c/t={F(flangeRatio)} class limits {F(flange.Class1Limit)}/{F(flange.Class2Limit)}/{F(flange.Class3Limit)} => {flange.Class}; " +
            $"sectionClass={Math.Max(flange.Class, web.Class)}; " +
            $"sourceArea={grossProperties.PropertiesEvidence}; geometry={evidence.GeometryEvidence}; " +
            $"nominalFilletAreaDiscrepancy={F(plastic.NominalAreaRelativeDiscrepancy)}; " +
            "NO claim of resistance, N/M/V interaction, class4 effective section, member buckling, " +
            "connections, full-station/combination coverage or structural compliance. " +
            "OPPO nominal tables NOT certified product geometry; alpha=0/tension is Unsupported.";
        return new Ntc2018SteelMixedCompressionDominantClassD0M7(
            plastic, elastic, flange, web, epsilon, trace);
    }

    private static Ntc2018SteelPlateClassificationD0 Plate(string part, double c,
        double t, double ratio, double l1, double l2, double l3) =>
        new(part, c, t, ratio, l1, l2, l3,
            ratio <= l1 ? 1 : ratio <= l2 ? 2 : ratio <= l3 ? 3 : 4);

    private static string F(double value) =>
        value.ToString("G17", CultureInfo.InvariantCulture);
}
