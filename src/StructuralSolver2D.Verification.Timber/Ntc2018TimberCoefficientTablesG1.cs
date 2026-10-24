using System.Text.Json;
using System.Text.Json.Serialization;
using StructuralSolver2D.Catalogs;

namespace StructuralSolver2D.Verification.Timber;

/// <summary>NTC 2018 timber tables, independent of the EC5-recommended M92-B data.</summary>
public sealed record Ntc2018KmodRowG1(StrengthMaterialKindM88C Kind, int ServiceClass,
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

public sealed record Ntc2018GammaRowG1(StrengthMaterialKindM88C Kind,
    double FundamentalColumnA, double FundamentalColumnB);

/// <summary>
/// Exact limited subset of NTC 2018, Tables 4.4.III and 4.4.IV. Rows are checked against
/// independently coded reference values; a modified JSON must not silently change the code basis.
/// No verification of materials supplied to a real construction is performed.
/// </summary>
public sealed class Ntc2018TimberCoefficientTablesG1
{
    public const string DocumentId = "DM 17 gennaio 2018";
    public const string EditionId = "NTC2018";
    public const string AnnexId = "IT-NTC2018-NATIONAL-CODE";
    public const string KmodClause = "NTC 2018, 4.4.6, Tabella 4.4.IV";
    public const string GammaClause = "NTC 2018, 4.4.6, Tabella 4.4.III";
    public const string CrackClause = "Circolare 21 gennaio 2019 n.7, C4.4.8.1.9";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly IReadOnlyList<Ntc2018KmodRowG1> kmodRows;
    private readonly IReadOnlyList<Ntc2018GammaRowG1> gammaRows;

    public IReadOnlyList<Ntc2018KmodRowG1> KmodRows => kmodRows;
    public IReadOnlyList<Ntc2018GammaRowG1> GammaRows => gammaRows;
    public int SchemaVersion { get; }
    public string SourceDescription { get; }

    private Ntc2018TimberCoefficientTablesG1(Document doc)
    {
        if (doc.SchemaVersion != 1 || doc.DocumentId != DocumentId || doc.EditionId != EditionId ||
            doc.KmodSource != KmodClause || doc.GammaSource != GammaClause ||
            doc.CrackSource != CrackClause || string.IsNullOrWhiteSpace(doc.SourceDescription) ||
            doc.KmodRows is null || doc.GammaRows is null || doc.KmodRows.Count != 6 || doc.GammaRows.Count != 2)
            throw new ArgumentException("NTC 2018 timber reference tables have incomplete or incompatible provenance.");
        var k = doc.KmodRows.ToArray();
        var g = doc.GammaRows.ToArray();
        foreach (var kind in new[] { StrengthMaterialKindM88C.SolidSoftwood, StrengthMaterialKindM88C.HomogeneousGlulam })
        {
            foreach (var service in new[] { 1, 2, 3 })
            {
                var matches = k.Where(r => r is not null && r.Kind == kind && r.ServiceClass == service).ToArray();
                if (matches.Length != 1) throw new ArgumentException("Duplicate or missing NTC kmod row.");
                var expected = service == 3
                    ? new[] { 0.50, 0.55, 0.65, 0.70, 0.90 }
                    : new[] { 0.60, 0.70, 0.80, 0.90, 1.10 };
                var actual = new[] { matches[0].Permanent, matches[0].LongTerm, matches[0].MediumTerm,
                    matches[0].ShortTerm, matches[0].Instantaneous };
                if (!actual.SequenceEqual(expected))
                    throw new ArgumentException("kmod differs from NTC 2018 Tab. 4.4.IV (solid/glulam). ");
            }
            var gamma = g.Where(r => r is not null && r.Kind == kind).ToArray();
            if (gamma.Length != 1) throw new ArgumentException("Duplicate or missing NTC gamma_M row.");
            var expectedA = kind == StrengthMaterialKindM88C.SolidSoftwood ? 1.50 : 1.45;
            var expectedB = kind == StrengthMaterialKindM88C.SolidSoftwood ? 1.45 : 1.35;
            if (gamma[0].FundamentalColumnA != expectedA || gamma[0].FundamentalColumnB != expectedB)
                throw new ArgumentException("gamma_M differs from NTC 2018 Tab. 4.4.III.");
        }
        SchemaVersion = doc.SchemaVersion;
        SourceDescription = doc.SourceDescription;
        kmodRows = Array.AsReadOnly(k);
        gammaRows = Array.AsReadOnly(g);
    }

    public static Ntc2018TimberCoefficientTablesG1 FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("NTC reference JSON required.", nameof(json));
        var data = JsonSerializer.Deserialize<Document>(json, Options)
            ?? throw new ArgumentException("Null NTC reference JSON.", nameof(json));
        return new Ntc2018TimberCoefficientTablesG1(data);
    }

    public static Ntc2018TimberCoefficientTablesG1 Load(string path) => FromJson(File.ReadAllText(path));

    public double Kmod(StrengthMaterialKindM88C kind, int service, TimberLoadDurationM92A duration)
    {
        if (!Enum.IsDefined(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
        var row = kmodRows.SingleOrDefault(r => r.Kind == kind && r.ServiceClass == service)
            ?? throw new NotSupportedException("NTC G1 covers solid softwood/glulam in service classes 1,2,3 only.");
        return row.At(duration);
    }

    public double Gamma(StrengthMaterialKindM88C kind, Ntc2018TimberSituationG1 situation,
        Ntc2018GammaColumnG1 column)
    {
        if (!Enum.IsDefined(situation) || !Enum.IsDefined(column)) throw new ArgumentOutOfRangeException(nameof(situation));
        var row = gammaRows.SingleOrDefault(r => r.Kind == kind)
            ?? throw new NotSupportedException("No NTC timber gamma_M for this material family.");
        if (situation == Ntc2018TimberSituationG1.Exceptional)
        {
            if (column != Ntc2018GammaColumnG1.NotApplicableExceptional)
                throw new ArgumentException("Exceptional NTC gamma_M=1.0 is not a Column A/B selection.");
            return 1.0;
        }
        return column switch
        {
            Ntc2018GammaColumnG1.A => row.FundamentalColumnA,
            Ntc2018GammaColumnG1.B => row.FundamentalColumnB,
            _ => throw new ArgumentException("NTC fundamental combinations require explicit Column A or B.")
        };
    }

    private sealed record Document(int SchemaVersion, string DocumentId, string EditionId,
        string SourceDescription, string KmodSource, string GammaSource, string CrackSource,
        IReadOnlyList<Ntc2018KmodRowG1>? KmodRows,
        IReadOnlyList<Ntc2018GammaRowG1>? GammaRows);
}
