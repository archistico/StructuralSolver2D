using System.Diagnostics;
using System.Text.Json.Nodes;
using StructuralSolver2D.Cli.Input;

namespace StructuralSolver2D.Cli.Tests;

internal sealed class CliTestWorkspace : IDisposable
{
    private static readonly string Parent = Path.Combine(Path.GetTempPath(), "StructuralSolver2D.Cli.Tests");
    public string DirectoryPath { get; } = Path.Combine(Parent, "cli regression " + Guid.NewGuid().ToString("N"));

    public CliTestWorkspace() => Directory.CreateDirectory(DirectoryPath);

    public string PathFor(string name) => Path.Combine(DirectoryPath, name);
    public static string ExamplePath(string relative) => Path.Combine(AppContext.BaseDirectory, "Examples", relative);
    public static JsonObject Model(string relative = "beams/cantilever-point-load.json") =>
        JsonNode.Parse(File.ReadAllText(ExamplePath(relative)))!.AsObject();

    public string Save(JsonObject model, string name = "input.json")
    {
        string path = PathFor(name);
        File.WriteAllText(path, model.ToJsonString());
        return path;
    }

    public async Task<CliProcessResult> Run(params string[] arguments)
    {
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet")
        {
            WorkingDirectory = DirectoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.ArgumentList.Add(typeof(StructuralModelJsonReader).Assembly.Location);
        foreach (string argument in arguments) start.ArgumentList.Add(argument);
        using var process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try { await process.WaitForExitAsync(timeout.Token); }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The CLI process did not finish within 30 seconds.");
        }
        return new CliProcessResult(process.ExitCode, await stdout, await stderr);
    }

    public void Dispose()
    {
        // Delete only the unique directory this fixture created below its fixed temporary parent.
        if (Path.GetDirectoryName(DirectoryPath) != Parent) throw new InvalidOperationException("Unexpected test directory.");
        Directory.Delete(DirectoryPath, recursive: true);
    }
}

internal sealed record CliProcessResult(int ExitCode, string Stdout, string Stderr)
{
    public void AssertSuccess()
    {
        Assert.True(ExitCode == 0, $"CLI exited with {ExitCode}: {Stderr}");
        Assert.Empty(Stderr);
    }
}
