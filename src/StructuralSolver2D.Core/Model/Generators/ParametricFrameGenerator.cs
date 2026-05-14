namespace StructuralSolver2D.Core.Model.Generators;

/// <summary>
/// Creates small parametric rigid-jointed frame models.
/// </summary>
public static class ParametricFrameGenerator
{
    /// <summary>
    /// Creates a single-bay portal frame with fixed column bases and rigid beam-column joints.
    /// </summary>
    public static StructuralModel PortalFrame(
        double bayWidth,
        double height,
        ParametricModelGenerationOptions? options = null)
    {
        EnsurePositive(bayWidth, nameof(bayWidth));
        EnsurePositive(height, nameof(height));

        options ??= new ParametricModelGenerationOptions();
        StructuralModel model = CreateModelWithDefaults(options);

        model.AddNode(new StructuralNode("N1", 0.0, 0.0, "Left base"));
        model.AddNode(new StructuralNode("N2", bayWidth, 0.0, "Right base"));
        model.AddNode(new StructuralNode("N3", 0.0, height, "Left eaves"));
        model.AddNode(new StructuralNode("N4", bayWidth, height, "Right eaves"));

        model.AddMember(new StructuralMember("C1", "N1", "N3", options.Material.Id, options.Section.Id, options.FrameMemberType, "Left column"));
        model.AddMember(new StructuralMember("B1", "N3", "N4", options.Material.Id, options.Section.Id, options.FrameMemberType, "Rigid beam"));
        model.AddMember(new StructuralMember("C2", "N2", "N4", options.Material.Id, options.Section.Id, options.FrameMemberType, "Right column"));

        model.AddSupport(StructuralSupport.Fixed("S1", "N1", "Left fixed base"));
        model.AddSupport(StructuralSupport.Fixed("S2", "N2", "Right fixed base"));

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

    private static void EnsurePositive(double value, string parameterName)
    {
        if (value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The value must be greater than zero.");
        }
    }
}
