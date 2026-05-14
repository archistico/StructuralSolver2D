using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Cli.Examples;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Cli.Output;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Reporting.Markdown;

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

internal sealed record AnalysisRun(
    StructuralAnalysisResult Result,
    IReadOnlyList<MemberInternalForceDiagram> Diagrams,
    IReadOnlyList<MemberDisplacementDiagram> DisplacementDiagrams,
    StructuralAnalysisSummary Summary);
