using StructuralSolver2D.Cli.Nonlinear;
using System.Text.Json;
using System.Globalization;
using StructuralSolver2D.Reporting.Modal;
using StructuralSolver2D.Reporting.Transient;
using StructuralSolver2D.Reporting.Seismic;
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

    if (NonlinearTruss2DCliM68.IsCommand(args[0]))
    {
        return NonlinearTruss2DCliM68.Run(args);
    }

    if (IsSeismicCommand(args[0]))
    {
        return RunSeismicCommand(args);
    }

    if (IsTimeConvergenceCommand(args[0]))
    {
        return RunTimeConvergenceCommand(args);
    }

    if (IsTimeCommand(args[0]))
    {
        return RunTimeCommand(args);
    }

    if (IsHarmonicCommand(args[0]))
    {
        return RunHarmonicCommand(args);
    }

    if (IsModalCommand(args[0]))
    {
        return RunModalCommand(args);
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

SeismicModalCombination ParseSeismicCombination(string? text)
{
    if (text is null) return SeismicModalCombination.Srss;
    if (Enum.TryParse<SeismicModalCombination>(text, true, out var combination) &&
        Enum.IsDefined(combination)) return combination;
    throw new ArgumentException("M70_COMBINATION: seismic.spectrum.combination must be 'Srss' or 'Cqc'.");
}

int RunSeismicCommand(string[] argv)
{
    string command = argv[0].ToLowerInvariant();
    string source = argv[1];
    StructuralModelJsonFile input = StructuralModelJsonReader.Read(source, allowModalWithoutLoadCase: true);
    if (input.Dynamics is null) throw new ArgumentException("M60_DYNAMICS_REQUIRED: Missing dynamics block.");
    using var document = JsonDocument.Parse(File.ReadAllText(source));
    if (!document.RootElement.TryGetProperty("seismic", out JsonElement block) ||
        block.ValueKind != JsonValueKind.Object)
        throw new ArgumentException("M60_SEISMIC_REQUIRED: Missing seismic block.");
    var options = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };
    var json = block.Deserialize<SeismicInputJson>(options) ??
        throw new ArgumentException("M60_SEISMIC_REQUIRED: Invalid seismic block.");
    if (!Enum.TryParse<SeismicGroundDirection>(json.Direction, true, out var direction) ||
        !Enum.IsDefined(direction))
        throw new ArgumentException("M60_DIRECTION: seismic.direction must be GlobalX or GlobalY.");
    var exporter = new SeismicReportExporter();
    var solver = new StructuralSolver2DService();
    bool time = command.StartsWith("seismic-time", StringComparison.Ordinal);
    string content;
    string? secondCsv = null;
    if (time)
    {
        var transient = json.TimeHistory ?? throw new ArgumentException("M60_TIME_REQUIRED: Missing seismic.timeHistory.");
        var result = solver.AnalyzeSeismicTime(new StructuralSeismicTimeRequest(input.Model, input.Dynamics,
            direction, transient.TimeStepSeconds ?? throw new ArgumentException("M60_DT_REQUIRED"),
            transient.StepCount ?? throw new ArgumentException("M60_STEP_COUNT_REQUIRED"),
            transient.Damping ?? throw new ArgumentException("M60_DAMPING_REQUIRED"),
            transient.GroundAcceleration ?? throw new ArgumentException("M60_GROUND_REQUIRED")));
        content = command == "seismic-time-csv" ? exporter.ExportTimeCsv(result) :
            exporter.ExportTimeMarkdown(result, input.Title);
    }
    else
    {
        var spectrum = json.Spectrum ?? throw new ArgumentException("M60_SPECTRUM_REQUIRED: Missing seismic.spectrum.");
        var result = solver.AnalyzeSeismicSpectrum(new StructuralSeismicSpectrumRequest(input.Model,
            input.Dynamics, direction, spectrum.Points ?? throw new ArgumentException("M60_SPECTRUM_POINTS_REQUIRED"),
            spectrum.ModeCount ?? 0,
            ParseSeismicCombination(spectrum.Combination), spectrum.ModalDampingRatio));
        content = command == "seismic-spectrum-csv" ? exporter.ExportSpectrumModesCsv(result) :
            exporter.ExportSpectrumMarkdown(result, input.Title);
        if (command == "seismic-spectrum-csv") secondCsv = exporter.ExportSpectrumNodesCsv(result);
    }
    if (command is "seismic-time" or "seismic-spectrum")
    {
        Console.WriteLine(content);
        return 0;
    }
    string target = argv[2];
    EnsureDifferentPaths(source, target);
    if (secondCsv is not null)
    {
        if (File.Exists(target)) throw new ArgumentException("M60_CSV_DIR: Destination must be a directory.");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "seismic-modes.csv"), content);
        File.WriteAllText(Path.Combine(target, "seismic-nodes.csv"), secondCsv);
    }
    else
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(target));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(target, content);
    }
    Console.WriteLine($"Seismic output written to: {target}");
    return 0;
}

int RunTimeConvergenceCommand(string[] argv)
{
    string source = argv[1];
    StructuralModelJsonFile input = StructuralModelJsonReader.Read(source, allowModalWithoutLoadCase: true);
    if (input.Dynamics is null)
        throw new ArgumentException("M591_DYNAMICS_REQUIRED: Missing dynamics block.");
    using var document = JsonDocument.Parse(File.ReadAllText(source));
    if (!document.RootElement.TryGetProperty("timeHistory", out JsonElement block) ||
        block.ValueKind != JsonValueKind.Object)
        throw new ArgumentException("M591_HISTORY_REQUIRED: Missing timeHistory block.");
    var options = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };
    TimeHistoryInputJson json = block.Deserialize<TimeHistoryInputJson>(options) ??
        throw new ArgumentException("M591_HISTORY_REQUIRED: Invalid timeHistory block.");
    if (json.TimeStepSeconds is null || json.StepCount is null)
        throw new ArgumentException("M591_GRID_REQUIRED: timeStepSeconds and stepCount are required.");
    var history = new StructuralTimeHistoryAnalysisRequest(input.Model, input.Dynamics,
        json.TimeStepSeconds.Value, json.StepCount.Value,
        json.Damping ?? throw new ArgumentException("M591_DAMPING_REQUIRED: A damping model is required."),
        json.Loads ?? throw new ArgumentException("M591_LOADS_REQUIRED: Explicit load patterns array required."),
        json.InitialStates);
    var convergence = new StructuralSolver2DService().AnalyzeTimeHistoryConvergence(
        new StructuralTimeConvergenceRequest(history));
    var report = new TimeConvergenceReportExporter();
    if (argv[0].Equals("time-convergence", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine(report.ExportMarkdown(convergence, input.Title));
        return 0;
    }
    string target = argv[2];
    EnsureDifferentPaths(source, target);
    string? parent = Path.GetDirectoryName(Path.GetFullPath(target));
    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
    File.WriteAllText(target, argv[0].Equals("time-convergence-csv", StringComparison.OrdinalIgnoreCase)
        ? report.ExportCsv(convergence) : report.ExportMarkdown(convergence, input.Title));
    Console.WriteLine($"Time-convergence output written to: {target}");
    return 0;
}

int RunTimeCommand(string[] argv)
{
    string command = argv[0].ToLowerInvariant();
    string source = argv[1];
    StructuralModelJsonFile input = StructuralModelJsonReader.Read(source, allowModalWithoutLoadCase: true);
    if (input.Dynamics is null) throw new ArgumentException("M59_DYNAMICS_REQUIRED: Missing dynamics block.");
    using var document = JsonDocument.Parse(File.ReadAllText(source));
    if (!document.RootElement.TryGetProperty("timeHistory", out JsonElement block) ||
        block.ValueKind != JsonValueKind.Object)
        throw new ArgumentException("M59_HISTORY_REQUIRED: Missing timeHistory input block.");
    var jsonOptions = new JsonSerializerOptions {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };
    var json = block.Deserialize<TimeHistoryInputJson>(jsonOptions) ??
        throw new ArgumentException("M59_HISTORY_REQUIRED: Invalid timeHistory block.");
    if (json.TimeStepSeconds is null || json.StepCount is null)
        throw new ArgumentException("M59_GRID_REQUIRED: timeStepSeconds and stepCount are mandatory.");
    var request = new StructuralTimeHistoryAnalysisRequest(input.Model, input.Dynamics,
        json.TimeStepSeconds.Value, json.StepCount.Value,
        json.Damping ?? throw new ArgumentException("M59_DAMPING_REQUIRED: timeHistory.damping is mandatory."),
        json.Loads ?? throw new ArgumentException("M59_LOADS_REQUIRED: timeHistory.loads must be explicitly set; an empty array is valid for free response."),
        json.InitialStates);
    if (command.StartsWith("time-forces", StringComparison.Ordinal))
    {
        StructuralDynamicForceRecoveryOutput forceResult = new StructuralSolver2DService()
            .AnalyzeTimeHistoryForces(request);
        var forceExporter = new DynamicForceReportExporter();
        if (command == "time-forces")
        {
            Console.WriteLine(forceExporter.ExportMarkdown(forceResult, input.Title));
            return 0;
        }
        string forceTarget = argv[2];
        EnsureDifferentPaths(source, forceTarget);
        string? forceParent = Path.GetDirectoryName(Path.GetFullPath(forceTarget));
        if (!string.IsNullOrEmpty(forceParent)) Directory.CreateDirectory(forceParent);
        if (command == "time-forces-csv")
        {
            if (File.Exists(forceTarget))
                throw new ArgumentException("M67_OUTPUT_PATH: CSV destination must be a directory.");
            Directory.CreateDirectory(forceTarget);
            File.WriteAllText(Path.Combine(forceTarget, "reactions.csv"), forceExporter.ExportReactionCsv(forceResult));
            File.WriteAllText(Path.Combine(forceTarget, "elastic-member-ends.csv"), forceExporter.ExportMemberCsv(forceResult));
        }
        else if (command == "time-forces-report")
            File.WriteAllText(forceTarget, forceExporter.ExportMarkdown(forceResult, input.Title));
        else throw new ArgumentException($"Unknown M67 command: {command}");
        Console.WriteLine($"M67 dynamic force output written to: {forceTarget}");
        return 0;
    }
    StructuralTimeHistoryAnalysisOutput result = new StructuralSolver2DService().AnalyzeTimeHistory(request);
    var exporter = new TimeHistoryReportExporter();
    if (command == "time")
    {
        Console.WriteLine(exporter.ExportMarkdown(result, input.Title));
        return 0;
    }
    string target = argv[2];
    EnsureDifferentPaths(source, target);
    string? parent = Path.GetDirectoryName(Path.GetFullPath(target));
    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
    if (command == "time-csv")
    {
        // Output is a directory, never silently overwrite an existing model.
        if (File.Exists(target)) throw new ArgumentException("M59_OUTPUT_PATH: CSV destination must be a directory.");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "time-nodes.csv"), exporter.ExportCsv(result));
        File.WriteAllText(Path.Combine(target, "time-energy.csv"), exporter.ExportEnergyCsv(result));
    }
    else if (command == "time-report") File.WriteAllText(target, exporter.ExportMarkdown(result, input.Title));
    else if (command == "time-viewer") File.WriteAllText(target,
        new InteractiveHtmlTimeHistoryViewerExporter().Export(input.Model, result, input.Title));
    else throw new ArgumentException($"Unknown M59 command: {command}");
    Console.WriteLine($"Time-history output written to: {target}");
    return 0;
}

int RunHarmonicCommand(string[] argv)
{
    string command = argv[0].ToLowerInvariant();
    string source = argv[1];
    StructuralModelJsonFile input = StructuralModelJsonReader.Read(source, allowModalWithoutLoadCase: true);
    if (input.Dynamics is null) throw new ArgumentException("M58_DYNAMICS_REQUIRED: Missing dynamics block.");
    using var document = JsonDocument.Parse(File.ReadAllText(source));
    if (!document.RootElement.TryGetProperty("harmonic", out JsonElement harmonicBlock) ||
        harmonicBlock.ValueKind != JsonValueKind.Object)
        throw new ArgumentException("M58_HARMONIC_REQUIRED: Missing harmonic input block.");
    var jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow
    };
    var harmonic = harmonicBlock.Deserialize<HarmonicInputJson>(jsonOptions) ??
        throw new ArgumentException("M58_HARMONIC_REQUIRED: Invalid harmonic block.");
    StructuralHarmonicAnalysisOutput result = new StructuralSolver2DService().AnalyzeHarmonic(
        new StructuralHarmonicAnalysisRequest(input.Model, input.Dynamics,
            harmonic.Forces ?? throw new ArgumentException("M58_FORCES_REQUIRED: harmonic.forces is mandatory."),
            harmonic.FrequenciesHertz ?? throw new ArgumentException("M58_FREQUENCIES_REQUIRED: harmonic.frequenciesHertz is mandatory."),
            harmonic.Damping ?? throw new ArgumentException("M58_DAMPING_REQUIRED: harmonic.damping is mandatory.")));
    var exporter = new HarmonicReportExporter();
    if (command == "harmonic")
    {
        Console.WriteLine(exporter.ExportMarkdown(result, input.Title));
        return 0;
    }
    string target = argv[2];
    EnsureDifferentPaths(source, target);
    if (command == "harmonic-csv")
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(target));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(target, exporter.ExportCsv(result));
    }
    else if (command == "harmonic-viewer")
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(target));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(target, new InteractiveHtmlHarmonicViewerExporter().Export(result));
    }
    else if (command == "harmonic-report")
    {
        string? folder = Path.GetDirectoryName(Path.GetFullPath(target));
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);
        File.WriteAllText(target, exporter.ExportMarkdown(result, input.Title));
    }
    else throw new ArgumentException($"Unsupported harmonic command: {command}");
    Console.WriteLine($"Harmonic output written to: {target}");
    return 0;
}

int RunModalCommand(string[] argv)
{
    string command = argv[0].ToLowerInvariant();
    string inputPath = argv[1];
    bool outputRequested = command != "modal";
    string? target = outputRequested ? argv[2] : null;
    if (target is not null && command != "modal-csv") EnsureDifferentPaths(inputPath, target);
    string? modeCountText = outputRequested ? argv.ElementAtOrDefault(3) : argv.ElementAtOrDefault(2);
    int modeCount = 3;
    if (modeCountText is not null &&
        (!int.TryParse(modeCountText, NumberStyles.None, CultureInfo.InvariantCulture, out modeCount) || modeCount <= 0))
        throw new ArgumentException("Modal mode count must be a positive integer.");

    StructuralModelJsonFile input = StructuralModelJsonReader.Read(inputPath, allowModalWithoutLoadCase: true);
    if (input.Dynamics is null)
        throw new InvalidOperationException("M57_DYNAMICS_REQUIRED: Input JSON must explicitly contain 'dynamics' with schemaVersion 1.");
    StructuralModalAnalysisOutput result = new StructuralSolver2DService().AnalyzeModes(
        new StructuralModalAnalysisRequest(input.Model, input.Dynamics, modeCount));
    var exporter = new ModalReportExporter();
    if (command == "modal")
    {
        Console.WriteLine(exporter.ExportMarkdown(result, input.Title));
        return 0;
    }
    if (target is null) throw new InvalidOperationException("Missing modal output path.");
    if (command == "modal-csv")
    {
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "modal-frequencies.csv"), exporter.ExportSummaryCsv(result));
        File.WriteAllText(Path.Combine(target, "modal-shapes.csv"), exporter.ExportShapesCsv(result));
    }
    else
    {
        string? parent = Path.GetDirectoryName(Path.GetFullPath(target));
        if (parent is not null) Directory.CreateDirectory(parent);
        string content = command switch
        {
            "modal-report" => exporter.ExportMarkdown(result, input.Title),
            "modal-viewer" => new InteractiveHtmlModalViewerExporter().Export(input.Model, result, input.Title),
            _ => throw new ArgumentException($"Unsupported modal command '{command}'.")
        };
        File.WriteAllText(target, content);
    }
    Console.WriteLine($"Modal output written to: {target}");
    return 0;
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
            DetailLevel = string.Equals(args[0], "report-full", StringComparison.OrdinalIgnoreCase) ? MarkdownReportDetail.Full : MarkdownReportDetail.Summary,
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
        _ when NonlinearTruss2DCliM68.IsCommand(command) =>
            (command.EndsWith("-report", StringComparison.OrdinalIgnoreCase) ||
             command.EndsWith("-csv", StringComparison.OrdinalIgnoreCase) ||
             command.EndsWith("-viewer", StringComparison.OrdinalIgnoreCase) ? (3, 3) : (2, 2)),
        _ when IsSeismicCommand(command) && (command.Equals("seismic-time", StringComparison.OrdinalIgnoreCase) || command.Equals("seismic-spectrum", StringComparison.OrdinalIgnoreCase)) => (2, 2),
        _ when IsSeismicCommand(command) => (3, 3),
        _ when IsTimeConvergenceCommand(command) && string.Equals(command, "time-convergence", StringComparison.OrdinalIgnoreCase) => (2, 2),
        _ when IsTimeConvergenceCommand(command) => (3, 3),
        _ when IsTimeCommand(command) && (string.Equals(command, "time", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(command, "time-forces", StringComparison.OrdinalIgnoreCase)) => (2, 2),
        _ when IsTimeCommand(command) => (3, 3),
        _ when IsHarmonicCommand(command) && string.Equals(command, "harmonic", StringComparison.OrdinalIgnoreCase) => (2, 2),
        _ when IsHarmonicCommand(command) => (3, 3),
        _ when IsModalCommand(command) && string.Equals(command, "modal", StringComparison.OrdinalIgnoreCase) => (2, 3),
        _ when IsModalCommand(command) => (3, 4),
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
    string.Equals(command, "report-full", StringComparison.OrdinalIgnoreCase) ||
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

static bool IsModalCommand(string command) =>
    string.Equals(command, "modal", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "modal-report", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "modal-csv", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(command, "modal-viewer", StringComparison.OrdinalIgnoreCase);

static bool IsSeismicCommand(string command) =>
    command.Equals("seismic-time", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("seismic-time-report", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("seismic-time-csv", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("seismic-spectrum", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("seismic-spectrum-report", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("seismic-spectrum-csv", StringComparison.OrdinalIgnoreCase);

static bool IsTimeConvergenceCommand(string command) =>
    command.Equals("time-convergence", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-convergence-report", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-convergence-csv", StringComparison.OrdinalIgnoreCase);

static bool IsTimeCommand(string command) =>
    command.Equals("time", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-report", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-csv", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-viewer", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-forces", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-forces-report", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("time-forces-csv", StringComparison.OrdinalIgnoreCase);

static bool IsHarmonicCommand(string command) =>
    command.Equals("harmonic", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("harmonic-report", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("harmonic-csv", StringComparison.OrdinalIgnoreCase) ||
    command.Equals("harmonic-viewer", StringComparison.OrdinalIgnoreCase);

// M58 opt-in input only. Old JSON model schema and static command semantics are unchanged.
sealed record HarmonicInputJson(
    IReadOnlyList<StructuralHarmonicNodalForce>? Forces,
    IReadOnlyList<double>? FrequenciesHertz,
    StructuralHarmonicDamping? Damping);

// M59 transient JSON is isolated from the existing static, modal and harmonic contracts.
sealed record TimeHistoryInputJson(
    double? TimeStepSeconds,
    int? StepCount,
    StructuralHarmonicDamping? Damping,
    IReadOnlyList<StructuralTimeLoadPattern>? Loads,
    IReadOnlyList<StructuralTimeInitialNodeState>? InitialStates);

// M60 intentionally separate from all pre-existing JSON features.
sealed record SeismicInputJson(string? Direction, SeismicTimeInputJson? TimeHistory,
    SeismicSpectrumInputJson? Spectrum);
sealed record SeismicTimeInputJson(double? TimeStepSeconds, int? StepCount,
    StructuralHarmonicDamping? Damping, IReadOnlyList<SeismicAccelerationKnot>? GroundAcceleration);
sealed record SeismicSpectrumInputJson(int? ModeCount, IReadOnlyList<SeismicSpectrumPoint>? Points,
    string? Combination = null, double? ModalDampingRatio = null);
