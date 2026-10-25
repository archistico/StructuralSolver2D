using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>
/// NTC 2018 Table 4.2.III, footnote's FIRST (sigmaC<=fyk) alternative.
/// This is not tensile-strain evidence; the other alternative still requires
/// independently documented strain and E. The stress-only path may not be
/// used when sigmaC exceeds fyk.
/// </summary>
public sealed record Ntc2018SteelMixedStressOnlyFootnoteEvidenceD0M5(
    double CompressionStressMagnitudeMpa,
    double OppositeElasticTensileStressMagnitudeMpa,
    string? ElasticEdgeStressEvidence);

/// <summary>
/// Classification of ONE documented FEM station and ONE modeled rolled I/H
/// section. A class is not a resistance or member-compliance judgment.
/// </summary>
public sealed record Ntc2018SteelMixedAutomaticSectionD0M5(
    Ntc2018SteelMixedPlasticWebAlphaD0M4 Plastic,
    Ntc2018SteelMixedElasticWebScreenD0M3 Elastic,
    Ntc2018SteelMixedIClassificationD0M1 Classification,
    string Trace)
{
    public int ClassWithinDeclaredScope => Classification.Class;
    public bool RequiresEffectiveSectionProperties => Classification.RequiresEffectiveSectionProperties;
    public bool EstablishesSectionResistance => false;
    public bool EstablishesNmOrShearInteraction => false;
    public bool EstablishesCompleteMemberCompliance => false;
    public bool EstablishesWholeStructureCompliance => false;
}

/// <summary>
/// P0-D0M5 orchestration, NOT a new formula. Enforce a single native-NTC2018
/// N/M/V FEM station, source-checked gross A/I and rolled geometry, then
/// compute plastic alpha (D0M4) and elastic psi (D0M3) separately and pass
/// their proven parameters into the EXISTING D0M1/M2 NTC classifier.
/// The plastic NA must remain within the flat web, V=0, and all sources and
/// edition identity must be complete. No EC3 substitution or member result.
/// </summary>
public static class Ntc2018SteelMixedAutomaticSectionClassifierD0M5
{
    public static DesignCodeEdition Code => Ntc2018SteelISectionClassifierD0.Code;

    public static Ntc2018SteelMixedAutomaticSectionD0M5 ClassifyCatalogProfile(
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
            throw new NotSupportedException("Only doubly symmetric parallel-flange hot-rolled IPE/HEA/HEB profiles are supported.");
        double fy = materials.GetSteelYieldMpa(steelGrade,
            Math.Max(profile.FlangeThicknessMm, profile.WebThicknessMm));
        return ClassifyGeometry(request,
            new Ntc2018SteelISectionDimensionsD0(profile.HeightMm,
                profile.WidthMm, profile.WebThicknessMm,
                profile.FlangeThicknessMm, profile.RootRadiusMm),
            new Ntc2018SteelGrossElasticPropertiesD0M3(profile.AreaCm2 * 100d,
                profile.StrongInertiaCm4 * 10000d,
                $"OPPO {profile.SourcePageUrl}; {profile.DataStatus} (NOT CERTIFIED)"),
            steelGrade, fy, evidence, tensileStrainEvidence);
    }

    public static Ntc2018SteelMixedAutomaticSectionD0M5 ClassifyGeometry(
        DesignCheckRequest request, Ntc2018SteelISectionDimensionsD0 dimensions,
        Ntc2018SteelGrossElasticPropertiesD0M3 grossProperties,
        string steelGrade, double yieldStrengthMpa,
        Ntc2018SteelMixedClassificationEvidenceD0M1 evidence,
        Ntc2018SteelTensileStrainEvidenceD0M3? tensileStrainEvidence = null)
    {
        ArgumentNullException.ThrowIfNull(grossProperties);
        var plastic = Ntc2018SteelMixedPlasticWebAlphaCalculatorD0M4.CalculateGeometry(
            request, dimensions,
            new Ntc2018SteelGrossPlasticAreaD0M4(
                grossProperties.GrossAreaMm2, grossProperties.PropertiesEvidence),
            steelGrade, yieldStrengthMpa, evidence);
        var elastic = Ntc2018SteelMixedElasticWebScreenerD0M3.ScreenGeometry(
            request, dimensions, grossProperties, steelGrade, yieldStrengthMpa,
            evidence, tensileStrainEvidence);

        // Comparing every identity/value used by the composed services avoids
        // silently combining extrema or effects from different FEM stations.
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
            !double.IsFinite(plastic.FlatWebDepthMm / plastic.WebThicknessMm) ||
            Math.Abs(plastic.FlatWebDepthMm / plastic.WebThicknessMm -
                elastic.WebSlenderness) >
                1e-12 * Math.Max(1d, elastic.WebSlenderness))
            throw new NotSupportedException("Plastic and elastic classifiers do not share the same native-NTC FEM section/material/geometry.");

        Ntc2018SteelMixedPsiBelowMinusOneEvidenceD0M2? fullFootnote = null;
        Ntc2018SteelMixedStressOnlyFootnoteEvidenceD0M5? stressOnlyFootnote = null;
        if (elastic.Psi < -1)
        {
            double tensileMagnitude = -elastic.OppositeEdgeStressMpa;
            if (elastic.FootnoteStressAlternativeSatisfied == true)
            {
                stressOnlyFootnote = new(
                    elastic.CompressiveEdgeStressMpa, tensileMagnitude,
                    "D0M3 independently sourced A/I; same FEM N/M; " +
                    "NTC Table 4.2.III stress-only OR alternative; " +
                    grossProperties.PropertiesEvidence);
            }
            else
            {
                if (elastic.FootnoteStrainAlternativeSatisfied != true ||
                    tensileStrainEvidence is null)
                    throw new NotSupportedException("NTC psi<-1 without stress alternative requires independently proven tensile strain and E.");
                fullFootnote = new(
                    elastic.CompressiveEdgeStressMpa, tensileMagnitude,
                    tensileStrainEvidence.TensileStrain,
                    tensileStrainEvidence.YoungModulusMpa,
                    $"D0M3 same FEM elastic stress + external E/strain: " +
                    tensileStrainEvidence.StrainAndModulusEvidence);
            }
        }

        var stress = new Ntc2018SteelMixedStressParametersD0M1(
            plastic.PlasticCompressedFractionAlpha,
            elastic.Psi,
            "D0M4 independently derived yielding-block axial force balance with PNA inside flat web: " +
                plastic.Trace,
            "D0M3 independently derived linear elastic web stresses from FEM N/M and sourced gross A/I: " +
                elastic.Trace,
            ConsistentWithSameFemActionConfirmed: true,
            SameActionAndStationEvidence:
                $"Exact equality of code/member/section/grade/FEM/station/side/N/M/fy and geometric web slenderness; " +
                $"FEM={plastic.AnalysisId}/{plastic.CombinationId} x={F(plastic.PositionMetres)} side={plastic.Side}")
        {
            PsiBelowMinusOneEvidence = fullFootnote,
            PsiBelowMinusOneStressOnlyEvidence = stressOnlyFootnote
        };
        var classified = Ntc2018SteelMixedIClassifierD0M1.ClassifyGeometry(
            request, dimensions, steelGrade, yieldStrengthMpa, evidence,
            stress, grossProperties.PropertiesEvidence,
            evidence.GradeAndYieldEvidence);
        if (classified.Code != Code ||
            classified.MemberId != plastic.MemberId ||
            classified.SectionId != plastic.SectionId ||
            classified.AnalysisId != plastic.AnalysisId ||
            classified.CombinationId != plastic.CombinationId ||
            classified.PositionMetres != plastic.PositionMetres ||
            classified.Side != plastic.Side ||
            classified.SignedAxialForceKn != plastic.SignedAxialForceKn ||
            classified.SignedMomentKnm != plastic.SignedStrongAxisMomentKnm ||
            Math.Abs(classified.Alpha - plastic.PlasticCompressedFractionAlpha) > 1e-12 ||
            Math.Abs(classified.Psi - elastic.Psi) > 1e-12 ||
            (classified.Web.Class == 4) !=
                (elastic.ConservativeWebClassUpperBound == 4))
            throw new NotSupportedException("Composed NTC mixed-classification consistency audit failed.");

        string trace = $"NATIVE NTC2018 SINGLE-FEM-STATION I/H SECTION CLASSIFICATION WITHIN DECLARED SCOPE; " +
            $"member={plastic.MemberId} section={plastic.SectionId} FEM={plastic.AnalysisId}/{plastic.CombinationId} " +
            $"x={F(plastic.PositionMetres)} side={plastic.Side}; " +
            $"alpha={F(plastic.PlasticCompressedFractionAlpha)} [D0M4] " +
            $"psi={F(elastic.Psi)} [D0M3]; class={classified.Class} " +
            $"web={classified.Web.Class} flange={classified.Flange.Class}; " +
            $"requiresAeffWeff={classified.RequiresEffectiveSectionProperties}; " +
            $"footnote={(elastic.Psi > -1 ? "not applicable" : stressOnlyFootnote is not null ? "stress only" : "external strain")}; " +
            "NO resistance/NM/shear interaction, member stability, effective Class4 section, " +
            "all-station/combination coverage or structural compliance. " +
            "OPPO catalog entries where used are indicative, not product certificates.";
        return new Ntc2018SteelMixedAutomaticSectionD0M5(
            plastic, elastic, classified, trace);
    }

    private static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
}
