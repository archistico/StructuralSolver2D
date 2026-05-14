namespace StructuralSolver2D.Core.Model.Generators;

/// <summary>
/// Creates common two-dimensional truss layouts for validation examples and demos.
/// </summary>
public static class ParametricTrussGenerator
{
    /// <summary>
    /// Creates a Pratt-like bridge truss with verticals and alternating diagonals.
    /// </summary>
    public static StructuralModel PrattBridge(
        double span,
        double height,
        int panels,
        ParametricModelGenerationOptions? options = null)
    {
        EnsurePositive(span, nameof(span));
        EnsurePositive(height, nameof(height));
        EnsureAtLeast(panels, 2, nameof(panels));

        options ??= new ParametricModelGenerationOptions();
        StructuralModel model = CreateModelWithDefaults(options);
        AddTwoChordNodes(model, span, height, panels, topYSelector: _ => height);
        AddTwoChordTrussMembers(model, panels, options, includeVerticals: true);

        int memberIndex = model.Members.Count + 1;
        for (int i = 0; i < panels; i++)
        {
            string start = i < panels / 2 ? BottomNodeId(i) : TopNodeId(i);
            string end = i < panels / 2 ? TopNodeId(i + 1) : BottomNodeId(i + 1);
            model.AddMember(CreateTrussMember(memberIndex++, start, end, options, "Pratt diagonal"));
        }

        AddSimpleTrussSupports(model, panels);
        return model;
    }

    /// <summary>
    /// Creates an isostatic triangular truss with one bottom chord and one apex.
    /// </summary>
    public static StructuralModel IsostaticTriangularTruss(
        double span,
        double rise,
        ParametricModelGenerationOptions? options = null)
    {
        EnsurePositive(span, nameof(span));
        EnsurePositive(rise, nameof(rise));

        options ??= new ParametricModelGenerationOptions();
        StructuralModel model = CreateModelWithDefaults(options);

        model.AddNode(new StructuralNode("N1", 0.0, 0.0, "Left support"));
        model.AddNode(new StructuralNode("N2", span, 0.0, "Right support"));
        model.AddNode(new StructuralNode("N3", span / 2.0, rise, "Apex"));

        model.AddMember(new StructuralMember("M1", "N1", "N2", options.Material.Id, options.Section.Id, options.TrussMemberType, "Bottom chord"));
        model.AddMember(new StructuralMember("M2", "N1", "N3", options.Material.Id, options.Section.Id, options.TrussMemberType, "Left web"));
        model.AddMember(new StructuralMember("M3", "N3", "N2", options.Material.Id, options.Section.Id, options.TrussMemberType, "Right web"));

        model.AddSupport(StructuralSupport.Hinge("S1", "N1", "Left hinge"));
        model.AddSupport(StructuralSupport.SimpleSupport("S2", "N2", "Right roller"));

        return model;
    }

    /// <summary>
    /// Creates a Nielsen-like truss with a parabolic upper chord and diagonal hangers.
    /// </summary>
    public static StructuralModel NielsenParabolicTruss(
        double span,
        double rise,
        int panels,
        ParametricModelGenerationOptions? options = null) =>
        ParabolicTwoChordTruss(span, rise, panels, inverted: false, alternatingDiagonals: true, options);

    /// <summary>
    /// Creates a two-chord truss with the parabolic chord below the deck line.
    /// </summary>
    public static StructuralModel InvertedParabolicTruss(
        double span,
        double rise,
        int panels,
        ParametricModelGenerationOptions? options = null) =>
        ParabolicTwoChordTruss(span, rise, panels, inverted: true, alternatingDiagonals: false, options);

    /// <summary>
    /// Creates a hyperstatic truss layout with both diagonals in every panel.
    /// </summary>
    public static StructuralModel DoubleDiagonalTruss(
        double span,
        double height,
        int panels,
        ParametricModelGenerationOptions? options = null)
    {
        EnsurePositive(span, nameof(span));
        EnsurePositive(height, nameof(height));
        EnsureAtLeast(panels, 1, nameof(panels));

        options ??= new ParametricModelGenerationOptions();
        StructuralModel model = CreateModelWithDefaults(options);
        AddTwoChordNodes(model, span, height, panels, topYSelector: _ => height);
        AddTwoChordTrussMembers(model, panels, options, includeVerticals: true);

        int memberIndex = model.Members.Count + 1;
        for (int i = 0; i < panels; i++)
        {
            model.AddMember(CreateTrussMember(memberIndex++, BottomNodeId(i), TopNodeId(i + 1), options, "Rising diagonal"));
            model.AddMember(CreateTrussMember(memberIndex++, TopNodeId(i), BottomNodeId(i + 1), options, "Falling diagonal"));
        }

        AddSimpleTrussSupports(model, panels);
        return model;
    }

    private static StructuralModel ParabolicTwoChordTruss(
        double span,
        double rise,
        int panels,
        bool inverted,
        bool alternatingDiagonals,
        ParametricModelGenerationOptions? options)
    {
        EnsurePositive(span, nameof(span));
        EnsurePositive(rise, nameof(rise));
        EnsureAtLeast(panels, 2, nameof(panels));

        options ??= new ParametricModelGenerationOptions();
        StructuralModel model = CreateModelWithDefaults(options);

        AddTwoChordNodes(
            model,
            span,
            rise,
            panels,
            topYSelector: t =>
            {
                double ordinate = 4.0 * rise * t * (1.0 - t);
                return inverted ? -ordinate : ordinate;
            });

        AddTwoChordTrussMembers(model, panels, options, includeVerticals: true, includeEndVerticals: false);

        int memberIndex = model.Members.Count + 1;
        for (int i = 0; i < panels; i++)
        {
            bool rising = !alternatingDiagonals || i % 2 == 0;
            string start = rising ? BottomNodeId(i) : TopNodeId(i);
            string end = rising ? TopNodeId(i + 1) : BottomNodeId(i + 1);
            model.AddMember(CreateTrussMember(memberIndex++, start, end, options, "Parabolic web"));
        }

        AddSimpleTrussSupports(model, panels);
        return model;
    }

    private static void AddTwoChordNodes(
        StructuralModel model,
        double span,
        double height,
        int panels,
        Func<double, double> topYSelector)
    {
        for (int i = 0; i <= panels; i++)
        {
            double t = (double)i / panels;
            double x = span * t;
            model.AddNode(new StructuralNode(BottomNodeId(i), x, 0.0, "Bottom chord node"));
            model.AddNode(new StructuralNode(TopNodeId(i), x, topYSelector(t), "Upper/lower curved chord node"));
        }
    }

    private static void AddTwoChordTrussMembers(
        StructuralModel model,
        int panels,
        ParametricModelGenerationOptions options,
        bool includeVerticals,
        bool includeEndVerticals = true)
    {
        int memberIndex = 1;
        for (int i = 0; i < panels; i++)
        {
            model.AddMember(CreateTrussMember(memberIndex++, BottomNodeId(i), BottomNodeId(i + 1), options, "Bottom chord"));
            model.AddMember(CreateTrussMember(memberIndex++, TopNodeId(i), TopNodeId(i + 1), options, "Top chord"));
        }

        if (!includeVerticals)
        {
            return;
        }

        int firstVerticalIndex = includeEndVerticals ? 0 : 1;
        int lastVerticalIndex = includeEndVerticals ? panels : panels - 1;

        for (int i = firstVerticalIndex; i <= lastVerticalIndex; i++)
        {
            model.AddMember(CreateTrussMember(memberIndex++, BottomNodeId(i), TopNodeId(i), options, "Vertical"));
        }
    }

    private static StructuralMember CreateTrussMember(
        int index,
        string startNodeId,
        string endNodeId,
        ParametricModelGenerationOptions options,
        string label) =>
        new(FormattableString.Invariant($"M{index}"), startNodeId, endNodeId, options.Material.Id, options.Section.Id, options.TrussMemberType, label);

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

    private static void AddSimpleTrussSupports(StructuralModel model, int panels)
    {
        model.AddSupport(StructuralSupport.Hinge("S1", BottomNodeId(0), "Left hinge"));
        model.AddSupport(StructuralSupport.SimpleSupport("S2", BottomNodeId(panels), "Right roller"));
    }

    private static string BottomNodeId(int index) => FormattableString.Invariant($"B{index}");

    private static string TopNodeId(int index) => FormattableString.Invariant($"T{index}");

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
