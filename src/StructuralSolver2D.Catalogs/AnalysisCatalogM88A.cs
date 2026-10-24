using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Sections;

namespace StructuralSolver2D.Catalogs;

/// <summary>Strictly an ANALYSIS catalog, not a code-design material classification.</summary>
public enum CatalogMaterialFamilyM88A { Unknown = 0, Steel = 1, Timber = 2 }

/// <summary>Provenance of elastic-only material values (never strength or partial factors).</summary>
public sealed record ElasticMaterialEntryM88A(
    string Id, string Name, CatalogMaterialFamilyM88A Family,
    double ElasticModulusKnPerM2, double UnitWeightKnPerM3,
    string DataStatus = "AnalysisPreset",
    string? SourceDocument = null, string? SourceEdition = null);

/// <summary>M80 geometry is reused; no duplicate section-formula implementation.</summary>
public sealed record AnalysisSectionEntryM88A(
    CatalogMaterialFamilyM88A Family, SectionProfileDefinitionM80 Geometry);

public sealed record ResolvedAnalysisSectionM88A(
    CatalogMaterialFamilyM88A Family, ResolvedSectionProfileM80 Geometry);

/// <summary>Only a load-bearing FEM input pair, never a code-conformity determination.</summary>
public sealed record ElasticAnalysisSelectionM88A(
    StructuralSection Section, StructuralMaterial Material,
    CatalogMaterialFamilyM88A Family,
    string GeometryDataStatus, string MaterialDataStatus)
{
    public bool IncludesNormativeStrengthOrDesignFactors => false;
}

/// <summary>
/// Versioned, immutable-in-use catalog for preliminary linear elastic 2D structural analysis.
/// Values for I, RHS, CHS are idealized by M80 (no radii or fabrication corrections).
/// Design strengths, lateral buckling parameters, partial factors and EC3/EC5/NTC checks
/// are intentionally absent. Missing material/geometry data is never silently substituted.
/// </summary>
public sealed class AnalysisCatalogM88A
{
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IReadOnlyList<ElasticMaterialEntryM88A> materials;
    private readonly IReadOnlyList<ResolvedAnalysisSectionM88A> sections;

    public AnalysisCatalogM88A(
        string title,
        IEnumerable<ElasticMaterialEntryM88A> materials,
        IEnumerable<AnalysisSectionEntryM88A> sections)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Catalog title is required.", nameof(title));
        ArgumentNullException.ThrowIfNull(materials);
        ArgumentNullException.ThrowIfNull(sections);
        Title = title;

        var materialItems = materials.ToArray();
        var materialIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var material in materialItems)
        {
            if (material is null) throw new ArgumentException("Null material entry.", nameof(materials));
            if (string.IsNullOrWhiteSpace(material.Id) || string.IsNullOrWhiteSpace(material.Name))
                throw new ArgumentException("Material id and name are required.", nameof(materials));
            if (material.Family == CatalogMaterialFamilyM88A.Unknown || !Enum.IsDefined(material.Family))
                throw new ArgumentException("Unsupported material family.", nameof(materials));
            if (!materialIds.Add(material.Id))
                throw new ArgumentException($"Duplicate material id '{material.Id}'.", nameof(materials));
            RequirePositiveFinite(material.ElasticModulusKnPerM2, nameof(material.ElasticModulusKnPerM2));
            RequirePositiveFinite(material.UnitWeightKnPerM3, nameof(material.UnitWeightKnPerM3));
            if (material.DataStatus != "AnalysisPreset" && material.DataStatus != "SourceDocumented")
                throw new ArgumentException($"Unsupported material data status '{material.DataStatus}'.", nameof(materials));
            if (material.DataStatus == "SourceDocumented" &&
                (string.IsNullOrWhiteSpace(material.SourceDocument) || string.IsNullOrWhiteSpace(material.SourceEdition)))
                throw new ArgumentException("SourceDocumented material needs document and edition.", nameof(materials));
        }
        this.materials = Array.AsReadOnly(materialItems);

        var sectionEntries = sections.ToArray();
        if (sectionEntries.Any(s => s is null || s.Geometry is null || s.Family == CatalogMaterialFamilyM88A.Unknown || !Enum.IsDefined(s.Family)))
            throw new ArgumentException("Null geometry or invalid section family.", nameof(sections));
        // Do not infer a missing family from the enum default, or silently allow
        // timber-only shapes to be labeled as steel (and vice versa).
        foreach (var entry in sectionEntries)
        {
            bool timberGeometry = entry.Geometry.Shape == "TimberRectangular";
            bool steelGeometry = entry.Geometry.Shape is "RectangularHollow" or "CircularHollow" or "DoublySymmetricI";
            if ((timberGeometry && entry.Family != CatalogMaterialFamilyM88A.Timber) ||
                (steelGeometry && entry.Family != CatalogMaterialFamilyM88A.Steel))
                throw new ArgumentException($"Section '{entry.Geometry.Id}' has a shape incompatible with its family.", nameof(sections));
        }
        // Existing M80 resolver enforces dimensions, shape, provenance and duplicate identifiers.
        var m80 = new SectionProfileCatalogM80(sectionEntries.Select(s => s.Geometry), title);
        this.sections = Array.AsReadOnly(sectionEntries.Select(entry =>
            new ResolvedAnalysisSectionM88A(entry.Family, m80.Find(entry.Geometry.Id))).ToArray());
    }

    public string Title { get; }
    public IReadOnlyList<ElasticMaterialEntryM88A> Materials => materials;
    public IReadOnlyList<ResolvedAnalysisSectionM88A> Sections => sections;
    public bool IncludesNormativeMaterialStrengths => false;

    public ResolvedAnalysisSectionM88A FindSection(string id) =>
        sections.FirstOrDefault(s => string.Equals(s.Geometry.Definition.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown M88 section '{id}'.");

    public ElasticMaterialEntryM88A FindMaterial(string id) =>
        materials.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown M88 material '{id}'.");

    public ElasticAnalysisSelectionM88A SelectForElasticAnalysis(string sectionId, string materialId)
    {
        var section = FindSection(sectionId);
        var material = FindMaterial(materialId);
        if (section.Family != material.Family)
            throw new InvalidOperationException(
                $"Material '{materialId}' ({material.Family}) cannot be assigned to section '{sectionId}' ({section.Family}).");
        return new ElasticAnalysisSelectionM88A(
            section.Geometry.Properties.ToSolverSection(),
            new StructuralMaterial(material.Id, material.Name, material.ElasticModulusKnPerM2, material.UnitWeightKnPerM3),
            material.Family, section.Geometry.Definition.DataStatus, material.DataStatus);
    }

    public string ToJson() => JsonSerializer.Serialize(
        new CatalogDocument(CurrentSchemaVersion, Title, materials,
            sections.Select(s => new AnalysisSectionEntryM88A(s.Family, s.Geometry.Definition)).ToArray()), JsonOptions);

    public static AnalysisCatalogM88A FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON is empty.", nameof(json));
        var document = JsonSerializer.Deserialize<CatalogDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Catalog JSON is missing.");
        if (document.SchemaVersion != CurrentSchemaVersion)
            throw new NotSupportedException($"M88 schema version '{document.SchemaVersion}' not supported.");
        if (document.Materials is null || document.Sections is null)
            throw new InvalidOperationException("Catalog materials and sections arrays are required.");
        return new AnalysisCatalogM88A(document.Title, document.Materials, document.Sections);
    }

    public static AnalysisCatalogM88A Load(string path) => FromJson(File.ReadAllText(path));

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Output path is required.", nameof(path));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, ToJson());
    }

    private static void RequirePositiveFinite(double value, string field)
    {
        if (!double.IsFinite(value) || value <= 0.0)
            throw new ArgumentOutOfRangeException(field, value, "Elastic property must be finite and positive.");
    }

    private sealed record CatalogDocument(
        int SchemaVersion, string Title,
        IReadOnlyList<ElasticMaterialEntryM88A>? Materials,
        IReadOnlyList<AnalysisSectionEntryM88A>? Sections);
}
