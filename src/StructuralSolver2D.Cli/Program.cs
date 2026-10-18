using System.Text.Json;
using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Cli.Examples;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Cli.Output;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Csv;
using StructuralSolver2D.Reporting.Markdown;
using StructuralSolver2D.Reporting.Pdf;
using StructuralSolver2D.Reporting.Visualization;
using StructuralSolver2D.Reporting.Xlsx;

const string ApplicationName = "StructuralSolver2D";

if (args.Length == 0)
{
    CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
    return 0;
}

try
{
    if (!ValidateArguments(args)) return 1;

    if (IsHelpCommand(args[0]))
    {
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 0;
    }

    if (IsExampleCommand(args[0]))
    {
        return RunBuiltInExample(args);
    }

    if (IsAnalyzeCommand(args[0]))
    {
        return RunJsonAnalysis(args);
    }

    if (IsReportCommand(args[0]))
    {
        return RunMarkdownReport(args);
    }

    if (IsCsvExportCommand(args[0]))
    {
        return RunCsvExport(args);
    }

    if (IsXlsxExportCommand(args[0]))
    {
        return RunXlsxExport(args);
    }

    if (IsPdfExportCommand(args[0]))
    {
        return RunPdfExport(args);
    }

    if (IsSvgExportCommand(args[0]))
    {
        return RunSvgExport(args);
    }

    if (IsHtmlExportCommand(args[0]))
    {
        return RunHtmlExport(args);
    }

    if (IsViewerExportCommand(args[0]))
    {
        return RunViewerExport(args);
    }

    CliOutputWriter.WriteError($"Unknown command '{args[0]}'.");
    CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
    return 1;
}
catch (FileNotFoundException exception)
{
    CliOutputWriter.WriteError(exception.Message);
    return 1;
}
catch (KeyNotFoundException exception)
{
    CliOutputWriter.WriteError(exception.Message);
    CliOutputWriter.WriteAvailableExamples(CliExampleModelFactory.GetAvailableExamples());
    return 1;
}
catch (StructuralAnalysisException exception)
{
    CliOutputWriter.WriteError(exception.Message);
    if (exception.ValidationIssues.Count > 0)
    {
        CliOutputWriter.WriteValidationIssues(exception.ValidationIssues);
    }

    return 2;
}
catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
{
    CliOutputWriter.WriteError(exception.Message);
    return 1;
}
catch (Exception exception)
{
    CliOutputWriter.WriteError(exception.Message);
    return 3;
}

int RunBuiltInExample(string[] args)
{
    CliExample example = CliExampleModelFactory.Create(args[1]);
    AnalyzeAndWrite(
        sourceLabel: $"Example: {example.Name}",
        title: example.Title,
        description: example.Description,
        model: example.Model,
        loadCaseId: example.LoadCaseId);

    return 0;
}

int RunJsonAnalysis(string[] args)
{
    StructuralModelJsonFile input = StructuralModelJsonReader.Read(args[1]);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(2));

    AnalyzeAndWrite(
        sourceLabel: $"Input file: {args[1]}",
        title: input.Title,
        description: input.Description,
        model: input.Model,
        loadCaseId: loadCaseId);

    return 0;
}

int RunMarkdownReport(string[] args)
{
    string inputPath = args[1];
    string outputPath = args[2];
    EnsureDifferentPaths(inputPath, outputPath);

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(3));

    StructuralAnalysisOutput run = Analyze(input.Model, loadCaseId);

    var generator = new MarkdownStructuralReportGenerator();
    string markdown = generator.Generate(
        input.Model,
        run.Result,
        run.InternalForceDiagrams,
        run.DisplacementDiagrams,
        run.Summary,
        new MarkdownReportOptions
        {
            Title = input.Title,
            Description = input.Description,
            SourceLabel = inputPath,
            IncludeInternalForceSamples = true,
            MaxSamplesPerMember = 21,
            IncludeDisplacementSamples = true,
            MaxDisplacementSamplesPerMember = 21,
        });

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    File.WriteAllText(outputPath, markdown);
    Console.WriteLine($"Report written to: {outputPath}");

    return 0;
}

int RunCsvExport(string[] args)
{
    string inputPath = args[1];
    string outputDirectory = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(3));

    StructuralAnalysisOutput run = Analyze(input.Model, loadCaseId);

    var exporter = new CsvStructuralResultExporter();
    var files = new Dictionary<string, string>
    {
        ["nodal-displacements.csv"] = exporter.ExportNodalDisplacements(run.Result),
        ["support-reactions.csv"] = exporter.ExportSupportReactions(run.Result),
        ["member-end-forces.csv"] = exporter.ExportMemberEndForces(run.Result),
        ["internal-force-samples.csv"] = exporter.ExportInternalForceSamples(run.Result.LoadCaseId, run.InternalForceDiagrams),
        ["displacement-samples.csv"] = exporter.ExportDisplacementSamples(run.Result.LoadCaseId, run.DisplacementDiagrams),
        ["summary.csv"] = exporter.ExportSummary(run.Summary)
    };
    foreach (string fileName in files.Keys) EnsureDifferentPaths(inputPath, Path.Combine(outputDirectory, fileName));
    Directory.CreateDirectory(outputDirectory);
    foreach (var file in files) WriteCsv(outputDirectory, file.Key, file.Value);

    Console.WriteLine($"CSV files written to: {outputDirectory}");

    return 0;
}

int RunXlsxExport(string[] args)
{
    string inputPath = args[1];
    string outputPath = args[2];
    EnsureDifferentPaths(inputPath, outputPath);

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(3));

    StructuralAnalysisOutput run = Analyze(input.Model, loadCaseId);

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    byte[] workbook = new XlsxStructuralResultExporter().Export(
        run.Result,
        run.InternalForceDiagrams,
        run.DisplacementDiagrams,
        run.Summary);

    File.WriteAllBytes(outputPath, workbook);
    Console.WriteLine($"XLSX report written to: {outputPath}");

    return 0;
}

int RunPdfExport(string[] args)
{
    string inputPath = args[1];
    string outputPath = args[2];
    EnsureDifferentPaths(inputPath, outputPath);

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(3));

    StructuralAnalysisOutput run = Analyze(input.Model, loadCaseId);

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    byte[] pdf = new PdfTechnicalReportExporter().Export(
        input.Model,
        run.Result,
        run.InternalForceDiagrams,
        run.DisplacementDiagrams,
        run.Summary,
        new PdfTechnicalReportOptions
        {
            Title = input.Title,
            Description = input.Description,
            SourceLabel = inputPath,
        });

    File.WriteAllBytes(outputPath, pdf);
    Console.WriteLine($"PDF technical report written to: {outputPath}");

    return 0;
}

int RunSvgExport(string[] args)
{
    string inputPath = args[1];
    string outputPath = args[2];
    EnsureDifferentPaths(inputPath, outputPath);

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(3));

    StructuralAnalysisOutput output = Analyze(input.Model, loadCaseId);
    StructuralVisualizationModel visualization = BuildVisualization(input.Model, output);

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    string svg = new SvgStructuralResultExporter().Export(
        visualization,
        new SvgExportOptions
        {
            Title = input.Title,
        });

    File.WriteAllText(outputPath, svg);
    Console.WriteLine($"SVG written to: {outputPath}");

    return 0;
}

int RunHtmlExport(string[] args)
{
    string inputPath = args[1];
    string outputPath = args[2];
    EnsureDifferentPaths(inputPath, outputPath);

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(3));

    StructuralAnalysisOutput output = Analyze(input.Model, loadCaseId);
    StructuralVisualizationModel visualization = BuildVisualization(input.Model, output);

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    string html = new HtmlStructuralResultPreviewExporter().Export(
        visualization,
        new HtmlPreviewExportOptions
        {
            Title = input.Title,
            Description = input.Description,
            SvgOptions = new SvgExportOptions
            {
                Title = input.Title,
            },
        });

    File.WriteAllText(outputPath, html);
    Console.WriteLine($"HTML preview written to: {outputPath}");

    return 0;
}

int RunViewerExport(string[] args)
{
    string inputPath = args[1];
    string outputPath = args[2];
    EnsureDifferentPaths(inputPath, outputPath);

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = ResolveTargetId(input, args.ElementAtOrDefault(3));

    StructuralAnalysisOutput output = Analyze(input.Model, loadCaseId);
    StructuralVisualizationModel visualization = BuildVisualization(input.Model, output);

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    string html = new InteractiveHtmlStructuralViewerExporter().Export(
        visualization,
        new InteractiveViewerExportOptions
        {
            Title = input.Title,
            Description = input.Description,
            SvgOptions = new SvgExportOptions
            {
                Title = input.Title,
                Width = 1400.0,
                Height = 900.0,
            },
        });

    File.WriteAllText(outputPath, html);
    Console.WriteLine($"Interactive viewer written to: {outputPath}");

    return 0;
}

StructuralVisualizationModel BuildVisualization(StructuralModel model, StructuralAnalysisOutput output)
{
    return new StructuralVisualizationModelBuilder().Build(
        model,
        output,
        new VisualizationOptions
        {
            DeformationScale = 100.0,
            NormalForceDiagramScale = 0.02,
            ShearForceDiagramScale = 0.02,
            BendingMomentDiagramScale = 0.02,
            BoundsPadding = 0.5,
        });
}

StructuralAnalysisOutput Analyze(StructuralModel model, string loadCaseId)
{
    var service = new StructuralSolver2DService();
    bool isCombination = model.LoadCombinations.Any(
        combination => string.Equals(combination.Id, loadCaseId, StringComparison.OrdinalIgnoreCase));
    var options = new StructuralAnalysisOptions
    {
        IncludeDisplacementDiagrams = true,
        InternalForceSampleCount = 21,
        DisplacementSampleCount = 21
    };

    return isCombination
        ? service.AnalyzeLoadCombination(model, loadCaseId, options)
        : service.AnalyzeLoadCase(model, loadCaseId, options);
}

void WriteCsv(string outputDirectory, string fileName, string content)
{
    string path = Path.Combine(outputDirectory, fileName);
    File.WriteAllText(path, content);
}

void AnalyzeAndWrite(
    string sourceLabel,
    string title,
    string description,
    StructuralModel model,
    string loadCaseId)
{
    StructuralAnalysisOutput run = Analyze(model, loadCaseId);

    CliOutputWriter.WriteAnalysisResult(ApplicationName, sourceLabel, title, description, run.Result, run.Summary);
}

string ResolveTargetId(StructuralModelJsonFile input, string? explicitTargetId)
{
    if (explicitTargetId is not null) return explicitTargetId;
    if (input.DefaultTargetIsCombination && !input.Model.LoadCombinations.Any(combination =>
        string.Equals(combination.Id, input.LoadCaseId, StringComparison.OrdinalIgnoreCase)))
        throw new StructuralAnalysisException($"Default load combination '{input.LoadCaseId}' was not found in the structural model.");
    return input.LoadCaseId;
}

void EnsureDifferentPaths(string inputPath, string outputPath)
{
    StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    if (string.Equals(Path.GetFullPath(inputPath), Path.GetFullPath(outputPath), comparison))
        throw new ArgumentException("The output path must be different from the input model path.");
}

bool ValidateArguments(string[] arguments)
{
    string command = arguments[0];
    (int Minimum, int Maximum)? arity = command switch
    {
        _ when IsHelpCommand(command) => (1, 1),
        _ when IsExampleCommand(command) => (2, 2),
        _ when IsAnalyzeCommand(command) => (2, 3),
        _ when IsReportCommand(command) || IsCsvExportCommand(command) || IsXlsxExportCommand(command) ||
            IsPdfExportCommand(command) || IsSvgExportCommand(command) || IsHtmlExportCommand(command) || IsViewerExportCommand(command) => (3, 4),
        _ => null
    };
    if (arity is null) return true; // The dispatcher reports unknown commands.
    if (arguments.Length >= arity.Value.Minimum && arguments.Length <= arity.Value.Maximum) return true;
    CliOutputWriter.WriteError(arguments.Length < arity.Value.Minimum
        ? $"Missing arguments for command '{command}'."
        : $"Unexpected argument(s) for command '{command}': {string.Join(" ", arguments.Skip(arity.Value.Maximum))}");
    CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
    return false;
}

static bool IsHelpCommand(string command) =>
    string.Equals(command, "help", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "--help", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "-h", StringComparison.OrdinalIgnoreCase);

static bool IsExampleCommand(string command) =>
    string.Equals(command, "example", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "examples", StringComparison.OrdinalIgnoreCase);

static bool IsAnalyzeCommand(string command) =>
    string.Equals(command, "analyze", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "analyse", StringComparison.OrdinalIgnoreCase);

static bool IsReportCommand(string command) =>
    string.Equals(command, "report", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "markdown", StringComparison.OrdinalIgnoreCase);

static bool IsCsvExportCommand(string command) =>
    string.Equals(command, "export-csv", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "csv", StringComparison.OrdinalIgnoreCase);

static bool IsXlsxExportCommand(string command) =>
    string.Equals(command, "export-xlsx", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "xlsx", StringComparison.OrdinalIgnoreCase);

static bool IsPdfExportCommand(string command) =>
    string.Equals(command, "export-pdf", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "pdf", StringComparison.OrdinalIgnoreCase);

static bool IsSvgExportCommand(string command) =>
    string.Equals(command, "export-svg", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "svg", StringComparison.OrdinalIgnoreCase);

static bool IsHtmlExportCommand(string command) =>
    string.Equals(command, "export-html", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "html", StringComparison.OrdinalIgnoreCase);

static bool IsViewerExportCommand(string command) =>
    string.Equals(command, "export-viewer", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "viewer", StringComparison.OrdinalIgnoreCase);
