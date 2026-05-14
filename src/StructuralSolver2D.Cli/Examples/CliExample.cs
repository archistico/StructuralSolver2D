using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Cli.Examples;

/// <summary>
/// Represents a built-in CLI example model.
/// </summary>
/// <param name="Name">Stable command-line example name.</param>
/// <param name="Title">User-facing example title.</param>
/// <param name="Description">Short description of the structural scheme.</param>
/// <param name="LoadCaseId">Load case analyzed by the CLI.</param>
/// <param name="Model">Structural model.</param>
public sealed record CliExample(
    string Name,
    string Title,
    string Description,
    string LoadCaseId,
    StructuralModel Model);
