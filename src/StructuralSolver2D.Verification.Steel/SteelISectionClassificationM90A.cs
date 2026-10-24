using System.Globalization;
using StructuralSolver2D.Catalogs;
using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Steel;

/// <summary>Two explicitly supported EC3 Table 5.2 loading patterns; no mixed N/M states.</summary>
public enum SteelISectionActionM90A
{
    PureUniformCompression = 1,
    PureStrongAxisBending = 2
}

/// <summary>Dimensions in mm, including the root fillet. For parallel-flange rolled I/H sections only.</summary>
public sealed record SteelISectionDimensionsM90A(
    double HeightMm, double WidthMm, double WebThicknessMm,
    double FlangeThicknessMm, double RootRadiusMm);

/// <summary>A local plate classification, NOT a design resistance or member verification.</summary>
public sealed record SteelIPlateClassificationM90A(
    string Part, double FlatWidthMm, double ThicknessMm, double Slenderness,
    double Class1Limit, double Class2Limit, double Class3Limit, int Class);

/// <summary>
/// EN 1993-1-1:2005+A1:2014, clause 5.5, Table 5.2, sheets 1 and 2.
/// Valid only for unperforated, hot-rolled, parallel-flange, doubly symmetric I/H
/// profiles, pure uniform axial compression OR pure strong-axis bending.
/// A class-4 outcome does not calculate effective section properties.
/// It never certifies a member or asserts EN/NTC compliance.
/// </summary>
public sealed record SteelISectionClassificationM90A(
    string SectionId, string SteelGrade, SteelISectionActionM90A Action,
    double YieldStrengthMpa, double Epsilon,
    SteelIPlateClassificationM90A Flange, SteelIPlateClassificationM90A Web,
    string GeometryReference, string YieldReference,
    string CodeReference, string Evidence)
{
    public int Class => Math.Max(Flange.Class, Web.Class);
    public bool RequiresEffectiveSectionProperties => Class == 4;
    public bool IsCompleteMemberVerification => false;
    public bool AllowsAutomaticApproval => false;
    public string GoverningParts => Flange.Class == Web.Class ? "FlangeAndWeb" :
        (Flange.Class > Web.Class ? "Flange" : "Web");
}

/// <summary>
/// Pure-section classifier. An explicit Eurocode edition/annex is mandatory for catalog use.
/// Geometry-only overload supports independent worked-example regression, but its input
/// material strength and dimensions are supplied by the caller and do not prove product conformity.
/// </summary>
public static class SteelISectionClassifierM90A
{
    public const string Standard = "EN 1993-1-1:2005+A1:2014";
    public const string Clause = "5.5, Table 5.2 (internal parts and outstand flanges)";

    public static SteelISectionClassificationM90A ClassifyCatalogProfile(
        DesignCodeEdition edition,
        CommercialRolledSteelCatalogM88B2 profiles,
        CharacteristicMaterialCatalogM88C materials,
        string sectionId, string steelGrade, SteelISectionActionM90A action)
    {
        ArgumentNullException.ThrowIfNull(edition);
        if (edition.DocumentId != "EN 1993-1-1" ||
            edition.EditionId != "2005+A1:2014" ||
            string.IsNullOrWhiteSpace(edition.NationalAnnexId))
            throw new NotSupportedException("M90-A only covers EN 1993-1-1:2005+A1:2014 with declared national-annex identity. NTC and 2022 are separate editions.");
        ArgumentNullException.ThrowIfNull(profiles);
        ArgumentNullException.ThrowIfNull(materials);
        var p = profiles.Find(sectionId).Definition;
        if (p.Family is not ("IPE" or "HEA" or "HEB"))
            throw new NotSupportedException("M90-A does not classify INP tapered flanges or other profile families.");
        var t = Math.Max(p.FlangeThicknessMm, p.WebThicknessMm);
        double fy = materials.GetSteelYieldMpa(steelGrade, t);
        var geometry = new SteelISectionDimensionsM90A(
            p.HeightMm, p.WidthMm, p.WebThicknessMm, p.FlangeThicknessMm, p.RootRadiusMm);
        return ClassifyGeometry(sectionId, steelGrade, geometry, fy, action,
            $"{p.SourceName}; {p.SourcePageUrl}; {p.DataStatus}; {p.SourceCheckedOn}",
            $"{materials.FindSteel(steelGrade).ReferenceStandard}; thickness=max(tf,tw)={F(t)} mm; {materials.FindSteel(steelGrade).DataStatus}");
    }

    public static SteelISectionClassificationM90A ClassifyGeometry(
        string sectionId, string steelGrade, SteelISectionDimensionsM90A geometry,
        double yieldStrengthMpa, SteelISectionActionM90A action,
        string geometryReference, string yieldReference)
    {
        if (string.IsNullOrWhiteSpace(sectionId) || string.IsNullOrWhiteSpace(steelGrade) ||
            string.IsNullOrWhiteSpace(geometryReference) || string.IsNullOrWhiteSpace(yieldReference))
            throw new ArgumentException("Profile, grade, geometry provenance and fy provenance are mandatory.");
        ArgumentNullException.ThrowIfNull(geometry);
        if (!Enum.IsDefined(action))
            throw new NotSupportedException("Unsupported load pattern; combined axial force and bending require M90-B.");
        Positive(geometry.HeightMm, nameof(geometry.HeightMm));
        Positive(geometry.WidthMm, nameof(geometry.WidthMm));
        Positive(geometry.WebThicknessMm, nameof(geometry.WebThicknessMm));
        Positive(geometry.FlangeThicknessMm, nameof(geometry.FlangeThicknessMm));
        Positive(geometry.RootRadiusMm, nameof(geometry.RootRadiusMm));
        Positive(yieldStrengthMpa, nameof(yieldStrengthMpa));
        double flangeFlat = (geometry.WidthMm - geometry.WebThicknessMm - 2*geometry.RootRadiusMm) / 2;
        double webFlat = geometry.HeightMm - 2*geometry.FlangeThicknessMm - 2*geometry.RootRadiusMm;
        if (!double.IsFinite(flangeFlat) || !double.IsFinite(webFlat) || flangeFlat <= 0 || webFlat <= 0)
            throw new ArgumentException("I/H root radius or plate dimensions leave nonpositive flat widths; unsupported geometry.");
        double epsilon = Math.Sqrt(235.0 / yieldStrengthMpa);
        double flangeSlenderness = flangeFlat / geometry.FlangeThicknessMm;
        double webSlenderness = webFlat / geometry.WebThicknessMm;
        var flange = Build("OutstandFlange", flangeFlat, geometry.FlangeThicknessMm,
            flangeSlenderness, new [] {9*epsilon, 10*epsilon, 14*epsilon});
        double[] limits = action == SteelISectionActionM90A.PureUniformCompression
            ? new[] {33*epsilon,38*epsilon,42*epsilon}
            : new[] {72*epsilon,83*epsilon,124*epsilon};
        var web = Build("InternalWeb", webFlat, geometry.WebThicknessMm, webSlenderness, limits);
        var evidence = $"{Standard}; {Clause}; {action}; geometry={geometryReference}; " +
            $"fy={F(yieldStrengthMpa)} MPa ({yieldReference}); epsilon={F(epsilon)}; " +
            $"outstand c/t={F(flangeSlenderness)} class={flange.Class}; " +
            $"web c/t={F(webSlenderness)} class={web.Class}; " +
            $"overall class={Math.Max(flange.Class,web.Class)}; " +
            "NOT checked: axial/bending interaction, weak-axis bending, perforations, " +
            "effective widths for class 4, shear, lateral-torsional/member buckling or NTC compliance.";
        return new SteelISectionClassificationM90A(sectionId, steelGrade, action,
            yieldStrengthMpa, epsilon, flange, web, geometryReference, yieldReference,
            $"{Standard}; {Clause}", evidence);
    }

    private static SteelIPlateClassificationM90A Build(string part, double c, double t,
        double ratio, double[] limits)
    {
        if (!double.IsFinite(ratio) || !limits.All(double.IsFinite) ||
            limits[0] <= 0 || limits[1] <= limits[0] || limits[2] <= limits[1])
            throw new ArgumentException("Invalid slenderness or EC3 limits.");
        int cl = ratio <= limits[0] ? 1 : ratio <= limits[1] ? 2 : ratio <= limits[2] ? 3 : 4;
        return new SteelIPlateClassificationM90A(part,c,t,ratio,limits[0],limits[1],limits[2],cl);
    }
    private static void Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, value, "Finite positive value required.");
    }
    private static string F(double n) => n.ToString("G17", CultureInfo.InvariantCulture);
}
