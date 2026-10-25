using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Independently sourced GROSS area and strong-axis second moment of area;
/// the rounded rolled-section fillets must not be reconstructed as rectangles.
/// </summary>
public sealed record Ntc2018SteelGrossElasticPropertiesD0M3(
    double GrossAreaMm2, double StrongAxisInertiaMm4, string? PropertiesEvidence);

/// <summary>
/// Optional independent physical evidence for the second NTC Table 4.2.III
/// psi&lt;=-1 footnote alternative. No strain is inferred from an elastic stress
/// beyond yield.
/// </summary>
public sealed record Ntc2018SteelTensileStrainEvidenceD0M3(
    double TensileStrain, double YoungModulusMpa, string? StrainAndModulusEvidence);

/// <summary>
/// An elastic SCREEN of a web's class-3 boundary, not a plastic class-1/2
/// classification, section resistance or whole-member code verification.
/// </summary>
public sealed record Ntc2018SteelMixedElasticWebScreenD0M3(
    DesignCodeEdition Code, string MemberId, string SectionId, string SteelGrade,
    string AnalysisId, string CombinationId, double PositionMetres,
    DesignEffectSectionSide Side, double SignedAxialForceKn,
    double SignedMomentKnm, double CompressiveEdgeStressMpa,
    double OppositeEdgeStressMpa, double Psi, double YieldStrengthMpa,
    double WebSlenderness, double Class3Limit, int ConservativeWebClassUpperBound,
    bool? FootnoteStressAlternativeSatisfied,
    bool? FootnoteStrainAlternativeSatisfied, string Trace)
{
    public bool EstablishesDefinitiveSectionClass => false;
    public bool EstablishesPlasticClassesOneOrTwo => false;
    public bool ProvidesSectionResistanceOrNmInteraction => false;
    public bool EstablishesCompleteMemberCompliance => false;
}

/// <summary>
/// NTC2018 §4.2.3.1, Table 4.2.III: derive the ELASTIC stress ratio psi at
/// the flat web edges from the same signed FEM N/M station and externally
/// sourced gross area and strong-axis inertia. No reuse of an EC3 result, no
/// automatic plastic alpha, no resistance or member compliance claim.
/// </summary>
public static class Ntc2018SteelMixedElasticWebScreenerD0M3
{
    public static DesignCodeEdition Code => Ntc2018SteelISectionClassifierD0.Code;

    public static Ntc2018SteelMixedElasticWebScreenD0M3 ScreenCatalogProfile(
        DesignCheckRequest request, CommercialRolledSteelCatalogM88B2 profiles,
        CharacteristicMaterialCatalogM88C materials, string steelGrade,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence,
        Ntc2018SteelTensileStrainEvidenceD0M3? tensileStrainEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Member);
        if (string.IsNullOrWhiteSpace(steelGrade))
            throw new ArgumentException("Documented steel grade required.", nameof(steelGrade));
        var p = profiles.Find(request.Member.SectionId).Definition;
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            throw new NotSupportedException("Only rolled parallel-flange doubly symmetric IPE/HEA/HEB; INP and welded sections unsupported.");
        double fy = materials.GetSteelYieldMpa(steelGrade,
            Math.Max(p.FlangeThicknessMm, p.WebThicknessMm));
        return ScreenGeometry(request,
            new Ntc2018SteelISectionDimensionsD0(
                p.HeightMm, p.WidthMm, p.WebThicknessMm,
                p.FlangeThicknessMm, p.RootRadiusMm),
            new Ntc2018SteelGrossElasticPropertiesD0M3(
                p.AreaCm2 * 100d, p.StrongInertiaCm4 * 10000d,
                $"OPPO {p.SourcePageUrl}; {p.DataStatus} (NOT CERTIFIED)"),
            steelGrade, fy, evidence, tensileStrainEvidence);
    }

    /// <summary>
    /// Supports independently sourced geometry/properties with documented
    /// plate/axis evidence, including numerical worked examples.
    /// </summary>
    public static Ntc2018SteelMixedElasticWebScreenD0M3 ScreenGeometry(
        DesignCheckRequest request, Ntc2018SteelISectionDimensionsD0 dimensions,
        Ntc2018SteelGrossElasticPropertiesD0M3 properties,
        string steelGrade, double yieldStrengthMpa,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence,
        Ntc2018SteelTensileStrainEvidenceD0M3? tensileStrainEvidence = null)
    {
        ValidateFem(request);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(properties);
        ArgumentNullException.ThrowIfNull(evidence);
        if (string.IsNullOrWhiteSpace(steelGrade) ||
            evidence.UnperforatedConfirmed != true ||
            evidence.HotRolledParallelFlangeDoublySymmetricConfirmed != true ||
            evidence.StrongAxisMappedToFemMomentConfirmed != true ||
            string.IsNullOrWhiteSpace(evidence.GeometryEvidence) ||
            string.IsNullOrWhiteSpace(evidence.GradeAndYieldEvidence) ||
            string.IsNullOrWhiteSpace(properties.PropertiesEvidence))
            throw new NotSupportedException("Complete independent rolled I/H geometry, material, strong-axis and A/I evidence required.");

        Positive(dimensions.HeightMm, nameof(dimensions.HeightMm));
        Positive(dimensions.WidthMm, nameof(dimensions.WidthMm));
        Positive(dimensions.WebThicknessMm, nameof(dimensions.WebThicknessMm));
        Positive(dimensions.FlangeThicknessMm, nameof(dimensions.FlangeThicknessMm));
        Positive(dimensions.RootRadiusMm, nameof(dimensions.RootRadiusMm));
        Positive(properties.GrossAreaMm2, nameof(properties.GrossAreaMm2));
        Positive(properties.StrongAxisInertiaMm4, nameof(properties.StrongAxisInertiaMm4));
        Positive(yieldStrengthMpa, nameof(yieldStrengthMpa));
        double webFlat = dimensions.HeightMm -
            2 * (dimensions.FlangeThicknessMm + dimensions.RootRadiusMm);
        double flangeFlat = (dimensions.WidthMm - dimensions.WebThicknessMm -
            2 * dimensions.RootRadiusMm) / 2;
        double maxInertia = properties.GrossAreaMm2 *
            Math.Pow(dimensions.HeightMm / 2, 2);
        if (!double.IsFinite(webFlat) || webFlat <= 0 ||
            !double.IsFinite(flangeFlat) || flangeFlat <= 0 ||
            !double.IsFinite(maxInertia) ||
            properties.GrossAreaMm2 >= dimensions.HeightMm * dimensions.WidthMm ||
            properties.StrongAxisInertiaMm4 >= maxInertia)
            throw new ArgumentException("Flat plate geometry or independently sourced gross A/I violates physical bounds.");

        var e = request.Effects;
        double n = e.AxialForceKn!.Value;
        double m = e.BendingMomentZKnm!.Value;
        double c = -n * 1000d / properties.GrossAreaMm2;
        double b = Math.Abs(m) * 1e6 * (webFlat / 2) /
            properties.StrongAxisInertiaMm4;
        double high = c + b;
        double low = c - b;
        if (!double.IsFinite(c) || !double.IsFinite(b) ||
            !double.IsFinite(high) || !double.IsFinite(low) || b <= 0 || high <= 0)
            throw new NotSupportedException("No finite compressed flat-web edge: NTC mixed elastic web screen unsupported.");
        double psi = low / high;
        if (!double.IsFinite(psi) || !(psi < 1) || psi == -1 ||
            (psi > -1 && n >= 0) || (psi < -1 && n <= 0))
            throw new NotSupportedException("Unresolved psi branch: signed N and same-section elastic edge stresses disagree or pure bending limit reached.");

        bool? stressAlternative = null;
        bool? strainAlternative = null;
        string footnoteTrace = "not applicable (psi>-1)";
        if (psi < -1)
        {
            stressAlternative = high <= yieldStrengthMpa;
            if (tensileStrainEvidence is not null)
            {
                Positive(tensileStrainEvidence.YoungModulusMpa,
                    nameof(tensileStrainEvidence.YoungModulusMpa));
                if (!double.IsFinite(tensileStrainEvidence.TensileStrain) ||
                    tensileStrainEvidence.TensileStrain < 0 ||
                    string.IsNullOrWhiteSpace(tensileStrainEvidence.StrainAndModulusEvidence))
                    throw new NotSupportedException("Independent tensile strain and E evidence are incomplete.");
                strainAlternative = tensileStrainEvidence.TensileStrain >
                    yieldStrengthMpa / tensileStrainEvidence.YoungModulusMpa;
            }
            if (stressAlternative != true && strainAlternative != true)
                throw new NotSupportedException("NTC 2018 Tab.4.2.III footnote fails: sigmaC>fyk and no proved tensile strain>fyk/E.");
            footnoteTrace = $"sigmaC<=fyk={stressAlternative}; tensile-strain>fyk/E={strainAlternative?.ToString() ?? "NOT EVALUATED"}; " +
                $"strainEvidence={tensileStrainEvidence?.StrainAndModulusEvidence ?? "none"}";
        }

        double epsilon = Math.Sqrt(235d / yieldStrengthMpa);
        double class3Limit = psi > -1
            ? 42 * epsilon / (0.67 + 0.33 * psi)
            : 62 * epsilon * (1 - psi) * Math.Sqrt(-psi);
        double slenderness = webFlat / dimensions.WebThicknessMm;
        if (!double.IsFinite(class3Limit) || class3Limit <= 0 ||
            !double.IsFinite(slenderness) || slenderness <= 0)
            throw new NotSupportedException("Nonfinite NTC web class-3 boundary or slenderness.");
        int upper = slenderness <= class3Limit ? 3 : 4;
        string trace = $"NTC2018 §4.2.3.1 Tab.4.2.III WEB ELASTIC SCREEN ONLY | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId}; " +
            $"member={request.Member.MemberId} section={request.Member.SectionId} grade={steelGrade} " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(e.PositionMetres)} side={e.SectionSide}; " +
            $"N={F(n)}kN(tension+) M={F(m)}kNm V={F(e.ShearForceYKn!.Value)}kN; " +
            $"grossA={F(properties.GrossAreaMm2)}mm2 Iy={F(properties.StrongAxisInertiaMm4)}mm4; " +
            $"C={F(c)}MPa B={F(b)}MPa sigmaC={F(high)}MPa sigmaOther={F(low)}MPa " +
            $"psi={F(psi)} branch={(psi > -1 ? "psi>-1" : "psi<-1")}; " +
            $"fyk={F(yieldStrengthMpa)}MPa footnote={footnoteTrace}; " +
            $"web(c/t)={F(slenderness)} class3limit={F(class3Limit)} classUpperBound={upper}; " +
            $"source={properties.PropertiesEvidence}; geometry={evidence.GeometryEvidence}; material={evidence.GradeAndYieldEvidence}; " +
            "NOT plastic alpha/class1/class2, entire section class, resistance, class4 effective section, stability or member compliance.";
        return new Ntc2018SteelMixedElasticWebScreenD0M3(
            Code, request.Member.MemberId, request.Member.SectionId, steelGrade,
            e.AnalysisId, e.CombinationId, e.PositionMetres, e.SectionSide!.Value,
            n, m, high, low, psi, yieldStrengthMpa, slenderness, class3Limit,
            upper, stressAlternative, strainAlternative, trace);
    }

    private static void ValidateFem(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Profile?.Code != Code)
            throw new NotSupportedException("Native NTC2018 edition identity required: EC3 is a different checker.");
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
            throw new ArgumentException("Exact same FEM member, load combination, station and section side required.");
        if (e.AxialForceKn is not double n || e.BendingMomentZKnm is not double m ||
            e.ShearForceYKn is not double v || !double.IsFinite(n) ||
            !double.IsFinite(m) || !double.IsFinite(v) || n == 0 || m == 0 || v != 0)
            throw new NotSupportedException("Only concomitant signed nonzero N/M strong-axis with V=0 can be screened.");
    }

    private static void Positive(double x, string name)
    {
        if (!double.IsFinite(x) || x <= 0)
            throw new ArgumentOutOfRangeException(name, x, "Finite positive value required.");
    }

    private static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
}
