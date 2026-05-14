using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.PlaneStructure2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Tests.Benchmarks;

/// <summary>
/// Runs benchmark models with the same solver selection rule used by the CLI.
/// </summary>
internal static class BenchmarkAnalysisRunner
{
    public static (StructuralModel Model, StructuralAnalysisResult Result) Run(string repositoryRoot, BenchmarkCase benchmark)
    {
        string modelPath = BenchmarkRepository.ResolveModelPath(repositoryRoot, benchmark);
        StructuralModelJsonFile modelFile = StructuralModelJsonReader.Read(modelPath);
        StructuralModel model = modelFile.Model;
        StructuralAnalysisResult result = Analyze(model, benchmark.AnalysisId);

        return (model, result);
    }

    private static StructuralAnalysisResult Analyze(StructuralModel model, string analysisId)
    {
        bool isTrussOnly = model.Members.All(member => member.Type == MemberType.Truss2D);
        bool isFrameOnly = model.Members.All(member => member.Type == MemberType.Frame2D);
        bool isCombination = model.LoadCombinations.Any(combination =>
            string.Equals(combination.Id, analysisId, StringComparison.OrdinalIgnoreCase));

        if (isTrussOnly)
        {
            Truss2DAnalyzer analyzer = new();
            return isCombination ? analyzer.AnalyzeCombination(model, analysisId) : analyzer.Analyze(model, analysisId);
        }

        if (isFrameOnly)
        {
            Frame2DAnalyzer analyzer = new();
            return isCombination ? analyzer.AnalyzeCombination(model, analysisId) : analyzer.Analyze(model, analysisId);
        }

        PlaneStructureAnalyzer planeAnalyzer = new();
        return isCombination ? planeAnalyzer.AnalyzeCombination(model, analysisId) : planeAnalyzer.Analyze(model, analysisId);
    }
}
