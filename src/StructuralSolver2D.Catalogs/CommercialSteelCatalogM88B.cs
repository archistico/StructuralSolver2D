using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Catalogs;

/// <summary>
/// Nominal, published TABLE properties of a rolled IPE profile. Distinct from M80
/// sharp-corner geometry; the profile's root radii are reflected in the table's A/I.
/// No plastic moduli, shear areas, torsional/warping constants or normative capacities.
/// Columns explicitly use mm, cm², cm³, cm⁴ and kg/m. Never infer missing values.
/// </summary>
public sealed record PublishedIpeSectionM88B(
    string Id, string Name, string Family,
    double HeightMm, double WidthMm, double WebThicknessMm, double FlangeThicknessMm,
    double AreaCm2, double StrongInertiaCm4, double WeakInertiaCm4,
    double StrongElasticModulusCm3, double WeakElasticModulusCm3,
    double MassKgPerM,
    string DimensionStandard, string PropertySourceName, string PropertySourceUrl,
    string SourceCheckedOn, string DataStatus = "PublishedThirdParty");

/// <summary>
/// A solver-ready profile whose area and inertia are source-tabulated, NOT recalculated
/// with SectionGeometryFactoryM80. In-plane bending = strong y-y axis of the IPE.
/// Rotating the physical section to bend around the weak axis requires an explicit
/// alternative orientation; this class never switches axes automatically.
/// </summary>
public sealed record ResolvedIpeSectionM88B(
    PublishedIpeSectionM88B Definition,
    SectionGeometricPropertiesM80 Properties)
{
    public StructuralSection ToSolverSection() => Properties.ToSolverSection();
}

/// <summary>
/// Catalog of published nominal IPE steel dimensions and elastic section properties.
/// Data are third-party reference values, not certified delivery documentation or
/// a valid EC3/NTC resistance or stability verification.
/// </summary>
public sealed class CommercialSteelCatalogM88B
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly IReadOnlyList<ResolvedIpeSectionM88B> sections;
    public string Title { get; }
    public IReadOnlyList<ResolvedIpeSectionM88B> Sections => sections;
    public bool IncludesNormativeVerification => false;

    public CommercialSteelCatalogM88B(string title, IEnumerable<PublishedIpeSectionM88B> definitions)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Catalog title is required.", nameof(title));
        ArgumentNullException.ThrowIfNull(definitions);
        Title = title;
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var items = new List<ResolvedIpeSectionM88B>();
        foreach (var d in definitions)
        {
            if (d is null) throw new ArgumentException("Null catalog entry.", nameof(definitions));
            Validate(d);
            if (!ids.Add(d.Id)) throw new ArgumentException($"Duplicate section identifier '{d.Id}'.", nameof(definitions));
            items.Add(new ResolvedIpeSectionM88B(d, Resolve(d)));
        }
        sections = Array.AsReadOnly(items.ToArray());
    }

    public ResolvedIpeSectionM88B Find(string id) =>
        sections.FirstOrDefault(x => string.Equals(x.Definition.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown commercial IPE profile '{id}'.");

    /// <summary>
    /// Only produces elastic FEM inputs with an explicit steel material preset.
    /// No fy/fu, EN 1993 or NTC factors are guessed from a grade label.
    /// </summary>
    public ElasticAnalysisSelectionM88A SelectForElasticAnalysis(string sectionId, AnalysisCatalogM88A materials, string materialId)
    {
        ArgumentNullException.ThrowIfNull(materials);
        var profile = Find(sectionId);
        var material = materials.FindMaterial(materialId);
        if (material.Family != CatalogMaterialFamilyM88A.Steel)
            throw new InvalidOperationException("Commercial IPE sections require a steel material; no implicit substitution.");
        return new ElasticAnalysisSelectionM88A(
            profile.ToSolverSection(),
            new StructuralMaterial(material.Id, material.Name, material.ElasticModulusKnPerM2, material.UnitWeightKnPerM3),
            CatalogMaterialFamilyM88A.Steel, profile.Definition.DataStatus, material.DataStatus);
    }

    public string ToJson() => JsonSerializer.Serialize(
        new CatalogDocument(SchemaVersion, Title, sections.Select(x => x.Definition).ToArray()), Options);

    public static CommercialSteelCatalogM88B FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON is required.", nameof(json));
        var doc = JsonSerializer.Deserialize<CatalogDocument>(json, Options)
            ?? throw new InvalidOperationException("Missing catalog document.");
        if (doc.SchemaVersion != SchemaVersion)
            throw new NotSupportedException($"Unsupported M88-B schema version '{doc.SchemaVersion}'.");
        if (doc.Sections is null) throw new InvalidOperationException("Catalog sections array is required.");
        return new CommercialSteelCatalogM88B(doc.Title, doc.Sections);
    }

    public static CommercialSteelCatalogM88B Load(string path) => FromJson(File.ReadAllText(path));

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output path is required.", nameof(path));
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(path, ToJson());
    }

    private static SectionGeometricPropertiesM80 Resolve(PublishedIpeSectionM88B d)
    {
        const double cm2ToM2 = 1e-4;
        const double cm4ToM4 = 1e-8;
        const double cm3ToM3 = 1e-6;
        var section = new StructuralSection(
            d.Id, d.Name, d.AreaCm2 * cm2ToM2, d.StrongInertiaCm4 * cm4ToM4,
            d.HeightMm * 1e-3, d.WidthMm * 1e-3);
        double otherI = d.WeakInertiaCm4 * cm4ToM4;
        return new SectionGeometricPropertiesM80(
            section, "RolledIPEPublishedTable", otherI,
            d.StrongElasticModulusCm3 * cm3ToM3,
            d.WeakElasticModulusCm3 * cm3ToM3,
            Math.Sqrt(section.MomentOfInertia / section.Area),
            Math.Sqrt(otherI / section.Area));
    }

    private static void Validate(PublishedIpeSectionM88B d)
    {
        if (string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Name))
            throw new ArgumentException("Section id and name are required.");
        if (d.Family != "IPE") throw new NotSupportedException("M88-B supports the IPE family only; never re-label shapes.");
        if (d.DataStatus != "PublishedThirdParty")
            throw new ArgumentException("M88-B must explicitly declare PublishedThirdParty provenance.");
        if (d.DimensionStandard != "EN 10365:2026")
            throw new ArgumentException("Explicit EN 10365:2026 dimensional reference required.");
        if (string.IsNullOrWhiteSpace(d.PropertySourceName) ||
            string.IsNullOrWhiteSpace(d.SourceCheckedOn) ||
            !DateOnly.TryParseExact(d.SourceCheckedOn, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out _) ||
            !Uri.TryCreate(d.PropertySourceUrl, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Documented property source, HTTPS URL and ISO source date required.");
        Positive(d.HeightMm, nameof(d.HeightMm));
        Positive(d.WidthMm, nameof(d.WidthMm));
        Positive(d.WebThicknessMm, nameof(d.WebThicknessMm));
        Positive(d.FlangeThicknessMm, nameof(d.FlangeThicknessMm));
        Positive(d.AreaCm2, nameof(d.AreaCm2));
        Positive(d.StrongInertiaCm4, nameof(d.StrongInertiaCm4));
        Positive(d.WeakInertiaCm4, nameof(d.WeakInertiaCm4));
        Positive(d.StrongElasticModulusCm3, nameof(d.StrongElasticModulusCm3));
        Positive(d.WeakElasticModulusCm3, nameof(d.WeakElasticModulusCm3));
        Positive(d.MassKgPerM, nameof(d.MassKgPerM));
        if (d.WebThicknessMm >= d.WidthMm || d.FlangeThicknessMm * 2 >= d.HeightMm)
            throw new ArgumentException("Incompatible IPE dimensions.");
        if (d.StrongInertiaCm4 <= d.WeakInertiaCm4)
            throw new ArgumentException("Strong and weak axis inertias appear swapped.");
        // W_el = I / distance to extreme fibre. Permit rounding of published tables.
        double expectedStrongW = 20.0 * d.StrongInertiaCm4 / d.HeightMm;
        double expectedWeakW = 20.0 * d.WeakInertiaCm4 / d.WidthMm;
        if (RelativeError(d.StrongElasticModulusCm3, expectedStrongW) > 0.015 ||
            RelativeError(d.WeakElasticModulusCm3, expectedWeakW) > 0.015)
            throw new ArgumentException("I/W/axis/dimension data are mutually inconsistent.");
        // 0.785 kg/(m*cm²) from a nominal steel density of 7850 kg/m³;
        // table mass is rounded and must not be treated as measured product mass.
        if (RelativeError(d.MassKgPerM, 0.785 * d.AreaCm2) > 0.05)
            throw new ArgumentException("Published mass/area data are inconsistent.");
        double heightCm = d.HeightMm / 10.0;
        double widthCm = d.WidthMm / 10.0;
        if (d.StrongInertiaCm4 >= d.AreaCm2 * heightCm * heightCm / 4.0 ||
            d.WeakInertiaCm4 >= d.AreaCm2 * widthCm * widthCm / 4.0)
            throw new ArgumentException("Inertia violates section bounding-box limits.");
    }

    private static double RelativeError(double actual, double expected) => Math.Abs(actual - expected) / expected;
    private static void Positive(double value, string name)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(name, value, "Positive finite tabular value required.");
    }

    private sealed record CatalogDocument(int SchemaVersion, string Title, IReadOnlyList<PublishedIpeSectionM88B>? Sections);
}
