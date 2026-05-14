using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Cli.Examples;

/// <summary>
/// Creates built-in structural examples used by the minimal command-line interface.
/// These examples are intentionally hardcoded to keep the first CLI independent from JSON parsing.
/// </summary>
public static class CliExampleModelFactory
{
    private const string LoadCaseId = "LC1";
    private const double ElasticModulus = 210_000_000.0;
    private const double Area = 0.003;
    private const double Inertia = 0.00002;

    private static readonly IReadOnlyList<CliExampleInfo> Examples = new[]
    {
        new CliExampleInfo(
            "simple-supported-beam",
            "Simply supported beam with uniform load",
            "L = 5 m, q = 10 kN/m downward."),
        new CliExampleInfo(
            "cantilever-point-load",
            "Cantilever with point load at the free end",
            "L = 4 m, P = 12 kN downward at the free end."),
        new CliExampleInfo(
            "cantilever-uniform-load",
            "Cantilever with uniform load",
            "L = 5 m, q = 10 kN/m downward."),
        new CliExampleInfo(
            "axial-bar",
            "Axially loaded bar",
            "L = 5 m, P = 100 kN in global X."),
    };

    /// <summary>
    /// Gets all examples currently available in the CLI.
    /// </summary>
    public static IReadOnlyList<CliExampleInfo> GetAvailableExamples() => Examples;

    /// <summary>
    /// Creates an example by name.
    /// </summary>
    /// <param name="name">Example name.</param>
    /// <returns>The requested example.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the example name is unknown.</exception>
    public static CliExample Create(string name) =>
        name.ToLowerInvariant() switch
        {
            "simple-supported-beam" => CreateSimpleSupportedBeam(),
            "cantilever-point-load" => CreateCantileverPointLoad(),
            "cantilever-uniform-load" => CreateCantileverUniformLoad(),
            "axial-bar" => CreateAxialBar(),
            _ => throw new KeyNotFoundException($"Unknown CLI example '{name}'.")
        };

    private static CliExample CreateSimpleSupportedBeam()
    {
        StructuralModel model = CreateSingleMemberModel(5.0)
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", LoadCaseId, "M1", StructuralLoadDirection.GlobalY, -10.0));

        return new CliExample(
            "simple-supported-beam",
            "Simply supported beam with uniform load",
            "L = 5 m, q = 10 kN/m downward.",
            LoadCaseId,
            model);
    }

    private static CliExample CreateCantileverPointLoad()
    {
        StructuralModel model = CreateSingleMemberModel(4.0)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.NodalForce("P1", LoadCaseId, "B", StructuralLoadDirection.GlobalY, -12.0));

        return new CliExample(
            "cantilever-point-load",
            "Cantilever with point load at the free end",
            "L = 4 m, P = 12 kN downward at the free end.",
            LoadCaseId,
            model);
    }

    private static CliExample CreateCantileverUniformLoad()
    {
        StructuralModel model = CreateSingleMemberModel(5.0)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddLoad(StructuralLoad.UniformDistributedLoad("Q1", LoadCaseId, "M1", StructuralLoadDirection.GlobalY, -10.0));

        return new CliExample(
            "cantilever-uniform-load",
            "Cantilever with uniform load",
            "L = 5 m, q = 10 kN/m downward.",
            LoadCaseId,
            model);
    }

    private static CliExample CreateAxialBar()
    {
        StructuralModel model = CreateSingleMemberModel(5.0)
            .AddSupport(StructuralSupport.Fixed("SA", "A"))
            .AddSupport(new StructuralSupport("SB", "B", false, true, true, SupportType.Custom, "Transverse and rotational restraint"))
            .AddLoad(StructuralLoad.NodalForce("P1", LoadCaseId, "B", StructuralLoadDirection.GlobalX, 100.0));

        return new CliExample(
            "axial-bar",
            "Axially loaded bar",
            "L = 5 m, P = 100 kN in global X.",
            LoadCaseId,
            model);
    }

    private static StructuralModel CreateSingleMemberModel(double length) =>
        new StructuralModel()
            .AddNode(new StructuralNode("A", 0.0, 0.0))
            .AddNode(new StructuralNode("B", length, 0.0))
            .AddMaterial(new StructuralMaterial("MAT", "Generic elastic material", ElasticModulus))
            .AddSection(new StructuralSection("SEC", "Generic section", Area, Inertia))
            .AddMember(new StructuralMember("M1", "A", "B", "MAT", "SEC", MemberType.Frame2D))
            .AddLoadCase(new StructuralLoadCase(LoadCaseId, "Default load case"));
}
