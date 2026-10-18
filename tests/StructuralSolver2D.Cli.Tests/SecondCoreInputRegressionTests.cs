namespace StructuralSolver2D.Cli.Tests;

public sealed class SecondCoreInputRegressionTests
{
    [Theory]
    [InlineData("height", "SECTION_INVALID_HEIGHT")]
    [InlineData("width", "SECTION_INVALID_WIDTH")]
    public async Task Analyze_ShouldRejectInvalidOptionalDimension(string property, string code)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model(); model["sections"]![0]![property] = -.2;
        var result = await workspace.Run("analyze", workspace.Save(model));
        Assert.Equal(2, result.ExitCode); Assert.Contains(code, result.Stderr);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public async Task Analyze_ShouldRejectNumericUnknownSupportType(int type)
    {
        using var workspace = new CliTestWorkspace();
        var model = CliTestWorkspace.Model(); model["supports"]![0]!["type"] = type;
        var result = await workspace.Run("analyze", workspace.Save(model));
        Assert.Equal(2, result.ExitCode); Assert.Contains("SUPPORT_INVALID_TYPE", result.Stderr);
    }
}
