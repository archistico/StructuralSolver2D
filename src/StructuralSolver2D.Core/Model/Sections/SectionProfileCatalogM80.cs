namespace StructuralSolver2D.Core.Model.Sections;

using System.Text.Json;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Units;

/// <summary>
/// Dimension-based catalog entry. Dimensions are explicit millimeters from a source or
/// a clearly labeled example. No implicit national-standard or manufacturer attribution.
/// </summary>
public sealed record SectionProfileDefinitionM80(
    string Id,
    string Name,
    string Shape,
    double? WidthMm = null,
    double? HeightMm = null,
    double? WallThicknessMm = null,
    double? WebThicknessMm = null,
    double? FlangeThicknessMm = null,
    double? OuterDiameterMm = null,
    double? InnerDiameterMm = null,
    string DataStatus = "Illustrative",
    string? SourceDocument = null,
    string? SourceEdition = null);

public sealed record ResolvedSectionProfileM80(
    SectionProfileDefinitionM80 Definition,
    SectionGeometricPropertiesM80 Properties);

/// <summary>
/// Separate additive catalog contract. The legacy v1 StructuralSectionCatalog JSON
/// is deliberately unchanged; the solver still receives ordinary StructuralSection.
/// </summary>
public sealed class SectionProfileCatalogM80
{
    private const int SchemaVersion = 1;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly List<ResolvedSectionProfileM80> profiles = new();

    public string Title { get; }
    public IReadOnlyList<ResolvedSectionProfileM80> Profiles => profiles;

    public SectionProfileCatalogM80(IEnumerable<SectionProfileDefinitionM80> definitions, string title = "Section geometry catalog")
    {
        ArgumentNullException.ThrowIfNull(definitions);
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Catalog title must not be empty.", nameof(title));
        Title = title;

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (SectionProfileDefinitionM80 definition in definitions)
        {
            if (definition is null)
                throw new ArgumentException("Catalog cannot contain null profiles.", nameof(definitions));
            if (string.IsNullOrWhiteSpace(definition.Id) || string.IsNullOrWhiteSpace(definition.Name))
                throw new ArgumentException("Every catalog profile requires a non-empty id and name.", nameof(definitions));
            if (!ids.Add(definition.Id))
                throw new InvalidOperationException($"Duplicate section profile id '{definition.Id}'.");
            ValidateProvenance(definition);
            profiles.Add(new ResolvedSectionProfileM80(definition, Resolve(definition)));
        }
    }

    public ResolvedSectionProfileM80 Find(string id) =>
        profiles.FirstOrDefault(p => string.Equals(p.Definition.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Section profile '{id}' was not found.");

    public StructuralSectionCatalog ToSolverCatalog() =>
        new(profiles.Select(profile => profile.Properties.ToSolverSection()));

    public string ToJson() => JsonSerializer.Serialize(
        new CatalogDocument(SchemaVersion, Title, profiles.Select(p => p.Definition).ToArray()), Options);

    public static SectionProfileCatalogM80 FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("Catalog JSON cannot be empty.", nameof(json));
        CatalogInputDocument? input = JsonSerializer.Deserialize<CatalogInputDocument>(json, Options);
        if (input is null || input.Profiles is null)
            throw new InvalidOperationException("M80 catalog JSON must contain a profiles array.");
        if (input.SchemaVersion != SchemaVersion)
            throw new NotSupportedException($"Unsupported M80 profile catalog schema version {input.SchemaVersion}.");
        if (input.Profiles.Any(p => p is null))
            throw new InvalidOperationException("M80 catalog profiles cannot contain null entries.");
        return new SectionProfileCatalogM80(input.Profiles.Select(profile => profile!), input.Title ?? "Section geometry catalog");
    }

    public static SectionProfileCatalogM80 Load(string path) => FromJson(File.ReadAllText(path));

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Catalog output path cannot be empty.", nameof(path));
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(path, ToJson());
    }

    private static void ValidateProvenance(SectionProfileDefinitionM80 definition)
    {
        if (definition.DataStatus != "Illustrative" && definition.DataStatus != "SourceDocumented")
            throw new ArgumentException($"Invalid dataStatus '{definition.DataStatus}' for '{definition.Id}'.");
        if (definition.DataStatus == "SourceDocumented" &&
            (string.IsNullOrWhiteSpace(definition.SourceDocument) || string.IsNullOrWhiteSpace(definition.SourceEdition)))
            throw new ArgumentException($"SourceDocumented profile '{definition.Id}' requires sourceDocument and sourceEdition.");
    }

    private static SectionGeometricPropertiesM80 Resolve(SectionProfileDefinitionM80 profile)
    {
        static double Required(double? value, string field, string id)
        {
            if (value is null)
                throw new ArgumentException($"Profile '{id}' requires {field}.");
            if (!double.IsFinite(value.Value) || value <= 0.0)
                throw new ArgumentOutOfRangeException(field, value, $"Profile '{id}': dimension must be finite and positive.");
            return StructuralUnitConversions.MillimetersToMeters(value.Value);
        }

        double W() => Required(profile.WidthMm, "widthMm", profile.Id);
        double H() => Required(profile.HeightMm, "heightMm", profile.Id);
        double T() => Required(profile.WallThicknessMm, "wallThicknessMm", profile.Id);
        double OD() => Required(profile.OuterDiameterMm, "outerDiameterMm", profile.Id);
        double ID() => Required(profile.InnerDiameterMm, "innerDiameterMm", profile.Id);
        return profile.Shape switch
        {
            "Rectangular" => SectionGeometryFactoryM80.Rectangular(profile.Id, W(), H(), profile.Name),
            "TimberRectangular" => SectionGeometryFactoryM80.TimberRectangular(profile.Id, W(), H(), profile.Name),
            "RectangularHollow" => SectionGeometryFactoryM80.RectangularHollow(profile.Id, W(), H(), T(), profile.Name),
            "DoublySymmetricI" => SectionGeometryFactoryM80.DoublySymmetricI(profile.Id, W(), H(),
                Required(profile.WebThicknessMm, "webThicknessMm", profile.Id),
                Required(profile.FlangeThicknessMm, "flangeThicknessMm", profile.Id), profile.Name),
            "CircularSolid" => SectionGeometryFactoryM80.CircularSolid(profile.Id, OD(), profile.Name),
            "CircularHollow" => SectionGeometryFactoryM80.CircularHollow(profile.Id, OD(), ID(), profile.Name),
            _ => throw new NotSupportedException($"Profile '{profile.Id}': unsupported shape '{profile.Shape}'."),
        };
    }

    private sealed record CatalogDocument(int SchemaVersion, string Title, IReadOnlyList<SectionProfileDefinitionM80> Profiles);
    private sealed record CatalogInputDocument(int SchemaVersion, string? Title, IReadOnlyList<SectionProfileDefinitionM80?>? Profiles);
}
