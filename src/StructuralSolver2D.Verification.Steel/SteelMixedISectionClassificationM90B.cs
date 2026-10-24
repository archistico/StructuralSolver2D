using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// A DECLARED pair of idealized, separately justified plastic and elastic web-stress
/// parameters. Neither parameter can generally be reconstructed uniquely from a
/// single arbitrary (N,M) pair without stating a loading path and limit-state model.
/// This is section classification, never a strength or member-design check.
/// </summary>
public sealed record SteelMixedStressParametersM90B(
    double PlasticCompressedFractionAlpha,
    double ElasticStressRatioPsi,
    string PlasticStressEvidence,
    string ElasticStressEvidence);

public sealed record SteelMixedIClassificationM90B(
    SteelISectionClassificationM90A GeometryReference,
    double Alpha, double Psi,
    SteelIPlateClassificationM90A Flange,
    SteelIPlateClassificationM90A Web,
    string Trace)
{
    public int Class => Math.Max(Flange.Class, Web.Class);
    public bool NeedsEffectiveProperties => Class == 4;
    public bool IsCompleteDesignCheck => false;
    public bool AllowsAutomaticApproval => false;
}

public sealed record SteelMixedBoundaryPointM90B(
    string Transition, double AlphaOrPsi,
    double AxialCompressionKn, double StrongAxisMomentKnm);

/// <summary>
/// Locations of the 1/2, 2/3 and 3/4 WEB-class boundaries in the positive-compression,
/// strong-axis N-M plane. This is a reference domain (Bernuzzi 2018 §4.3.1,
/// equations 4.13-4.16), NOT a complete cross-section domain: flange class, section
/// resistance, class-4 effective properties and choice of loading path remain separate.
/// </summary>
public sealed record SteelMixedWebTransitionDomainM90B(
    SteelMixedBoundaryPointM90B Class1To2,
    SteelMixedBoundaryPointM90B Class2To3,
    SteelMixedBoundaryPointM90B Class3To4,
    int FlangeClass,
    double WebSlenderness,
    double Epsilon,
    string Trace)
{
    public bool IsCompleteResistanceDomain => false;
    public bool AllowsAutomaticApproval => false;
}

/// <summary>
/// A conservative elastic-stress SCREEN of one traceable FEM (N,M) station. A reported
/// class-3 bound means the section is class 3 OR BETTER for the assumed elastic field;
/// it cannot determine whether class 1 or 2 is available. A class-4 screen is NOT a
/// proof of effective properties. Never feed this as definitive EC3 classification.
/// </summary>
public sealed record SteelMixedElasticScreenM90B(
    string MemberId, string SectionId, string AnalysisId, string TargetId,
    DesignEffectTargetKind TargetKind, DesignEffectSectionSide Side,
    double PositionMetres, double SignedAxialKn, double SignedMomentKnm,
    double CompressionEdgeStressMpa, double OppositeEdgeStressMpa,
    double Psi, double Class3SlendernessLimit, double ActualWebSlenderness,
    int ConservativeClassUpperBound, string Trace)
{
    public bool IsDefinitiveSectionClass => false;
    public bool IsCompleteDesignCheck => false;
    public bool AllowsAutomaticApproval => false;
}

/// <summary>
/// EN 1993-1-1:2005+A1:2014, 5.5 Table 5.2 (sheet 1), internal I/H webs;
/// Bernuzzi (Hoepli 2018), §4.3.1 and worked example A4.3. Only hot-rolled,
/// parallel-flange, doubly symmetric I/H, strong-axis, no perforations.
/// </summary>
public static class SteelMixedISectionClassifierM90B
{
    public const string Standard = SteelISectionClassifierM90A.Standard;
    public const string Reference = "EN 1993-1-1:2005+A1:2014 §5.5 Table 5.2; Bernuzzi 2018 §4.3.1";

    public static SteelMixedIClassificationM90B ClassifyDeclaredStressParameters(
        string sectionId, string steelGrade,
        SteelISectionDimensionsM90A dimensions, double fyMpa,
        SteelMixedStressParametersM90B parameters,
        string geometryProvenance, string materialProvenance)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (string.IsNullOrWhiteSpace(parameters.PlasticStressEvidence) ||
            string.IsNullOrWhiteSpace(parameters.ElasticStressEvidence))
            throw new ArgumentException("Both plastic and elastic stress-distribution evidence are mandatory.");
        Positive(fyMpa, nameof(fyMpa));
        double alpha = parameters.PlasticCompressedFractionAlpha;
        double psi = parameters.ElasticStressRatioPsi;
        if (!double.IsFinite(alpha) || alpha <= 0 || alpha > 1 ||
            !double.IsFinite(psi) || psi > 1)
            throw new ArgumentOutOfRangeException(nameof(parameters),
                "The documented compression-positive pattern requires 0<alpha<=1, psi<=1.");
        var basic = SteelISectionClassifierM90A.ClassifyGeometry(sectionId, steelGrade, dimensions,
            fyMpa, SteelISectionActionM90A.PureUniformCompression,
            geometryProvenance, materialProvenance);
        double eps = basic.Epsilon;
        double limit1 = alpha > .5 ? 396 * eps / (13 * alpha - 1) : 36 * eps / alpha;
        double limit2 = alpha > .5 ? 456 * eps / (13 * alpha - 1) : 41.5 * eps / alpha;
        double limit3 = ElasticClass3Limit(eps, psi);
        if (!double.IsFinite(limit1) || !double.IsFinite(limit2) ||
            !double.IsFinite(limit3) || limit1 <= 0 || limit2 < limit1 || limit3 < limit2)
            throw new NotSupportedException("Inconsistent/unsupported paired alpha/psi: EC3 class thresholds are not ordered. Stress assumptions must be reviewed.");
        int webClass = basic.Web.Slenderness <= limit1 ? 1 :
            basic.Web.Slenderness <= limit2 ? 2 : basic.Web.Slenderness <= limit3 ? 3 : 4;
        var web = basic.Web with
        {
            Class1Limit = limit1, Class2Limit = limit2, Class3Limit = limit3,
            Class = webClass
        };
        return new SteelMixedIClassificationM90B(basic, alpha, psi, basic.Flange, web,
            $"{Reference}; alpha={F(alpha)} ({parameters.PlasticStressEvidence}); " +
            $"psi={F(psi)} ({parameters.ElasticStressEvidence}); " +
            $"web c/t={F(web.Slenderness)} limits={F(limit1)}/{F(limit2)}/{F(limit3)}; " +
            $"flange class={basic.Flange.Class}; resulting class={Math.Max(basic.Flange.Class, webClass)}; " +
            "idealized DECLARED stress patterns only; NOT an N-M strength interaction, " +
            "class-4 effective section, frame stability or complete EC3/NTC check.");
    }

    public static SteelMixedWebTransitionDomainM90B ComputeStrongAxisWebTransitions(
        string sectionId, string grade, SteelISectionDimensionsM90A dimensions,
        double fyMpa, double areaCm2, double strongElasticModulusCm3,
        double strongPlasticModulusCm3, string geometryEvidence, string materialEvidence,
        string plasticModulusEvidence)
    {
        Positive(areaCm2, nameof(areaCm2));
        Positive(strongElasticModulusCm3, nameof(strongElasticModulusCm3));
        Positive(strongPlasticModulusCm3, nameof(strongPlasticModulusCm3));
        if (strongPlasticModulusCm3 < strongElasticModulusCm3 ||
            string.IsNullOrWhiteSpace(plasticModulusEvidence))
            throw new ArgumentException("Independently sourced Wpl>=Wel and provenance are mandatory.");
        var basic = SteelISectionClassifierM90A.ClassifyGeometry(sectionId, grade, dimensions,
            fyMpa, SteelISectionActionM90A.PureUniformCompression,
            geometryEvidence, materialEvidence);
        double c = basic.Web.FlatWidthMm;
        double tw = basic.Web.ThicknessMm;
        double r = basic.Web.Slenderness;
        double eps = basic.Epsilon;
        double mplKnm = strongPlasticModulusCm3 * fyMpa / 1000;
        SteelMixedBoundaryPointM90B Plastic(string name, double factor)
        {
            double alpha = (factor * eps + r) / (13 * r);
            if (!(alpha >= .5 && alpha <= 1))
                throw new NotSupportedException($"{name}: plastic neutral axis is not within the assumed compression-dominated web domain.");
            double q = 2 * alpha - 1;
            return new SteelMixedBoundaryPointM90B(name, alpha,
                q * c * tw * fyMpa / 1000,
                mplKnm - q*q*c*c*tw*fyMpa/4e6);
        }
        var oneTwo = Plastic("1/2", 396);
        var twoThree = Plastic("2/3", 456);
        double psi = (42 * eps / r - .67) / .33;
        if (!(psi > -1 && psi <= 1))
            throw new NotSupportedException("3/4 reference requires -1<psi<=1; other elastic branches require separate treatment.");
        double compressionFraction = (1 + psi) / 2;
        var threeFour = new SteelMixedBoundaryPointM90B("3/4", psi,
            compressionFraction * areaCm2 * 100 * fyMpa / 1000,
            (1 - compressionFraction) * strongElasticModulusCm3 * fyMpa / 1000);
        if (oneTwo.StrongAxisMomentKnm < 0 || twoThree.StrongAxisMomentKnm < 0 ||
            threeFour.StrongAxisMomentKnm < 0 ||
            oneTwo.AxialCompressionKn > twoThree.AxialCompressionKn ||
            twoThree.AxialCompressionKn > threeFour.AxialCompressionKn)
            throw new NotSupportedException("Transition coordinates are outside the supported ordered web-domain assumptions.");
        return new SteelMixedWebTransitionDomainM90B(oneTwo, twoThree, threeFour,
            basic.Flange.Class, r, eps,
            $"{Reference}; {sectionId}/{grade}; geometry={geometryEvidence}; " +
            $"fy={F(fyMpa)} MPa ({materialEvidence}); area={F(areaCm2)} cm2; " +
            $"Wel={F(strongElasticModulusCm3)} cm3; Wpl={F(strongPlasticModulusCm3)} cm3 ({plasticModulusEvidence}); " +
            "WEB transition points only. Classification along an N-M loading path is NOT automatic. " +
            "Class-4 effective properties and flange-governed constraints excluded.");
    }

    /// <summary>
    /// Uses signed FEM-local N (tension positive) and M at ONE station from M87-B.
    /// The elastic stress-gradient ratio is evaluated at the flat-web boundaries;
    /// class-1/2 plastic distributions are NOT inferred from a service action pair.
    /// </summary>
    public static SteelMixedElasticScreenM90B ScreenConcomitantElasticFEM(
        DesignCheckRequest request,
        CommercialRolledSteelCatalogM88B2 profiles,
        CharacteristicMaterialCatalogM88C materials, string grade)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(materials);
        var code = request.Profile?.Code;
        if (code is null || code.DocumentId != "EN 1993-1-1" ||
            code.EditionId != "2005+A1:2014" || string.IsNullOrWhiteSpace(code.NationalAnnexId))
            throw new NotSupportedException("EC3 2005+A1:2014 with declared annex identity is mandatory.");
        if (request.Member.Material != DesignMaterial.Steel ||
            request.LimitState != DesignLimitState.Ultimate)
            throw new NotSupportedException("Steel ultimate-limit-state context required.");
        var e = request.Effects ?? throw new ArgumentException("FEM action snapshot is mandatory.");
        if (e.Origin != DesignEffectOrigin.FemAnalysis ||
            !string.Equals(e.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            e.TargetKind is null || e.SectionSide is null ||
            string.IsNullOrWhiteSpace(e.AnalysisId) || string.IsNullOrWhiteSpace(e.CombinationId) ||
            !double.IsFinite(e.PositionMetres) || e.PositionMetres < 0 ||
            e.PositionMetres > request.Member.MemberLengthMetres)
            throw new ArgumentException("Complete single-member, single-target, single-side FEM provenance is required.");
        if (e.AxialForceKn is not double n || e.BendingMomentZKnm is not double m ||
            e.ShearForceYKn is not double shear ||
            !double.IsFinite(n) || !double.IsFinite(m) || !double.IsFinite(shear) ||
            n == 0 || m == 0)
            throw new NotSupportedException("One signed nonzero combined N/M state with finite concomitant shear is required.");
        var p = profiles.Find(request.Member.SectionId).Definition;
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            throw new NotSupportedException("Inclined-flange INP and non-I/H sections are excluded.");
        var dims = new SteelISectionDimensionsM90A(
            p.HeightMm, p.WidthMm, p.WebThicknessMm, p.FlangeThicknessMm, p.RootRadiusMm);
        double fy = materials.GetSteelYieldMpa(grade, Math.Max(p.WebThicknessMm,p.FlangeThicknessMm));
        var pure = SteelISectionClassifierM90A.ClassifyGeometry(
            p.Id, grade, dims, fy, SteelISectionActionM90A.PureUniformCompression,
            p.SourcePageUrl, materials.FindSteel(grade).ReferenceStandard);
        double areaMm2 = p.AreaCm2 * 100;
        double ixMm4 = p.StrongInertiaCm4 * 10000;
        Positive(areaMm2, nameof(areaMm2));
        Positive(ixMm4, nameof(ixMm4));
        double axialCompressionStress = -n * 1000 / areaMm2;
        double bendingWebStress = Math.Abs(m) * 1e6 * pure.Web.FlatWidthMm / (2 * ixMm4);
        double high = axialCompressionStress + bendingWebStress;
        double low = axialCompressionStress - bendingWebStress;
        if (!double.IsFinite(high) || !double.IsFinite(low) || high <= 0)
            throw new NotSupportedException("No compressive stress at either flat-web edge: this screening cannot determine the class.");
        double psi = low / high;
        double limit = ElasticClass3Limit(pure.Epsilon, psi);
        double cOverT = pure.Web.Slenderness;
        int upper = Math.Max(pure.Flange.Class, cOverT <= limit ? 3 : 4);
        return new SteelMixedElasticScreenM90B(
            request.Member.MemberId, p.Id, e.AnalysisId, e.CombinationId,
            e.TargetKind.Value, e.SectionSide.Value, e.PositionMetres,
            n, m, high, low, psi, limit, cOverT, upper,
            $"{Reference}; {code.EditionId}/{code.NationalAnnexId}; " +
            $"member={request.Member.MemberId} section={p.Id} x={F(e.PositionMetres)}m " +
            $"target={e.CombinationId} kind={e.TargetKind} side={e.SectionSide}; " +
            $"N={F(n)}kN(tension+) M={F(m)}kNm V={F(shear)}kN; " +
            $"web sigmaHigh={F(high)}MPa sigmaLow={F(low)}MPa " +
            $"psi={F(psi)} class3-limit={F(limit)} web-c/t={F(cOverT)}; " +
            $"indicative-source={p.SourcePageUrl}; result is only a CONSERVATIVE ELASTIC SCREEN: " +
            "classes 1/2 and plastic alpha NOT established, no full compliance/approval.");
    }

    private static double ElasticClass3Limit(double eps, double psi)
    {
        if (!double.IsFinite(psi) || psi > 1) throw new ArgumentOutOfRangeException(nameof(psi));
        double limit = psi > -1
            ? 42*eps/(.67 + .33*psi)
            : 62*eps*(1-psi)*Math.Sqrt(-psi);
        if (!double.IsFinite(limit) || limit <= 0)
            throw new NotSupportedException("Elastic web stress ratio outside finite EC3 table range.");
        return limit;
    }
    private static void Positive(double value, string label)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(label, "Finite positive number required.");
    }
    private static string F(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
}
