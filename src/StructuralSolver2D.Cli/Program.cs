using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Cli.Examples;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Cli.Output;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Csv;
using StructuralSolver2D.Reporting.Markdown;
using StructuralSolver2D.Reporting.Pdf;
using StructuralSolver2D.Reporting.Visualization;
using StructuralSolver2D.Reporting.Xlsx;
using StructuralSolver2D.Analysis.PublicApi;

const string ApplicationName = "StructuralSolver2D";

if (args.Length == 0 || IsHelpCommand(args[0]))
{
    CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
    return 0;
}

try
{
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
catch (Exception exception)
{
    CliOutputWriter.WriteError(exception.Message);
    return 3;
}

int RunBuiltInExample(string[] args)
{
    if (args.Length < 2)
    {
        CliOutputWriter.WriteError("Missing example name.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

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
    if (args.Length < 2)
    {
        CliOutputWriter.WriteError("Missing JSON input file path.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(args[1]);
    string loadCaseId = args.Length >= 3 ? args[2] : input.LoadCaseId;

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
    if (args.Length < 3)
    {
        CliOutputWriter.WriteError("Missing report arguments.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    string inputPath = args[1];
    string outputPath = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = args.Length >= 4 ? args[3] : input.LoadCaseId;

    AnalysisRun run = Analyze(input.Model, loadCaseId);

    var generator = new MarkdownStructuralReportGenerator();
    string markdown = generator.Generate(
        input.Model,
        run.Result,
        run.Diagrams,
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
    if (args.Length < 3)
    {
        CliOutputWriter.WriteError("Missing CSV export arguments.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    string inputPath = args[1];
    string outputDirectory = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = args.Length >= 4 ? args[3] : input.LoadCaseId;

    AnalysisRun run = Analyze(input.Model, loadCaseId);

    Directory.CreateDirectory(outputDirectory);

    var exporter = new CsvStructuralResultExporter();
    WriteCsv(outputDirectory, "nodal-displacements.csv", exporter.ExportNodalDisplacements(run.Result));
    WriteCsv(outputDirectory, "support-reactions.csv", exporter.ExportSupportReactions(run.Result));
    WriteCsv(outputDirectory, "member-end-forces.csv", exporter.ExportMemberEndForces(run.Result));
    WriteCsv(outputDirectory, "internal-force-samples.csv", exporter.ExportInternalForceSamples(run.Result.LoadCaseId, run.Diagrams));
    WriteCsv(outputDirectory, "displacement-samples.csv", exporter.ExportDisplacementSamples(run.Result.LoadCaseId, run.DisplacementDiagrams));
    WriteCsv(outputDirectory, "summary.csv", exporter.ExportSummary(run.Summary));

    Console.WriteLine($"CSV files written to: {outputDirectory}");

    return 0;
}

int RunXlsxExport(string[] args)
{
    if (args.Length < 3)
    {
        CliOutputWriter.WriteError("Missing XLSX export arguments.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    string inputPath = args[1];
    string outputPath = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = args.Length >= 4 ? args[3] : input.LoadCaseId;

    AnalysisRun run = Analyze(input.Model, loadCaseId);

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    byte[] workbook = new XlsxStructuralResultExporter().Export(
        run.Result,
        run.Diagrams,
        run.DisplacementDiagrams,
        run.Summary);

    File.WriteAllBytes(outputPath, workbook);
    Console.WriteLine($"XLSX report written to: {outputPath}");

    return 0;
}

int RunPdfExport(string[] args)
{
    if (args.Length < 3)
    {
        CliOutputWriter.WriteError("Missing PDF export arguments.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    string inputPath = args[1];
    string outputPath = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = args.Length >= 4 ? args[3] : input.LoadCaseId;

    AnalysisRun run = Analyze(input.Model, loadCaseId);

    string? directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
    {
        Directory.CreateDirectory(directory);
    }

    byte[] pdf = new PdfTechnicalReportExporter().Export(
        input.Model,
        run.Result,
        run.Diagrams,
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
    if (args.Length < 3)
    {
        CliOutputWriter.WriteError("Missing SVG export arguments.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    string inputPath = args[1];
    string outputPath = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = args.Length >= 4 ? args[3] : input.LoadCaseId;

    StructuralAnalysisOutput output = AnalyzeForVisualization(input.Model, loadCaseId);
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
    if (args.Length < 3)
    {
        CliOutputWriter.WriteError("Missing HTML export arguments.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    string inputPath = args[1];
    string outputPath = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = args.Length >= 4 ? args[3] : input.LoadCaseId;

    StructuralAnalysisOutput output = AnalyzeForVisualization(input.Model, loadCaseId);
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
    if (args.Length < 3)
    {
        CliOutputWriter.WriteError("Missing viewer export arguments.");
        CliOutputWriter.WriteHelp(ApplicationName, CliExampleModelFactory.GetAvailableExamples());
        return 1;
    }

    string inputPath = args[1];
    string outputPath = args[2];

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath);
    string loadCaseId = args.Length >= 4 ? args[3] : input.LoadCaseId;

    StructuralAnalysisOutput output = AnalyzeForVisualization(input.Model, loadCaseId);
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

StructuralAnalysisOutput AnalyzeForVisualization(StructuralModel model, string loadCaseId)
{
    var service = new StructuralSolver2DService();
    bool isCombination = model.LoadCombinations.Any(
        combination => string.Equals(combination.Id, loadCaseId, StringComparison.OrdinalIgnoreCase));

    return isCombination
        ? service.AnalyzeLoadCombination(
            model,
            loadCaseId,
            new StructuralAnalysisOptions
            {
                IncludeDisplacementDiagrams = true,
                InternalForceSampleCount = 21,
                DisplacementSampleCount = 21,
            })
        : service.AnalyzeLoadCase(
            model,
            loadCaseId,
            new StructuralAnalysisOptions
            {
                IncludeDisplacementDiagrams = true,
                InternalForceSampleCount = 21,
                DisplacementSampleCount = 21,
            });
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
    AnalysisRun run = Analyze(model, loadCaseId);

    CliOutputWriter.WriteAnalysisResult(ApplicationName, sourceLabel, title, description, run.Result, run.Summary);
}

AnalysisRun Analyze(StructuralModel model, string loadCaseId)
{
    StructuralAnalysisResult result;
    IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams;

    bool isTrussOnlyModel = model.Members.Count > 0 &&
        model.Members.All(member => member.Type == StructuralSolver2D.Core.Model.Enums.MemberType.Truss2D);
    bool isFrameOnlyModel = model.Members.Count > 0 &&
        model.Members.All(member => member.Type == StructuralSolver2D.Core.Model.Enums.MemberType.Frame2D);
    bool isCombination = model.LoadCombinations.Any(
        combination => string.Equals(combination.Id, loadCaseId, StringComparison.OrdinalIgnoreCase));

    if (isTrussOnlyModel)
    {
        var analyzer = new Truss2DAnalyzer();
        result = isCombination
            ? analyzer.AnalyzeCombination(model, loadCaseId)
            : analyzer.Analyze(model, loadCaseId);

        displacementDiagrams = Array.Empty<MemberDisplacementDiagram>();
    }
    else if (isFrameOnlyModel)
    {
        var analyzer = new Frame2DAnalyzer();
        result = isCombination
            ? analyzer.AnalyzeCombination(model, loadCaseId)
            : analyzer.Analyze(model, loadCaseId);

        displacementDiagrams = new Frame2DDisplacementSampler().SampleAllMembers(model, result, sampleCount: 21);
    }
    else
    {
        var analyzer = new PlaneStructureAnalyzer();
        result = isCombination
            ? analyzer.AnalyzeCombination(model, loadCaseId)
            : analyzer.Analyze(model, loadCaseId);

        var displacementSampler = new Frame2DDisplacementSampler();
        displacementDiagrams = model.Members
            .Where(member => member.Type == StructuralSolver2D.Core.Model.Enums.MemberType.Frame2D)
            .Select(member => displacementSampler.SampleMember(model, result, member.Id, sampleCount: 21))
            .ToList();
    }

    var sampler = new Frame2DInternalForceSampler();
    IReadOnlyList<MemberInternalForceDiagram> diagrams = sampler.SampleAllMembers(model, result, sampleCount: 21);

    var summarizer = new Frame2DResultSummarizer();
    StructuralAnalysisSummary summary = summarizer.Summarize(result, diagrams);

    return new AnalysisRun(result, diagrams, displacementDiagrams, summary);
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

internal sealed record AnalysisRun(
    StructuralAnalysisResult Result,
    IReadOnlyList<MemberInternalForceDiagram> Diagrams,
    IReadOnlyList<MemberDisplacementDiagram> DisplacementDiagrams,
    StructuralAnalysisSummary Summary);
