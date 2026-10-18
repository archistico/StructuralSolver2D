using System.Globalization;
using System.Text.RegularExpressions;
using StructuralSolver2D.Analysis.PublicApi;
using StructuralSolver2D.Cli.Input;

namespace StructuralSolver2D.Cli.Tests;

public sealed class CliSummaryRegressionTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task Summary_ShouldReportMagnitudesAndKeepSignedResultsAndLocations(int sign)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model();
        model["loads"]![0]!["value"] = sign * 12;
        var axial = model["loads"]![0]!.DeepClone();
        axial["id"] = "PX";
        axial["direction"] = "GlobalX";
        axial["value"] = sign * 100;
        model["loads"]!.AsArray().Add(axial);
        string source = workspace.Save(model);
        var analysis = new StructuralSolver2DService().AnalyzeLoadCase(StructuralModelJsonReader.Read(source).Model, "LC1");
        var summary = analysis.Summary;
        var expected = new Dictionary<string, (double Value, string Location)>
        {
            ["Ux"] = (summary.MaxAbsUx.Value, "at node " + summary.MaxAbsUx.EntityId),
            ["Uy"] = (summary.MaxAbsUy.Value, "at node " + summary.MaxAbsUy.EntityId),
            ["Rz"] = (summary.MaxAbsRz.Value, "at node " + summary.MaxAbsRz.EntityId),
            ["Fx"] = (summary.MaxAbsReactionFx.Value, "at support " + summary.MaxAbsReactionFx.EntityId),
            ["Fy"] = (summary.MaxAbsReactionFy.Value, "at support " + summary.MaxAbsReactionFy.EntityId),
            ["Mz"] = (summary.MaxAbsReactionMz.Value, "at support " + summary.MaxAbsReactionMz.EntityId),
            ["N"] = (summary.MaxAbsNormalForce.Value, $"at member {summary.MaxAbsNormalForce.MemberId}, x = {summary.MaxAbsNormalForce.Distance.ToString("0.000000", CultureInfo.InvariantCulture)} m"),
            ["V"] = (summary.MaxAbsShearForce.Value, $"at member {summary.MaxAbsShearForce.MemberId}, x = {summary.MaxAbsShearForce.Distance.ToString("0.000000", CultureInfo.InvariantCulture)} m"),
            ["M"] = (summary.MaxAbsBendingMoment.Value, $"at member {summary.MaxAbsBendingMoment.MemberId}, x = {summary.MaxAbsBendingMoment.Distance.ToString("0.000000", CultureInfo.InvariantCulture)} m")
        };
        var result = await workspace.Run("analyze", source);
        result.AssertSuccess();
        var matches = Regex.Matches(result.Stdout, @"Max \|(?<name>\w+)\|\s*=\s*(?<value>-?[\d.]+)[^\r\n]+", RegexOptions.CultureInvariant);
        Assert.Equal(9, matches.Count);
        foreach (Match match in matches)
        {
            var extreme = expected[match.Groups["name"].Value];
            double value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
            Assert.True(value >= 0, match.Value);
            Assert.Equal(Math.Abs(extreme.Value), value, precision: 6);
            Assert.Contains(extreme.Location, match.Value);
        }
        string signedUy = analysis.Result.Displacements.Single(item => item.NodeId == "B").Uy.ToString("0.000000", CultureInfo.InvariantCulture);
        Assert.Contains(signedUy, result.Stdout.Split("Maximum absolute results:")[0]);
    }
}
