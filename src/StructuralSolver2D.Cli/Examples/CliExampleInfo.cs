namespace StructuralSolver2D.Cli.Examples;

/// <summary>
/// Describes a built-in CLI example without constructing the full model.
/// </summary>
/// <param name="Name">Stable command-line example name.</param>
/// <param name="Title">User-facing example title.</param>
/// <param name="Description">Short description of the structural scheme.</param>
public sealed record CliExampleInfo(
    string Name,
    string Title,
    string Description);
