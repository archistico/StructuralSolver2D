using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Model.Generators;

/// <summary>
/// Creates small parametric beam models useful for examples, regression tests and demos.
/// </summary>
public static class ParametricBeamGenerator
{
    /// <summary>
    /// Creates a simply supported beam discretized into equal frame members.
    /// </summary>
    public static StructuralModel SimplySupportedBeam(
        double span,
        int divisions,
        ParametricModelGenerationOptions? options = null)
    {
        EnsurePositive(span, nameof(span));
        EnsureAtLeast(divisions, 1, nameof(divisions));

        options ??= new ParametricModelGenerationOptions();
        StructuralModel model = CreateModelWithDefaults(options);

        for (int i = 0; i <= divisions; i++)
        {
            double x = span * i / divisions;
            model.AddNode(new StructuralNode(NodeId(i), x, 0.0));
        }

        for (int i = 0; i < divisions; i++)
        {
            model.AddMember(new StructuralMember(
                MemberId(i + 1),
                NodeId(i),
                NodeId(i + 1),
                options.Material.Id,
                options.Section.Id,
                options.FrameMemberType));
        }

        model.AddSupport(StructuralSupport.Hinge("S1", NodeId(0), "Left hinge"));
        model.AddSupport(StructuralSupport.SimpleSupport("S2", NodeId(divisions), "Right roller"));

        return model;
    }

    /// <summary>
    /// Creates a Gerber beam with two internal hinges and asymmetric vertical point loads.
    /// The hinges are represented by end moment releases on the adjacent frame members.
    /// </summary>
    public static StructuralModel GerberBeamWithAsymmetricLoads(
        double leftSpan,
        double suspendedSpan,
        double rightSpan,
        ParametricModelGenerationOptions? options = null)
    {
        EnsurePositive(leftSpan, nameof(leftSpan));
        EnsurePositive(suspendedSpan, nameof(suspendedSpan));
        EnsurePositive(rightSpan, nameof(rightSpan));

        options ??= new ParametricModelGenerationOptions();
        StructuralModel model = CreateModelWithDefaults(options);

        double x0 = 0.0;
        double x1 = leftSpan;
        double x2 = leftSpan + suspendedSpan;
        double x3 = leftSpan + suspendedSpan + rightSpan;

        model.AddNode(new StructuralNode("N0", x0, 0.0, "Left support"));
        model.AddNode(new StructuralNode("N1", x1, 0.0, "Left internal hinge"));
        model.AddNode(new StructuralNode("N2", x2, 0.0, "Right internal hinge"));
        model.AddNode(new StructuralNode("N3", x3, 0.0, "Right support"));

        model.AddMember(new StructuralMember("M1", "N0", "N1", options.Material.Id, options.Section.Id, options.FrameMemberType, ReleaseEndMoment: true));
        model.AddMember(new StructuralMember("M2", "N1", "N2", options.Material.Id, options.Section.Id, options.FrameMemberType, ReleaseStartMoment: true, ReleaseEndMoment: true));
        model.AddMember(new StructuralMember("M3", "N2", "N3", options.Material.Id, options.Section.Id, options.FrameMemberType, ReleaseStartMoment: true));

        model.AddSupport(StructuralSupport.Hinge("S1", "N0", "Left hinge"));
        model.AddSupport(StructuralSupport.SimpleSupport("S2", "N3", "Right roller"));

        if (options.AddDefaultLoadCase)
        {
            model.AddLoad(StructuralLoad.PointLoadOnMember("P1", options.LoadCaseId, "M1", StructuralLoadDirection.GlobalY, -12.0, 0.65, "Asymmetric left point load"));
            model.AddLoad(StructuralLoad.PointLoadOnMember("P2", options.LoadCaseId, "M2", StructuralLoadDirection.GlobalY, -7.0, 0.35, "Asymmetric suspended-span point load"));
            model.AddLoad(StructuralLoad.PointLoadOnMember("P3", options.LoadCaseId, "M3", StructuralLoadDirection.GlobalY, -18.0, 0.70, "Asymmetric right point load"));
        }

        return model;
    }

    private static StructuralModel CreateModelWithDefaults(ParametricModelGenerationOptions options)
    {
        StructuralModel model = new StructuralModel()
            .AddMaterial(options.Material)
            .AddSection(options.Section);

        if (options.AddDefaultLoadCase)
        {
            model.AddLoadCase(new StructuralLoadCase(options.LoadCaseId, options.LoadCaseName));
        }

        return model;
    }

    private static string NodeId(int index) => FormattableString.Invariant($"N{index}");

    private static string MemberId(int index) => FormattableString.Invariant($"M{index}");

    private static void EnsurePositive(double value, string parameterName)
    {
        if (value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be greater than zero.");
        }
    }

    private static void EnsureAtLeast(int value, int minimum, string parameterName)
    {
        if (value < minimum)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"The value must be at least {minimum}.");
        }
    }
}
