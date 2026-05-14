using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Cli.Input;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Tests.ValidationExamples;

/// <summary>
/// Ensures that the user-facing validation JSON files remain loadable, valid and analyzable.
/// These tests intentionally check robustness and finite results, not code-specific design values.
/// </summary>
public sealed class JsonValidationExampleFileTests
{
    public static IEnumerable<object[]> ValidationJsonFiles() =>
        Directory.EnumerateFiles(GetValidationExamplesDirectory(), "*.json")
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(path => new object[] { path });

    [Theory]
    [MemberData(nameof(ValidationJsonFiles))]
    public void ValidationJsonFile_ShouldBeValidAndAnalyzable(string jsonPath)
    {
        StructuralModelJsonFile input = StructuralModelJsonReader.Read(jsonPath);
        StructuralModel model = input.Model;
        StructuralModelValidationResult validation = new StructuralModelValidator().Validate(model);

        Assert.True(
            validation.IsValid,
            $"JSON validation example '{Path.GetFileName(jsonPath)}' should be valid. Issues: {string.Join("; ", validation.Issues.Select(issue => issue.Message))}");

        var service = new StructuralSolver2DService();
        var options = new StructuralAnalysisOptions
        {
            InternalForceSampleCount = 9,
            DisplacementSampleCount = 9,
            IncludeDisplacementDiagrams = true,
        };

        StructuralAnalysisOutput output;
        try
        {
            output = service.AnalyzeLoadCase(model, input.LoadCaseId, options);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"JSON validation example '{Path.GetFileName(jsonPath)}' should be analyzable, but analysis failed.",
                exception);
        }

        Assert.Equal(model.Nodes.Count, output.Result.Displacements.Count);
        Assert.Equal(model.Supports.Count, output.Result.Reactions.Count);
        Assert.Equal(model.Members.Count, output.Result.MemberEndForces.Count);
        Assert.Equal(model.Members.Count, output.InternalForceDiagrams.Count);

        AssertFinite(output.Result.Displacements.SelectMany(displacement => new[]
        {
            displacement.Ux,
            displacement.Uy,
            displacement.Rz,
        }));

        AssertFinite(output.Result.Reactions.SelectMany(reaction => new[]
        {
            reaction.Fx,
            reaction.Fy,
            reaction.Mz,
        }));

        AssertFinite(output.Result.MemberEndForces.SelectMany(force => new[]
        {
            force.StartAxial,
            force.StartShear,
            force.StartMoment,
            force.EndAxial,
            force.EndShear,
            force.EndMoment,
        }));
    }

    private static void AssertFinite(IEnumerable<double> values)
    {
        foreach (double value in values)
        {
            Assert.True(double.IsFinite(value), $"Expected a finite numerical result, but found {value}.");
        }
    }

    private static string GetValidationExamplesDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, "examples", "validation");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find the examples/validation directory from the test output path.");
    }
}
