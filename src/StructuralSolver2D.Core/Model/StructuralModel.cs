namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Contains the structural entities of a plane structural model.
/// This model is independent from any CAD, UI, rendering or persistence layer.
/// </summary>
public sealed class StructuralModel
{
    /// <summary>
    /// Gets the model nodes.
    /// Internal coordinate unit: meter [m].
    /// </summary>
    public IList<StructuralNode> Nodes { get; } = new List<StructuralNode>();

    /// <summary>
    /// Gets the one-dimensional structural members.
    /// </summary>
    public IList<StructuralMember> Members { get; } = new List<StructuralMember>();

    /// <summary>
    /// Gets the structural materials.
    /// </summary>
    public IList<StructuralMaterial> Materials { get; } = new List<StructuralMaterial>();

    /// <summary>
    /// Gets the structural sections.
    /// </summary>
    public IList<StructuralSection> Sections { get; } = new List<StructuralSection>();

    /// <summary>
    /// Gets the nodal supports.
    /// </summary>
    public IList<StructuralSupport> Supports { get; } = new List<StructuralSupport>();

    /// <summary>
    /// Gets the load cases available in the model.
    /// </summary>
    public IList<StructuralLoadCase> LoadCases { get; } = new List<StructuralLoadCase>();

    /// <summary>
    /// Gets the structural loads assigned to nodes, members or the whole model.
    /// </summary>
    public IList<StructuralLoad> Loads { get; } = new List<StructuralLoad>();

    /// <summary>
    /// Adds a node and returns the current model for fluent construction.
    /// </summary>
    public StructuralModel AddNode(StructuralNode node)
    {
        Nodes.Add(node);
        return this;
    }

    /// <summary>
    /// Adds a member and returns the current model for fluent construction.
    /// </summary>
    public StructuralModel AddMember(StructuralMember member)
    {
        Members.Add(member);
        return this;
    }

    /// <summary>
    /// Adds a material and returns the current model for fluent construction.
    /// </summary>
    public StructuralModel AddMaterial(StructuralMaterial material)
    {
        Materials.Add(material);
        return this;
    }

    /// <summary>
    /// Adds a section and returns the current model for fluent construction.
    /// </summary>
    public StructuralModel AddSection(StructuralSection section)
    {
        Sections.Add(section);
        return this;
    }

    /// <summary>
    /// Adds a support and returns the current model for fluent construction.
    /// </summary>
    public StructuralModel AddSupport(StructuralSupport support)
    {
        Supports.Add(support);
        return this;
    }

    /// <summary>
    /// Adds a load case and returns the current model for fluent construction.
    /// </summary>
    public StructuralModel AddLoadCase(StructuralLoadCase loadCase)
    {
        LoadCases.Add(loadCase);
        return this;
    }

    /// <summary>
    /// Adds a structural load and returns the current model for fluent construction.
    /// </summary>
    public StructuralModel AddLoad(StructuralLoad load)
    {
        Loads.Add(load);
        return this;
    }
}
