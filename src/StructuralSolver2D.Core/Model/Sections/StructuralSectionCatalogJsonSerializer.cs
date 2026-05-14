namespace StructuralSolver2D.Core.Model.Sections;

using System.Text.Json;
using StructuralSolver2D.Core.Model;

/// <summary>
/// Reads and writes <see cref="StructuralSectionCatalog"/> instances using a small JSON document format.
/// </summary>
public static class StructuralSectionCatalogJsonSerializer
{
    private const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    /// <summary>
    /// Serializes a section catalog to JSON.
    /// </summary>
    /// <param name="catalog">Catalog to serialize.</param>
    /// <param name="title">Optional user-facing catalog title.</param>
    /// <returns>A JSON document string.</returns>
    public static string Serialize(StructuralSectionCatalog catalog, string? title = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var document = new SectionCatalogDocument(
            CurrentSchemaVersion,
            string.IsNullOrWhiteSpace(title) ? "Structural section catalog" : title,
            catalog.Sections.ToArray());

        return JsonSerializer.Serialize(document, JsonOptions);
    }

    /// <summary>
    /// Deserializes a section catalog from JSON.
    /// </summary>
    /// <param name="json">JSON document text.</param>
    /// <returns>The loaded section catalog.</returns>
    public static StructuralSectionCatalog Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("The section catalog JSON cannot be empty.", nameof(json));
        }

        SectionCatalogDocument? document = JsonSerializer.Deserialize<SectionCatalogDocument>(json, JsonOptions);
        if (document is null)
        {
            throw new InvalidOperationException("The section catalog JSON could not be read.");
        }

        if (document.SchemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Unsupported section catalog schema version '{document.SchemaVersion}'.");
        }

        if (document.Sections is null)
        {
            throw new InvalidOperationException("The section catalog JSON does not contain a sections array.");
        }

        return new StructuralSectionCatalog(document.Sections);
    }

    /// <summary>
    /// Saves a section catalog to a JSON file, creating the output directory when necessary.
    /// </summary>
    public static void Save(StructuralSectionCatalog catalog, string path, string? title = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The output path cannot be empty.", nameof(path));
        }

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(catalog, title));
    }

    /// <summary>
    /// Loads a section catalog from a JSON file.
    /// </summary>
    public static StructuralSectionCatalog Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("The input path cannot be empty.", nameof(path));
        }

        return Deserialize(File.ReadAllText(path));
    }

    private sealed record SectionCatalogDocument(
        int SchemaVersion,
        string Title,
        IReadOnlyList<StructuralSection> Sections);
}
