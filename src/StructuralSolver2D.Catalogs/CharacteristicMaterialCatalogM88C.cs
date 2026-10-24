using System.Text.Json;
using System.Text.Json.Serialization;

namespace StructuralSolver2D.Catalogs;

/// <summary>M88-C contains characteristic product properties, NOT design resistances.</summary>
public enum StrengthMaterialKindM88C { Unknown = 0, RolledSteel = 1, SolidSoftwood = 2, HomogeneousGlulam = 3 }

/// <summary>Minimum characteristic/nominal steel yield in MPa on (LowerExclusiveMm, UpperInclusiveMm].</summary>
public sealed record SteelYieldBandM88C(double LowerExclusiveMm, double UpperInclusiveMm, double YieldStrengthMpa);

/// <summary>Strength-class data; no product certificate, partial safety factor or design resistance.</summary>
public sealed record SteelStrengthClassM88C(
    string Id, string ReferenceStandard, string SourceUrl, string DataStatus,
    IReadOnlyList<SteelYieldBandM88C> YieldByNominalThickness);

/// <summary>EN 338 / EN 14080 characteristics: strengths and stiffness in MPa, density in kg/m³.</summary>
public sealed record TimberStrengthClassM88C(
    string Id, StrengthMaterialKindM88C Kind,
    double BendingStrengthMpa, double TensionParallelMpa, double TensionPerpendicularMpa,
    double CompressionParallelMpa, double CompressionPerpendicularMpa, double ShearStrengthMpa,
    double ElasticModulusParallelMeanMpa, double ElasticModulusParallel05Mpa,
    double ShearModulusMeanMpa, double DensityCharacteristicKgM3, double DensityMeanKgM3,
    string ReferenceStandard, string SourceUrl, string DataStatus);

/// <summary>
/// Read-only characteristic properties with explicit references and restricted scope.
/// Does not select a normative edition/annex or establish any EC3/EC5/NTC pass.
/// Unsupported steel thicknesses fail closed: no extrapolation or nominal-grade fallback.
/// </summary>
public sealed class CharacteristicMaterialCatalogM88C
{
    private const int CurrentSchemaVersion = 1;
    private const string ReferenceOnly = "ReferenceNotCertified";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IReadOnlyList<SteelStrengthClassM88C> steel;
    private readonly IReadOnlyList<TimberStrengthClassM88C> timber;
    public string Title { get; }
    public IReadOnlyList<SteelStrengthClassM88C> Steel => steel;
    public IReadOnlyList<TimberStrengthClassM88C> Timber => timber;
    public bool IncludesDesignStrengthsOrPartialFactors => false;
    public bool ConfersCodeCompliance => false;

    public CharacteristicMaterialCatalogM88C(string title,
        IEnumerable<SteelStrengthClassM88C> steel,
        IEnumerable<TimberStrengthClassM88C> timber)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A catalog title is required.", nameof(title));
        ArgumentNullException.ThrowIfNull(steel);
        ArgumentNullException.ThrowIfNull(timber);
        Title = title;
        var steels = steel.ToArray();
        var timbers = timber.ToArray();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in steels)
        {
            if (s is null) throw new ArgumentException("Null steel class.", nameof(steel));
            RequireId(s.Id, names);
            RequireReference(s.ReferenceStandard, s.SourceUrl, s.DataStatus);
            if (!string.Equals(s.ReferenceStandard, "EN 10025-2:2019", StringComparison.Ordinal))
                throw new ArgumentException("Unsupported steel product standard edition.");
            if (s.YieldByNominalThickness is null || s.YieldByNominalThickness.Count == 0)
                throw new ArgumentException("Steel thickness bands are mandatory.");
            double previousUpper = 0;
            foreach (var band in s.YieldByNominalThickness)
            {
                if (band is null || !double.IsFinite(band.LowerExclusiveMm) ||
                    !double.IsFinite(band.UpperInclusiveMm) ||
                    band.LowerExclusiveMm != previousUpper ||
                    band.UpperInclusiveMm <= band.LowerExclusiveMm)
                    throw new ArgumentException($"Non-contiguous or invalid thickness range for {s.Id}.");
                Positive(band.YieldStrengthMpa, "yield strength");
                previousUpper = band.UpperInclusiveMm;
            }
        }
        foreach (var t in timbers)
        {
            if (t is null) throw new ArgumentException("Null timber class.", nameof(timber));
            RequireId(t.Id, names);
            RequireReference(t.ReferenceStandard, t.SourceUrl, t.DataStatus);
            string standard = t.Kind switch
            {
                StrengthMaterialKindM88C.SolidSoftwood => "EN 338:2016",
                StrengthMaterialKindM88C.HomogeneousGlulam => "EN 14080:2013",
                _ => throw new ArgumentException("Unknown or unsupported timber class type.")
            };
            if (t.ReferenceStandard != standard)
                throw new ArgumentException("Timber family and material standard do not agree.");
            if (t.Kind == StrengthMaterialKindM88C.SolidSoftwood && !t.Id.StartsWith('C') ||
                t.Kind == StrengthMaterialKindM88C.HomogeneousGlulam && !t.Id.EndsWith('h'))
                throw new ArgumentException("Invalid timber strength-class name for its family.");
            Positive(t.BendingStrengthMpa, nameof(t.BendingStrengthMpa));
            Positive(t.TensionParallelMpa, nameof(t.TensionParallelMpa));
            Positive(t.TensionPerpendicularMpa, nameof(t.TensionPerpendicularMpa));
            Positive(t.CompressionParallelMpa, nameof(t.CompressionParallelMpa));
            Positive(t.CompressionPerpendicularMpa, nameof(t.CompressionPerpendicularMpa));
            Positive(t.ShearStrengthMpa, nameof(t.ShearStrengthMpa));
            Positive(t.ElasticModulusParallelMeanMpa, nameof(t.ElasticModulusParallelMeanMpa));
            Positive(t.ElasticModulusParallel05Mpa, nameof(t.ElasticModulusParallel05Mpa));
            Positive(t.ShearModulusMeanMpa, nameof(t.ShearModulusMeanMpa));
            Positive(t.DensityCharacteristicKgM3, nameof(t.DensityCharacteristicKgM3));
            Positive(t.DensityMeanKgM3, nameof(t.DensityMeanKgM3));
            if (t.ElasticModulusParallel05Mpa > t.ElasticModulusParallelMeanMpa ||
                t.DensityCharacteristicKgM3 > t.DensityMeanKgM3)
                throw new ArgumentException("Characteristic/mean stiffness or density is inverted.");
        }
        this.steel = Array.AsReadOnly(steels.Select(s => s with
        {
            YieldByNominalThickness = Array.AsReadOnly(s.YieldByNominalThickness.ToArray())
        }).ToArray());
        this.timber = Array.AsReadOnly(timbers);
    }

    public SteelStrengthClassM88C FindSteel(string id) =>
        steel.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown steel grade '{id}'.");

    public TimberStrengthClassM88C FindTimber(string id) =>
        timber.FirstOrDefault(t => string.Equals(t.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? throw new KeyNotFoundException($"Unknown timber class '{id}'.");

    /// <summary>Returns the table minimum in MPa, never a design stress or resistance.</summary>
    public double GetSteelYieldMpa(string id, double nominalThicknessMm)
    {
        if (!double.IsFinite(nominalThicknessMm) || nominalThicknessMm <= 0)
            throw new ArgumentOutOfRangeException(nameof(nominalThicknessMm));
        var grade = FindSteel(id);
        var band = grade.YieldByNominalThickness.FirstOrDefault(b =>
            nominalThicknessMm > b.LowerExclusiveMm && nominalThicknessMm <= b.UpperInclusiveMm);
        return band?.YieldStrengthMpa ?? throw new NotSupportedException(
            $"No validated yield-strength band for {id} at {nominalThicknessMm} mm. Do not extrapolate.");
    }

    public string ToJson() => JsonSerializer.Serialize(new CatalogDocument(
        CurrentSchemaVersion, Title, steel, timber), JsonOptions);

    public static CharacteristicMaterialCatalogM88C FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Catalog JSON is required.", nameof(json));
        var doc = JsonSerializer.Deserialize<CatalogDocument>(json, JsonOptions)
            ?? throw new InvalidOperationException("Missing material catalog.");
        if (doc.SchemaVersion != CurrentSchemaVersion)
            throw new NotSupportedException($"Unsupported characteristic catalog schema {doc.SchemaVersion}.");
        if (doc.Steel is null || doc.Timber is null)
            throw new InvalidOperationException("Both steel and timber arrays are mandatory.");
        return new CharacteristicMaterialCatalogM88C(doc.Title, doc.Steel, doc.Timber);
    }

    public static CharacteristicMaterialCatalogM88C Load(string path) => FromJson(File.ReadAllText(path));

    public void Save(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path required.", nameof(path));
        var folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(path, ToJson());
    }

    private static void RequireId(string id, HashSet<string> names)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Material id is required.");
        if (!names.Add(id)) throw new ArgumentException($"Duplicated material id '{id}'.");
    }
    private static void RequireReference(string standard, string source, string status)
    {
        if (string.IsNullOrWhiteSpace(standard) ||
            !Uri.TryCreate(source, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            status != ReferenceOnly)
            throw new ArgumentException("Explicit standard, HTTPS source and reference-only status are mandatory.");
    }
    private static void Positive(double value, string field)
    {
        if (!double.IsFinite(value) || value <= 0)
            throw new ArgumentOutOfRangeException(field, value, "Property must be finite and positive.");
    }

    private sealed record CatalogDocument(int SchemaVersion, string Title,
        IReadOnlyList<SteelStrengthClassM88C>? Steel, IReadOnlyList<TimberStrengthClassM88C>? Timber);
}
