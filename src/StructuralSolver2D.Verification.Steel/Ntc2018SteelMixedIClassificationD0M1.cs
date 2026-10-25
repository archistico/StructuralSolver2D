using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// Independently established yield-plastic and elastic web stress parameters.
/// Alpha is the compressed PLASTIC depth fraction; psi is the ratio of ELASTIC
/// longitudinal edge stresses. They are not interchangeable and are never
/// inferred from unqualified 2D FEM stresses.
/// </summary>
public sealed record Ntc2018SteelMixedStressParametersD0M1(
    double PlasticCompressedFractionAlpha,
    double ElasticWebEdgeStressRatioPsi,
    string? PlasticStressFieldEvidence,
    string? ElasticStressFieldEvidence,
    bool? ConsistentWithSameFemActionConfirmed,
    string? SameActionAndStationEvidence)
{
    /// <summary>
    /// Only for the NTC Tab. 4.2.III class-3 psi <= -1 branch. A missing
    /// record keeps that branch unsupported. Existing D0M1 callers are unchanged.
    /// </summary>
    public Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2? PsiBelowMinusOneEvidence { get; init; }

    /// <summary>
    /// New D0M5 alternative: NTC Table 4.2.III permits psi&lt;=-1 when the
    /// compressed edge stress does not exceed fyk; no strain/E evidence is
    /// needed to establish THIS first alternative. Does not weaken the old
    /// D0M2 tensile-strain evidence path.
    /// </summary>
    public Ntc2018SteelMixedStressOnlyFootnoteEvidenceD0M5? PsiBelowMinusOneStressOnlyEvidence { get; init; }
}

/// <summary>
/// Measured/justified inputs to the special psi <= -1 footnote. Compression
/// and opposite-edge elastic tensile stresses are positive MAGNITUDES in MPa;
/// their negative ratio must reproduce the declared psi. Tensile strain is
/// dimensionless; E is in MPa and externally sourced, never assumed from FEM.
/// This record does not establish physical compatibility with N/M by itself.
/// </summary>
public sealed record Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2(
    double CompressionStressMagnitudeMpa,
    double OppositeElasticTensileStressMagnitudeMpa,
    double TensileStrain,
    double YoungModulusMpa,
    string? FootnoteStressStrainEvidence);

/// <summary>Independent evidence for a rolled unperforated I/H section.</summary>
public sealed record Ntc2018SteelMixedClassificationEvidenceD0M1(
    bool? UnperforatedConfirmed,
    bool? HotRolledParallelFlangeDoublySymmetricConfirmed,
    bool? StrongAxisMappedToFemMomentConfirmed,
    string? GeometryEvidence,
    string? GradeAndYieldEvidence);

/// <summary>Class diagnosis at one FEM section; cannot be promoted to resistance/compliance.</summary>
public sealed record Ntc2018SteelMixedIClassificationD0M1(
    DesignCodeEdition Code,
    string MemberId, string SectionId, string SteelGrade,
    string AnalysisId, string CombinationId,
    double PositionMetres, DesignEffectSectionSide Side,
    double SignedCompressionKn, double SignedMomentKnm,
    double Alpha, double Psi, double YieldStrengthMpa, double Epsilon,
    Ntc2018SteelPlateClassificationD0 Flange,
    Ntc2018SteelPlateClassificationD0 Web,
    string Trace)
{
    public int Class => Math.Max(Flange.Class, Web.Class);
    public string GoverningParts => Flange.Class == Web.Class ? "FlangeAndWeb" :
        Flange.Class > Web.Class ? "Flange" : "Web";
    public bool RequiresEffectiveSectionProperties => Class == 4;
    public bool EstablishesCompleteMemberCompliance => false;
    public bool ProvidesResistanceOrNmInteractionCheck => false;
    public string Class3Branch => Psi <= -1 ? "PsiBelowOrEqualMinusOneD0M2" : "PsiAboveMinusOneD0M1";
    // Keep the original positional property for D0M1 source compatibility;
    // D0M2 also accepts positive FEM axial tension, hence the neutral alias.
    public double SignedAxialForceKn => SignedCompressionKn;
}

/// <summary>
/// Native NTC 2018 §4.2.3.1 Table 4.2.III mixed strong-axis N/M web classes;
/// Table 4.2.IV rolled compressed flange limits. Supports both alpha branches;
/// for psi < -1 the extra NTC footnote is mandatory (D0M2 extension, FEM N>0
/// tension with compression on one edge). Psi=-1 is the pure bending limit,
/// handled by D0. Strong-axis M!=0 at ONE FEM station with V=0, N<0 in D0M1
/// or N>0 in D0M2. Never a substitute for EC3 M90-B or a strength check.
/// </summary>
public static class Ntc2018SteelMixedIClassifierD0M1
{
    public const string Clause = "NTC 2018, 4.2.3.1, Tab. 4.2.III/IV, campo N+M (alpha, psi; footnote psi<=-1)";
    public static DesignCodeEdition Code => Ntc2018SteelISectionClassifierD0.Code;

    public static Ntc2018SteelMixedIClassificationD0M1 ClassifyCatalogProfile(
        DesignCheckRequest request,
        CommercialRolledSteelCatalogM88B2 catalog,
        CharacteristicMaterialCatalogM88C materials,
        string steelGrade,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence,
        Ntc2018SteelMixedStressParametersD0M1 stress)
    {
        ValidateFem(request);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(materials);
        if (string.IsNullOrWhiteSpace(steelGrade)) throw new ArgumentException("Steel grade required.");
        var p = catalog.Find(request.Member.SectionId).Definition;
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            throw new NotSupportedException("D0M1 excludes tapered INP, welded and other non-parallel-flange profiles.");
        double thickness = Math.Max(p.FlangeThicknessMm, p.WebThicknessMm);
        double fy = materials.GetSteelYieldMpa(steelGrade, thickness);
        return ClassifyGeometry(request,
            new Ntc2018SteelISectionDimensionsD0(
                p.HeightMm, p.WidthMm, p.WebThicknessMm, p.FlangeThicknessMm, p.RootRadiusMm),
            steelGrade, fy, evidence, stress,
            $"{evidence?.GeometryEvidence}; OPPO={p.SourcePageUrl}; {p.DataStatus} (NOT CERTIFIED)",
            $"{evidence?.GradeAndYieldEvidence}; {materials.FindSteel(steelGrade).ReferenceStandard}; thickness={F(thickness)} mm");
    }

    /// <summary>Geometry-only worked example with signed and traceable FEM N/M snapshot.</summary>
    public static Ntc2018SteelMixedIClassificationD0M1 ClassifyGeometry(
        DesignCheckRequest request,
        Ntc2018SteelISectionDimensionsD0 dimensions,
        string steelGrade, double fyMpa,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence,
        Ntc2018SteelMixedStressParametersD0M1 stress,
        string? geometrySource = null, string? strengthSource = null)
    {
        ValidateFem(request);
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(stress);
        if (string.IsNullOrWhiteSpace(steelGrade) ||
            evidence.UnperforatedConfirmed != true ||
            evidence.HotRolledParallelFlangeDoublySymmetricConfirmed != true ||
            evidence.StrongAxisMappedToFemMomentConfirmed != true ||
            string.IsNullOrWhiteSpace(evidence.GeometryEvidence) ||
            string.IsNullOrWhiteSpace(evidence.GradeAndYieldEvidence) ||
            string.IsNullOrWhiteSpace(stress.PlasticStressFieldEvidence) ||
            string.IsNullOrWhiteSpace(stress.ElasticStressFieldEvidence) ||
            stress.ConsistentWithSameFemActionConfirmed != true ||
            string.IsNullOrWhiteSpace(stress.SameActionAndStationEvidence))
            throw new NotSupportedException("Missing independent hole/geometry/material/axis/plastic+elastic stress-field evidence for the same N/M action.");

        Positive(fyMpa, nameof(fyMpa));
        Positive(dimensions.HeightMm, nameof(dimensions.HeightMm));
        Positive(dimensions.WidthMm, nameof(dimensions.WidthMm));
        Positive(dimensions.WebThicknessMm, nameof(dimensions.WebThicknessMm));
        Positive(dimensions.FlangeThicknessMm, nameof(dimensions.FlangeThicknessMm));
        Positive(dimensions.RootRadiusMm, nameof(dimensions.RootRadiusMm));

        double cFlange = (dimensions.WidthMm - dimensions.WebThicknessMm - 2 * dimensions.RootRadiusMm) / 2d;
        double cWeb = dimensions.HeightMm - 2 * dimensions.FlangeThicknessMm - 2 * dimensions.RootRadiusMm;
        if (!double.IsFinite(cFlange) || !double.IsFinite(cWeb) || cFlange <= 0 || cWeb <= 0)
            throw new ArgumentException("Unsupported nonpositive flat plate widths.");
        double alpha = stress.PlasticCompressedFractionAlpha;
        double psi = stress.ElasticWebEdgeStressRatioPsi;
        if (!double.IsFinite(alpha) || !(alpha > 0 && alpha < 1) ||
            !double.IsFinite(psi) || !(psi < 1))
            throw new NotSupportedException("D0M1/M2 requires 0<alpha<1 and psi<1; invalid/pure branches not implemented.");
        double signedN = request.Effects.AxialForceKn!.Value;
        if (psi > -1)
        {
            if (signedN >= 0)
                throw new NotSupportedException("D0M1 psi>-1 mixed-compression branch requires FEM N<0.");
        }
        else if (signedN <= 0 || psi == -1 || alpha >= 0.5)
        {
            throw new NotSupportedException("D0M2 psi<-1 requires tensile FEM N>0 and plastic compression fraction alpha<0.5; psi=-1 is pure bending D0.");
        }

        double epsilon = Math.Sqrt(235d / fyMpa);
        double fRatio = cFlange / dimensions.FlangeThicknessMm;
        double wRatio = cWeb / dimensions.WebThicknessMm;
        double l1 = alpha > 0.5 ? 396 * epsilon / (13 * alpha - 1) : 36 * epsilon / alpha;
        double l2 = alpha > 0.5 ? 456 * epsilon / (13 * alpha - 1) : 41.5 * epsilon / alpha;
        double l3;
        string footnoteTrace;
        if (psi > -1)
        {
            l3 = 42 * epsilon / (0.67 + 0.33 * psi);
            footnoteTrace = "class3=psi>-1";
        }
        else
        {
            var extra = stress.PsiBelowMinusOneEvidence;
            var stressOnly = stress.PsiBelowMinusOneStressOnlyEvidence;
            if (extra is not null && stressOnly is not null)
                throw new NotSupportedException("Ambiguous NTC footnote evidence: choose one independently documented path.");

            if (extra is null && stressOnly is not null)
            {
                // Exactly the first, stress-based OR alternative in the NTC
                // footnote. Do NOT fabricate an E or a tensile strain.
                if (string.IsNullOrWhiteSpace(stressOnly.ElasticEdgeStressEvidence) ||
                    !double.IsFinite(stressOnly.CompressionStressMagnitudeMpa) ||
                    stressOnly.CompressionStressMagnitudeMpa <= 0 ||
                    !double.IsFinite(stressOnly.OppositeElasticTensileStressMagnitudeMpa) ||
                    stressOnly.OppositeElasticTensileStressMagnitudeMpa <= 0)
                    throw new NotSupportedException("Complete, finite opposite elastic edge stresses required for the stress-only footnote alternative.");
                double psiFromEdges = -stressOnly.OppositeElasticTensileStressMagnitudeMpa /
                    stressOnly.CompressionStressMagnitudeMpa;
                if (!double.IsFinite(psiFromEdges) ||
                    Math.Abs(psiFromEdges - psi) > 1e-10 * Math.Max(1d, Math.Abs(psi)))
                    throw new NotSupportedException("Stress-only edge ratio does not agree with declared psi.");
                if (stressOnly.CompressionStressMagnitudeMpa > fyMpa)
                    throw new NotSupportedException("Stress-only NTC footnote cannot pass sigmaC>fyk without independent tensile-strain/E evidence.");
                l3 = 62 * epsilon * (1 - psi) * Math.Sqrt(-psi);
                footnoteTrace = $"class3=psi<=-1; psiFromEdges={F(psiFromEdges)}; " +
                    $"sigmaC={F(stressOnly.CompressionStressMagnitudeMpa)}MPa<=fy=True; " +
                    $"sigmaTElastic={F(stressOnly.OppositeElasticTensileStressMagnitudeMpa)}MPa; " +
                    $"tensile-strain/E=NOT REQUIRED BY FIRST FOOTNOTE ALTERNATIVE; " +
                    $"source={stressOnly.ElasticEdgeStressEvidence}";
            }
            else
            {
                if (extra is null ||
                    string.IsNullOrWhiteSpace(extra.FootnoteStressStrainEvidence) ||
                    !double.IsFinite(extra.CompressionStressMagnitudeMpa) ||
                    extra.CompressionStressMagnitudeMpa <= 0 ||
                    !double.IsFinite(extra.OppositeElasticTensileStressMagnitudeMpa) ||
                    extra.OppositeElasticTensileStressMagnitudeMpa <= 0 ||
                    !double.IsFinite(extra.TensileStrain) || extra.TensileStrain < 0 ||
                    !double.IsFinite(extra.YoungModulusMpa) || extra.YoungModulusMpa <= 0)
                    throw new NotSupportedException("NTC Table 4.2.III psi<=-1 requires documented opposite elastic edge stresses and a supported footnote alternative.");

                double psiFromEdges = -extra.OppositeElasticTensileStressMagnitudeMpa /
                    extra.CompressionStressMagnitudeMpa;
                if (!double.IsFinite(psiFromEdges) ||
                    Math.Abs(psiFromEdges - psi) > 1e-10 * Math.Max(1d, Math.Abs(psi)))
                    throw new NotSupportedException("Declared psi disagrees with documented same-section elastic web edge stress ratio.");

                double tensileYieldStrain = fyMpa / extra.YoungModulusMpa;
                bool stressCondition = extra.CompressionStressMagnitudeMpa <= fyMpa;
                bool strainCondition = extra.TensileStrain > tensileYieldStrain;
                if (!stressCondition && !strainCondition)
                    throw new NotSupportedException("NTC psi<=-1 footnote does not hold: neither sigma_compression<=fyk nor tensile_strain>fyk/E.");

                l3 = 62 * epsilon * (1 - psi) * Math.Sqrt(-psi);
                footnoteTrace = $"class3=psi<=-1; psiFromEdges={F(psiFromEdges)}; " +
                    $"sigmaC={F(extra.CompressionStressMagnitudeMpa)}MPa<=fy={stressCondition}; " +
                    $"sigmaTElastic={F(extra.OppositeElasticTensileStressMagnitudeMpa)}MPa; " +
                    $"epsilonT={F(extra.TensileStrain)}>fy/E={F(tensileYieldStrain)}={strainCondition}; " +
                    $"E={F(extra.YoungModulusMpa)}MPa; source={extra.FootnoteStressStrainEvidence}";
            }
        }
        if (!double.IsFinite(l1) || !double.IsFinite(l2) || !double.IsFinite(l3) ||
            !(l1 > 0 && l2 > l1 && l3 >= l2))
            throw new NotSupportedException("Plastic alpha and elastic psi produce non-ordered NTC classes: declared stress fields require review.");
        var flange = Plate("OutstandFlange", cFlange, dimensions.FlangeThicknessMm, fRatio,
            9 * epsilon, 10 * epsilon, 14 * epsilon);
        var web = Plate("InternalWeb", cWeb, dimensions.WebThicknessMm, wRatio, l1, l2, l3);
        var e = request.Effects;
        string trace = $"SINGLE-SECTION CLASSIFICATION ONLY | {Clause} | " +
            $"member={request.Member.MemberId} section={request.Member.SectionId} grade={steelGrade}; " +
            $"NTC2018={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId}; " +
            $"FEM={e.AnalysisId}/{e.CombinationId} x={F(e.PositionMetres)}m side={e.SectionSide}; " +
            $"N={F(e.AxialForceKn!.Value)}kN (compression negative, tension positive), M={F(e.BendingMomentZKnm!.Value)}kNm " +
            $"V={F(e.ShearForceYKn!.Value)}kN; fy={F(fyMpa)}MPa epsilon={F(epsilon)}; " +
            $"alpha={F(alpha)} ({stress.PlasticStressFieldEvidence}), psi={F(psi)} ({stress.ElasticStressFieldEvidence}); " +
            $"{footnoteTrace}; " +
            $"sameStation={stress.SameActionAndStationEvidence}; " +
            $"web c/t={F(wRatio)} limits={F(l1)}/{F(l2)}/{F(l3)} class={web.Class}; " +
            $"flange c/t={F(fRatio)} limits={F(flange.Class1Limit)}/{F(flange.Class2Limit)}/{F(flange.Class3Limit)} class={flange.Class}; " +
            $"overall={Math.Max(flange.Class, web.Class)}; geometry={geometrySource ?? evidence.GeometryEvidence}; " +
            $"material={strengthSource ?? evidence.GradeAndYieldEvidence}; " +
            "NOT resistance or N/M interaction, class4 Aeff, stability, connections, other stations or full code compliance.";
        return new Ntc2018SteelMixedIClassificationD0M1(Code,
            request.Member.MemberId, request.Member.SectionId, steelGrade,
            e.AnalysisId, e.CombinationId, e.PositionMetres, e.SectionSide!.Value,
            e.AxialForceKn!.Value, e.BendingMomentZKnm!.Value, alpha, psi, fyMpa,
            epsilon, flange, web, trace);
    }

    private static Ntc2018SteelPlateClassificationD0 Plate(string part, double c,
        double t, double ratio, double l1, double l2, double l3) =>
        new(part, c, t, ratio, l1, l2, l3,
            ratio <= l1 ? 1 : ratio <= l2 ? 2 : ratio <= l3 ? 3 : 4);

    private static void ValidateFem(DesignCheckRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Profile?.Code != Code)
            throw new NotSupportedException("Native NTC2018 edition identity required; no EC3 substitution.");
        if (request.Member is null || request.Member.Material != DesignMaterial.Steel ||
            request.LimitState != DesignLimitState.Ultimate ||
            string.IsNullOrWhiteSpace(request.Member.MemberId) ||
            string.IsNullOrWhiteSpace(request.Member.SectionId) ||
            !double.IsFinite(request.Member.MemberLengthMetres) || request.Member.MemberLengthMetres <= 0)
            throw new ArgumentException("Steel SLU member and positive member length required.");
        var e = request.Effects;
        if (e is null || e.Origin != DesignEffectOrigin.FemAnalysis ||
            !string.Equals(e.MemberId, request.Member.MemberId, StringComparison.OrdinalIgnoreCase) ||
            e.TargetKind != DesignEffectTargetKind.LoadCombination || e.SectionSide is null ||
            string.IsNullOrWhiteSpace(e.AnalysisId) || string.IsNullOrWhiteSpace(e.CombinationId) ||
            !double.IsFinite(e.PositionMetres) || e.PositionMetres < 0 ||
            e.PositionMetres > request.Member.MemberLengthMetres)
            throw new ArgumentException("Exact same-member, same-combination, same-side FEM station required.");
        if (e.AxialForceKn is not double n || e.BendingMomentZKnm is not double m ||
            e.ShearForceYKn is not double v || !double.IsFinite(n) || !double.IsFinite(m) ||
            !double.IsFinite(v) || n == 0 || m == 0 || v != 0)
            throw new NotSupportedException("Only concomitant nonzero signed FEM N and strong-axis M with V=0 are supported; D0M1 compression or D0M2 tension branch is checked separately.");
    }

    private static void Positive(double x, string name)
    {
        if (!double.IsFinite(x) || x <= 0)
            throw new ArgumentOutOfRangeException(name, x, "Finite positive number required.");
    }

    private static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
}
