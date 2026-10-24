using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Catalogs;

/// <summary>
/// Source-transcribed, INDICATIVE geometric properties from an OPPO steel table.
/// References shown on those pages are historical UNI designations, NOT declarations
/// that the section satisfies a current dimensional, product or design standard.
/// "Strong" corresponds to OPPO Jx/Wx; "Weak" to OPPO Jy/Wy.
/// </summary>
public sealed record PublishedRolledSteelSectionM88B2(
    string Id, string Name, string Family,
    double HeightMm, double WidthMm, double WebThicknessMm, double FlangeThicknessMm,
    double RootRadiusMm, double MassKgPerM, double AreaCm2,
    double StrongInertiaCm4, double WeakInertiaCm4,
    double StrongElasticModulusCm3, double WeakElasticModulusCm3,
    double StrongRadiusCm, double WeakRadiusCm,
    string ReferenceTable, string SourceName, string SourcePageUrl,
    string SourceCheckedOn, string DataStatus = "IndicativeThirdParty");

/// <summary>
/// Strong-axis linear-elastic, in-plane FEM is available for all four families.
/// This conversion does NOT model inclined flanges, shear deformation, torsion,
/// lateral buckling or section class; the tabulated A and Ix are preserved exactly.
/// </summary>
public sealed record ResolvedRolledSteelSectionM88B2(
    PublishedRolledSteelSectionM88B2 Definition,
    SectionGeometricPropertiesM80 Properties)
{
    public StructuralSection ToSolverSection() => Properties.ToSolverSection();
    public bool HasInclinedFlanges => Definition.Family == "INP";
}

/// <summary>
/// Versioned, strict catalog of source-indicative IPE/HEA/HEB/INP section properties.
/// It is NOT a code-design, product-compliance or procurement certification database.
/// </summary>
public sealed class CommercialRolledSteelCatalogM88B2
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly IReadOnlyList<ResolvedRolledSteelSectionM88B2> sections;

    public string Title { get; }
    public IReadOnlyList<ResolvedRolledSteelSectionM88B2> Sections => sections;
    public bool IncludesNormativeVerification => false;

    public CommercialRolledSteelCatalogM88B2(
        string title, IEnumerable<PublishedRolledSteelSectionM88B2> definitions)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Catalog title is required.", nameof(title));
        ArgumentNullException.ThrowIfNull(definitions);
        Title = title;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<ResolvedRolledSteelSectionM88B2>();
        foreach (var item in definitions)
        {
            if (item is null)
                throw new ArgumentException("Catalog contains a null section.", nameof(definitions));
            Validate(item);
            if (!ids.Add(item.Id))
                throw new ArgumentException($"Duplicate profile identifier '{item.Id}'.", nameof(definitions));
            list.Add(new ResolvedRolledSteelSectionM88B2(item, Resolve(item)));
        }
        sections = Array.AsReadOnly(list.ToArray());
    }

    public ResolvedRolledSteelSectionM88B2 Find(string id) =>
        sections.FirstOrDefault(s => string.Equals(s.Definition.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown OPPO profile '{id}'.");

    public IReadOnlyList<ResolvedRolledSteelSectionM88B2> FindFamily(string family)
    {
        if (!IsSupportedFamily(family)) throw new NotSupportedException($"Unknown steel family '{family}'.");
        return sections.Where(s => string.Equals(s.Definition.Family, family, StringComparison.Ordinal)).ToArray();
    }

    /// <summary>
    /// Return geometry and an existing STEEL elastic preset; never infer fy, fu,
    /// section class, out-of-plane restraints or Eurocode/NTC safety factors.
    /// </summary>
    public ElasticAnalysisSelectionM88A SelectForElasticAnalysis(
        string sectionId, AnalysisCatalogM88A elasticCatalog, string materialId)
    {
        ArgumentNullException.ThrowIfNull(elasticCatalog);
        var profile = Find(sectionId);
        var material = elasticCatalog.FindMaterial(materialId);
        if (material.Family != CatalogMaterialFamilyM88A.Steel)
            throw new InvalidOperationException("Rolled steel profiles require an explicitly selected steel preset.");
        return new ElasticAnalysisSelectionM88A(
            profile.ToSolverSection(),
            new StructuralMaterial(material.Id, material.Name,
                material.ElasticModulusKnPerM2, material.UnitWeightKnPerM3),
            CatalogMaterialFamilyM88A.Steel, profile.Definition.DataStatus, material.DataStatus);
    }

    public string ToJson() => JsonSerializer.Serialize(
        new CatalogDocument(SchemaVersion, Title, sections.Select(s => s.Definition).ToArray()), Options);

    public static CommercialRolledSteelCatalogM88B2 FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON is required.", nameof(json));
        var document = JsonSerializer.Deserialize<CatalogDocument>(json, Options)
            ?? throw new InvalidOperationException("Invalid catalog document.");
        if (document.SchemaVersion != SchemaVersion)
            throw new NotSupportedException($"Unsupported OPPO catalog schema version '{document.SchemaVersion}'.");
        if (document.Sections is null)
            throw new InvalidOperationException("Sections are mandatory; absent is not equivalent to empty.");
        return new CommercialRolledSteelCatalogM88B2(document.Title, document.Sections);
    }

    public static CommercialRolledSteelCatalogM88B2 Load(string path) => FromJson(File.ReadAllText(path));

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output path is required.", nameof(path));
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(path, ToJson());
    }

    private static SectionGeometricPropertiesM80 Resolve(PublishedRolledSteelSectionM88B2 item)
    {
        var solverSection = new StructuralSection(
            item.Id, item.Name, item.AreaCm2 * 1e-4, item.StrongInertiaCm4 * 1e-8,
            item.HeightMm * 1e-3, item.WidthMm * 1e-3);
        return new SectionGeometricPropertiesM80(
            solverSection,
            item.Family == "INP" ? "RolledINPInclinedFlangesTabulated" : $"Rolled{item.Family}Tabulated",
            item.WeakInertiaCm4 * 1e-8,
            item.StrongElasticModulusCm3 * 1e-6,
            item.WeakElasticModulusCm3 * 1e-6,
            Math.Sqrt(item.StrongInertiaCm4 / item.AreaCm2) * 1e-2,
            Math.Sqrt(item.WeakInertiaCm4 / item.AreaCm2) * 1e-2);
    }

    private static bool IsSupportedFamily(string? family) => family is "IPE" or "HEA" or "HEB" or "INP";

    private static void Validate(PublishedRolledSteelSectionM88B2 p)
    {
        if (string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Name))
            throw new ArgumentException("Profile id and name are required.");
        if (!IsSupportedFamily(p.Family))
            throw new NotSupportedException("Only IPE, HEA, HEB and INP families are admitted.");
        if (!p.Id.StartsWith(p.Family + "-", StringComparison.Ordinal))
            throw new ArgumentException("Profile ID must match its family.");
        var declaredStandard = p.Family switch
        {
            "IPE" => "UNI 5398-78",
            "HEA" or "HEB" => "UNI 5397-78",
            "INP" => "UNI 5679-73",
            _ => throw new NotSupportedException("Unsupported family.")
        };
        if (p.ReferenceTable != declaredStandard)
            throw new ArgumentException("Original OPPO table reference must be retained verbatim; it is NOT a current standard certification.");
        if (p.DataStatus != "IndicativeThirdParty" || p.SourceName != "OPPO - tabelle profilati metallici")
            throw new ArgumentException("Must preserve indicative third-party status and OPPO attribution.");
        var canonical = $"https://www.oppo.it/tabelle/profilati_{p.Family.ToLowerInvariant()}.htm";
        if (!string.Equals(p.SourcePageUrl, canonical, StringComparison.Ordinal))
            throw new ArgumentException("Incorrect OPPO source page for profile family.");
        if (!DateOnly.TryParseExact(p.SourceCheckedOn, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out _))
            throw new ArgumentException("ISO source-check date is mandatory.");

        Positive(p.HeightMm, nameof(p.HeightMm));
        Positive(p.WidthMm, nameof(p.WidthMm));
        Positive(p.WebThicknessMm, nameof(p.WebThicknessMm));
        Positive(p.FlangeThicknessMm, nameof(p.FlangeThicknessMm));
        Positive(p.RootRadiusMm, nameof(p.RootRadiusMm));
        Positive(p.MassKgPerM, nameof(p.MassKgPerM));
        Positive(p.AreaCm2, nameof(p.AreaCm2));
        Positive(p.StrongInertiaCm4, nameof(p.StrongInertiaCm4));
        Positive(p.WeakInertiaCm4, nameof(p.WeakInertiaCm4));
        Positive(p.StrongElasticModulusCm3, nameof(p.StrongElasticModulusCm3));
        Positive(p.WeakElasticModulusCm3, nameof(p.WeakElasticModulusCm3));
        Positive(p.StrongRadiusCm, nameof(p.StrongRadiusCm));
        Positive(p.WeakRadiusCm, nameof(p.WeakRadiusCm));

        if (2 * p.FlangeThicknessMm >= p.HeightMm || p.WebThicknessMm >= p.WidthMm)
            throw new ArgumentException("Physically incompatible flange/web dimensions.");
        if (p.StrongInertiaCm4 <= p.WeakInertiaCm4)
            throw new ArgumentException("Strong and weak axes appear swapped.");
        if (p.AreaCm2 >= p.HeightMm * p.WidthMm * 1e-2)
            throw new ArgumentException("Tabulated area exceeds bounding rectangle.");
        if (p.StrongInertiaCm4 >= p.AreaCm2 * Math.Pow(p.HeightMm / 10, 2) / 4 ||
            p.WeakInertiaCm4 >= p.AreaCm2 * Math.Pow(p.WidthMm / 10, 2) / 4)
            throw new ArgumentException("Inertia exceeds geometric bounding limit.");

        // Independent arithmetic consistency checks only: preserve published values,
        // do not replace them by formulas for a sharp-corner I-section.
        CheckClose(p.StrongElasticModulusCm3, 20 * p.StrongInertiaCm4 / p.HeightMm, 0.015, "Jx/Wx/h");
        CheckClose(p.WeakElasticModulusCm3, 20 * p.WeakInertiaCm4 / p.WidthMm, 0.015, "Jy/Wy/b");
        CheckClose(p.StrongRadiusCm, Math.Sqrt(p.StrongInertiaCm4 / p.AreaCm2), 0.02, "ix/Jx/A");
        CheckClose(p.WeakRadiusCm, Math.Sqrt(p.WeakInertiaCm4 / p.AreaCm2), 0.02, "iy/Jy/A");
        CheckClose(p.MassKgPerM, 0.785 * p.AreaCm2, 0.05, "mass/area/nominal-density");
    }

    private static void CheckClose(double observed, double expected, double tolerance, string field)
    {
        if (Math.Abs(observed - expected) / expected > tolerance)
            throw new ArgumentException($"Inconsistent tabulated values: {field}.");
    }

    private static void Positive(double value, string field)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(field, value, "Finite positive tabulated value required.");
    }

    private sealed record CatalogDocument(
        int SchemaVersion, string Title, IReadOnlyList<PublishedRolledSteelSectionM88B2>? Sections);
}
