using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>Only the two pure-stress patterns explicitly read from NTC 2018 Tables 4.2.III-IV.</summary>
public enum Ntc2018SteelPureActionD0 { UniformCompression = 1, StrongAxisBending = 2 }

/// <summary>Rolled, doubly symmetric, parallel-flange I/H geometry, in millimetres.</summary>
public sealed record Ntc2018SteelISectionDimensionsD0(
    double HeightMm, double WidthMm, double WebThicknessMm,
    double FlangeThicknessMm, double RootRadiusMm);

/// <summary>No FEM observation can prove these geometric, loading or material assertions.</summary>
public sealed record Ntc2018SteelClassificationEvidenceD0(
    bool? UnperforatedSectionConfirmed,
    bool? HotRolledParallelFlangeAndDoublySymmetricConfirmed,
    bool? StrongAxisOfBendingConfirmed,
    string? SectionGeometryEvidence,
    string? MaterialYieldEvidence,
    string? PureActionEvidence);

public sealed record Ntc2018SteelPlateClassificationD0(
    string Part, double FlatWidthMm, double ThicknessMm, double WidthThicknessRatio,
    double Class1Limit, double Class2Limit, double Class3Limit, int Class);

/// <summary>A sectional plate-class diagnosis, never member resistance or an approval.</summary>
public sealed record Ntc2018SteelISectionClassificationD0(
    DesignCodeEdition Code, string SectionId, string SteelGrade,
    Ntc2018SteelPureActionD0 Action, double YieldStrengthMpa, double Epsilon,
    Ntc2018SteelPlateClassificationD0 Flange,
    Ntc2018SteelPlateClassificationD0 Web,
    string GeometrySource, string YieldSource, string EvidenceTrace)
{
    public int Class => Math.Max(Flange.Class, Web.Class);
    public string GoverningParts => Flange.Class == Web.Class ? "FlangeAndWeb" :
        (Flange.Class > Web.Class ? "Flange" : "Web");
    public bool RequiresEffectiveSectionProperties => Class == 4;
    public bool EstablishesCompleteMemberCompliance => false;
    public bool ProvidesSectionResistance => false;
}

/// <summary>
/// Native NTC 2018 §4.2.3.1 Tables 4.2.III-IV. Independent from the existing
/// M90-A EC3 implementation, despite intentionally equal pure-pattern limits.
/// This does not implement mixed N/M, welds, tapered INP flanges, holes, weak-axis
/// bending, class-4 effective widths or elastic/plastic member resistance.
/// </summary>
public static class Ntc2018SteelISectionClassifierD0
{
    public const string Clause = "NTC 2018, 4.2.3.1, Tabelle 4.2.III e 4.2.IV";
    public static DesignCodeEdition Code => Ntc2018SteelTensionProfileD1.Edition;

    public static Ntc2018SteelISectionClassificationD0 ClassifyCatalogProfile(
        DesignCodeEdition code,
        CommercialRolledSteelCatalogM88B2 profiles,
        CharacteristicMaterialCatalogM88C materials,
        string sectionId, string steelGrade,
        Ntc2018SteelPureActionD0 action,
        Ntc2018SteelClassificationEvidenceD0 declared)
    {
        EnsureCode(code);
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(materials);
        if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(steelGrade))
            throw new ArgumentException("Profile ID and grade are mandatory.");
        var p = profiles.Find(sectionId).Definition;
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            throw new NotSupportedException("NTC D0: only IPE, HEA, HEB parallel-flange hot-rolled catalog profiles; not INP tapered flanges.");
        double t = Math.Max(p.FlangeThicknessMm, p.WebThicknessMm);
        double fy = materials.GetSteelYieldMpa(steelGrade, t);
        return ClassifyGeometry(code, sectionId, steelGrade,
            new Ntc2018SteelISectionDimensionsD0(
                p.HeightMm, p.WidthMm, p.WebThicknessMm, p.FlangeThicknessMm, p.RootRadiusMm),
            fy, action, declared,
            $"{declared?.SectionGeometryEvidence}; OPPO={p.SourcePageUrl}; " +
            $"status={p.DataStatus} (NON CERTIFICATO)",
            $"{declared?.MaterialYieldEvidence}; M88-C grade={steelGrade}; " +
            $"thickness=max(tf,tw)={F(t)} mm; {materials.FindSteel(steelGrade).ReferenceStandard}");
    }

    /// <summary>
    /// Caller-supplied worked-example geometry; signed evidence remains mandatory.
    /// No manufacturing or product certification is inferred from its dimensions.
    /// </summary>
    public static Ntc2018SteelISectionClassificationD0 ClassifyGeometry(
        DesignCodeEdition code,
        string sectionId, string steelGrade,
        Ntc2018SteelISectionDimensionsD0 dimensions,
        double yieldStrengthMpa,
        Ntc2018SteelPureActionD0 action,
        Ntc2018SteelClassificationEvidenceD0 declared,
        string? geometrySource = null,
        string? yieldSource = null)
    {
        EnsureCode(code);
        if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(steelGrade))
            throw new ArgumentException("Section and grade must be identified.");
        ArgumentNullException.ThrowIfNull(dimensions);
        ArgumentNullException.ThrowIfNull(declared);
        if (!Enum.IsDefined(action))
            throw new NotSupportedException("Only pure uniform compression or pure strong-axis bending; mixed N/M unsupported.");
        if (declared.UnperforatedSectionConfirmed != true ||
            declared.HotRolledParallelFlangeAndDoublySymmetricConfirmed != true ||
            (action == Ntc2018SteelPureActionD0.StrongAxisBending && declared.StrongAxisOfBendingConfirmed != true) ||
            string.IsNullOrWhiteSpace(declared.SectionGeometryEvidence) ||
            string.IsNullOrWhiteSpace(declared.MaterialYieldEvidence) ||
            string.IsNullOrWhiteSpace(declared.PureActionEvidence))
            throw new NotSupportedException("Documented unperforated rolled I/H geometry, material strength and pure loading pattern are mandatory; strong-axis evidence for bending.");

        Positive(dimensions.HeightMm, nameof(dimensions.HeightMm));
        Positive(dimensions.WidthMm, nameof(dimensions.WidthMm));
        Positive(dimensions.WebThicknessMm, nameof(dimensions.WebThicknessMm));
        Positive(dimensions.FlangeThicknessMm, nameof(dimensions.FlangeThicknessMm));
        Positive(dimensions.RootRadiusMm, nameof(dimensions.RootRadiusMm));
        Positive(yieldStrengthMpa, nameof(yieldStrengthMpa));
        double flangeFlat = (dimensions.WidthMm - dimensions.WebThicknessMm - 2 * dimensions.RootRadiusMm) / 2d;
        double webFlat = dimensions.HeightMm - 2 * dimensions.FlangeThicknessMm - 2 * dimensions.RootRadiusMm;
        if (!double.IsFinite(flangeFlat) || !double.IsFinite(webFlat) || flangeFlat <= 0 || webFlat <= 0)
            throw new ArgumentException("Fillets/web/flange dimensions leave nonpositive flat widths.");

        // The NTC text gives epsilon = sqrt(235/fyk); printed rounded values in
        // the adjacent table are illustrative, not constants for computation.
        double epsilon = Math.Sqrt(235d / yieldStrengthMpa);
        double flangeRatio = flangeFlat / dimensions.FlangeThicknessMm;
        double webRatio = webFlat / dimensions.WebThicknessMm;
        var flange = Build("OutstandFlange", flangeFlat, dimensions.FlangeThicknessMm,
            flangeRatio, 9 * epsilon, 10 * epsilon, 14 * epsilon);
        bool compression = action == Ntc2018SteelPureActionD0.UniformCompression;
        var web = Build("InternalWeb", webFlat, dimensions.WebThicknessMm, webRatio,
            (compression ? 33 : 72) * epsilon,
            (compression ? 38 : 83) * epsilon,
            (compression ? 42 : 124) * epsilon);
        string geometry = geometrySource ?? declared.SectionGeometryEvidence!;
        string yield = yieldSource ?? declared.MaterialYieldEvidence!;
        string trace = $"SECTION CLASSIFICATION ONLY | {Clause} | " +
            $"code={Code.DocumentId}/{Code.EditionId}/{Code.NationalAnnexId}; " +
            $"section={sectionId}; grade={steelGrade}; action={action}; fyk={F(yieldStrengthMpa)} MPa; " +
            $"epsilon={F(epsilon)}; c_flange={F(flangeFlat)} mm; " +
            $"c_web={F(webFlat)} mm; flange c/t={F(flangeRatio)} class={flange.Class}; " +
            $"web c/t={F(webRatio)} class={web.Class}; " +
            $"class={Math.Max(flange.Class, web.Class)}; geometrySource={geometry}; " +
            $"materialSource={yield}; loadingSource={declared.PureActionEvidence}; " +
            "NO resistance, no Aeff/Weff, mixed action, hole, spatial instability, connection or whole-member verification.";
        return new Ntc2018SteelISectionClassificationD0(code, sectionId, steelGrade, action,
            yieldStrengthMpa, epsilon, flange, web, geometry, yield, trace);
    }

    private static Ntc2018SteelPlateClassificationD0 Build(string name, double width,
        double thickness, double ratio, double l1, double l2, double l3)
    {
        if (!double.IsFinite(ratio) || !double.IsFinite(l1) || !double.IsFinite(l2) ||
            !double.IsFinite(l3) || !(l1 > 0 && l2 > l1 && l3 > l2))
            throw new ArgumentException("Invalid NTC table limits or slenderness.");
        int cl = ratio <= l1 ? 1 : ratio <= l2 ? 2 : ratio <= l3 ? 3 : 4;
        return new Ntc2018SteelPlateClassificationD0(name, width, thickness, ratio,
            l1, l2, l3, cl);
    }

    private static void EnsureCode(DesignCodeEdition code)
    {
        ArgumentNullException.ThrowIfNull(code);
        if (code != Code)
            throw new NotSupportedException("D0 is NTC 2018 only; no EC3 edition, 2022 edition or user-defined annex.");
    }

    private static void Positive(double x, string name)
    {
        if (!double.IsFinite(x) || x <= 0)
            throw new ArgumentOutOfRangeException(name, x, "Finite positive geometry/material value required.");
    }

    private static string F(double x) => x.ToString("G17", CultureInfo.InvariantCulture);
}
