using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Cli.Input;

/// <summary>
/// Represents a structural model loaded from a JSON input file together with CLI metadata.
/// </summary>
/// <param name="Title">User-facing title of the input model.</param>
/// <param name="Description">Short description of the structural scheme.</param>
/// <param name="LoadCaseId">Default analysis id to use. It may refer to either a load case or a manual load combination.</param>
/// <param name="Model">Structural model built from the JSON file.</param>
public sealed record StructuralModelJsonFile(
    string Title,
    string Description,
    string LoadCaseId,
    StructuralModel Model);
