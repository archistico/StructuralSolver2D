using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Cli.Input;

/// <summary>
/// Represents a structural model loaded from a JSON input file together with CLI metadata.
/// </summary>
/// <param name="Title">User-facing title of the input model.</param>
/// <param name="Description">Short description of the structural scheme.</param>
/// <param name="LoadCaseId">Default load case to analyze.</param>
/// <param name="Model">Structural model built from the JSON file.</param>
public sealed record StructuralModelJsonFile(
    string Title,
    string Description,
    string LoadCaseId,
    StructuralModel Model);
