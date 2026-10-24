using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Catalogs;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>Versioned reference data. Recommended EC5 values are NOT the Italian National Annex.</summary>
public sealed record TimberKmodRowM92B(StrengthMaterialKindM88C Kind, int ServiceClass,
    double Permanent, double LongTerm, double MediumTerm, double ShortTerm, double Instantaneous)
{
    public double At(TimberLoadDurationM92A duration) => duration switch
    {
        TimberLoadDurationM92A.Permanent => Permanent,
        TimberLoadDurationM92A.LongTerm => LongTerm,
        TimberLoadDurationM92A.MediumTerm => MediumTerm,
        TimberLoadDurationM92A.ShortTerm => ShortTerm,
        TimberLoadDurationM92A.Instantaneous => Instantaneous,
        _ => throw new ArgumentOutOfRangeException(nameof(duration))
    };
}

public sealed record TimberGammaRowM92B(StrengthMaterialKindM88C Kind, double GammaM);

public sealed class TimberEc5CoefficientTablesM92B
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly IReadOnlyList<TimberKmodRowM92B> rows;
    private readonly IReadOnlyList<TimberGammaRowM92B> gammaRows;

    public int SchemaVersion { get; }
    public string Edition { get; }
    public string Basis { get; }
    public string KmodSource { get; }
    public string GammaSource { get; }
    public string SourceUrl { get; }
    public IReadOnlyList<TimberKmodRowM92B> KmodRows => rows;
    public IReadOnlyList<TimberGammaRowM92B> RecommendedGammaRows => gammaRows;

    private TimberEc5CoefficientTablesM92B(TableDocument doc)
    {
        if (doc.SchemaVersion != 1 ||
            doc.Edition != TimberRectangularSectionRuleM92A.Edition ||
            doc.Basis != "EN 1995-1-1 first-generation recommended values, not a national annex" ||
            string.IsNullOrWhiteSpace(doc.KmodSource) ||
            string.IsNullOrWhiteSpace(doc.GammaSource) ||
            !Uri.TryCreate(doc.SourceUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            doc.KmodRows is null || doc.RecommendedGammaRows is null)
            throw new ArgumentException("Missing, incompatible or untraceable first-generation EC5 table metadata.");
        var expectedKinds = new[] { StrengthMaterialKindM88C.SolidSoftwood, StrengthMaterialKindM88C.HomogeneousGlulam };
        var candidates = doc.KmodRows.ToArray();
        if (candidates.Length != 6 || doc.RecommendedGammaRows.Count != 2)
            throw new ArgumentException("Expected exactly 6 kmod and 2 recommended gamma_M rows.");
        foreach (var kind in expectedKinds)
        {
            foreach (int service in new[] { 1, 2, 3 })
            {
                var selected = candidates.Where(r => r is not null && r.Kind == kind && r.ServiceClass == service).ToArray();
                if (selected.Length != 1) throw new ArgumentException("Missing or duplicate material/service row.");
                var r = selected[0];
                var values = new[] { r.Permanent, r.LongTerm, r.MediumTerm, r.ShortTerm, r.Instantaneous };
                if (values.Any(v => !double.IsFinite(v) || v <= 0 || v > 2))
                    throw new ArgumentException("Invalid kmod table value.");
            }
            var g = doc.RecommendedGammaRows.Where(r => r is not null && r.Kind == kind).ToArray();
            if (g.Length != 1 || !double.IsFinite(g[0].GammaM) || g[0].GammaM <= 0)
                throw new ArgumentException("Missing, duplicate or invalid gamma_M row.");
        }
        rows = Array.AsReadOnly(candidates);
        gammaRows = Array.AsReadOnly(doc.RecommendedGammaRows.ToArray());
        SchemaVersion = doc.SchemaVersion;
        Edition = doc.Edition;
        Basis = doc.Basis;
        KmodSource = doc.KmodSource;
        GammaSource = doc.GammaSource;
        SourceUrl = doc.SourceUrl;
    }

    public static TimberEc5CoefficientTablesM92B FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("EC5 JSON table required.", nameof(json));
        var doc = JsonSerializer.Deserialize<TableDocument>(json, JsonOptions)
            ?? throw new ArgumentException("Null EC5 table document.", nameof(json));
        return new TimberEc5CoefficientTablesM92B(doc);
    }

    public static TimberEc5CoefficientTablesM92B Load(string path) => FromJson(File.ReadAllText(path));

    public double Kmod(StrengthMaterialKindM88C kind, int serviceClass, TimberLoadDurationM92A duration)
    {
        if (!Enum.IsDefined(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
        var row = rows.SingleOrDefault(r => r.Kind == kind && r.ServiceClass == serviceClass)
            ?? throw new NotSupportedException("Only solid softwood / homogeneous glulam and service classes 1..3 are covered.");
        return row.At(duration);
    }

    public double RecommendedGammaM(StrengthMaterialKindM88C kind) =>
        gammaRows.SingleOrDefault(r => r.Kind == kind)?.GammaM
        ?? throw new NotSupportedException("No recommended gamma_M for this material family.");

    private sealed record TableDocument(int SchemaVersion, string Edition, string Basis,
        string KmodSource, string GammaSource, string SourceUrl,
        IReadOnlyList<TimberKmodRowM92B>? KmodRows,
        IReadOnlyList<TimberGammaRowM92B>? RecommendedGammaRows);
}
