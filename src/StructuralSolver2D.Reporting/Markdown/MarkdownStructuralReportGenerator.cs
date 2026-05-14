using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Serviceability;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Reporting.Markdown;

/// <summary>
/// Generates a human-readable Markdown report for one structural analysis result.
/// </summary>
public sealed class MarkdownStructuralReportGenerator
{
    /// <summary>
    /// Generates a Markdown report from the model, analysis result, internal-force diagrams and summary.
    /// </summary>
    /// <param name="model">Analyzed structural model.</param>
    /// <param name="result">Analysis result for a single load case.</param>
    /// <param name="diagrams">Sampled internal-force diagrams.</param>
    /// <param name="summary">Analysis summary and extrema.</param>
    /// <param name="options">Optional report generation options.</param>
    /// <returns>Markdown report content.</returns>
    public string Generate(
        StructuralModel model,
        StructuralAnalysisResult result,
        IReadOnlyList<MemberInternalForceDiagram> diagrams,
        StructuralAnalysisSummary summary,
        MarkdownReportOptions? options = null) =>
        Generate(model, result, diagrams, Array.Empty<MemberDisplacementDiagram>(), summary, options);

    /// <summary>
    /// Generates a Markdown report from the model, analysis result, internal-force diagrams, displacement diagrams and summary.
    /// </summary>
    /// <param name="model">Analyzed structural model.</param>
    /// <param name="result">Analysis result for a single load case or load combination.</param>
    /// <param name="diagrams">Sampled internal-force diagrams.</param>
    /// <param name="displacementDiagrams">Sampled displacement/deformed-shape diagrams.</param>
    /// <param name="summary">Analysis summary and extrema.</param>
    /// <param name="options">Optional report generation options.</param>
    /// <returns>Markdown report content.</returns>
    public string Generate(
        StructuralModel model,
        StructuralAnalysisResult result,
        IReadOnlyList<MemberInternalForceDiagram> diagrams,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        StructuralAnalysisSummary summary,
        MarkdownReportOptions? options = null) =>
        Generate(model, result, diagrams, displacementDiagrams, Array.Empty<DeflectionCheckResult>(), summary, options);

    /// <summary>
    /// Generates a Markdown report from the model, analysis result, diagrams, preliminary deflection checks and summary.
    /// </summary>
    /// <param name="model">Analyzed structural model.</param>
    /// <param name="result">Analysis result for a single load case or load combination.</param>
    /// <param name="diagrams">Sampled internal-force diagrams.</param>
    /// <param name="displacementDiagrams">Sampled displacement/deformed-shape diagrams.</param>
    /// <param name="deflectionChecks">Optional preliminary serviceability deflection check results.</param>
    /// <param name="summary">Analysis summary and extrema.</param>
    /// <param name="options">Optional report generation options.</param>
    /// <returns>Markdown report content.</returns>
    public string Generate(
        StructuralModel model,
        StructuralAnalysisResult result,
        IReadOnlyList<MemberInternalForceDiagram> diagrams,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        IReadOnlyList<DeflectionCheckResult> deflectionChecks,
        StructuralAnalysisSummary summary,
        MarkdownReportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(diagrams);
        ArgumentNullException.ThrowIfNull(displacementDiagrams);
        ArgumentNullException.ThrowIfNull(deflectionChecks);
        ArgumentNullException.ThrowIfNull(summary);

        options ??= new MarkdownReportOptions();

        var builder = new StringBuilder();

        WriteHeader(builder, options, result);
        WriteEducationalGuide(builder, options);
        WriteUnits(builder);
        WriteExecutiveSummary(builder, model, summary, deflectionChecks, options);
        WriteModel(builder, model);
        WriteResults(builder, result, summary);
        WriteInternalForceDiagrams(builder, diagrams, options);
        WriteCharacteristicPoints(builder, diagrams, options);
        WriteDisplacementDiagrams(builder, displacementDiagrams, options);
        WriteDeflectionChecks(builder, deflectionChecks, options);
        WriteNotes(builder);

        return builder.ToString();
    }

    private static void WriteHeader(StringBuilder builder, MarkdownReportOptions options, StructuralAnalysisResult result)
    {
        builder.AppendLine($"# {Text(options.Title)}");
        builder.AppendLine();
        builder.AppendLine($"**Description:** {Text(options.Description)}");

        if (!string.IsNullOrWhiteSpace(options.SourceLabel))
        {
            builder.AppendLine($"**Source:** `{options.SourceLabel}`");
        }

        builder.AppendLine($"**Analysis id:** `{result.LoadCaseId}`");
        builder.AppendLine($"**Generated UTC:** {options.GeneratedAtUtc:yyyy-MM-dd HH:mm:ss}");
        builder.AppendLine();
    }

    private static void WriteEducationalGuide(StringBuilder builder, MarkdownReportOptions options)
    {
        if (!options.IncludeEducationalExplanations)
        {
            return;
        }

        builder.AppendLine("## How to read this report");
        builder.AppendLine();
        builder.AppendLine("This report is organized as a learning-oriented structural analysis note: first the assumptions and model data, then the numerical results, then sampled diagrams and checks.");
        builder.AppendLine();
        builder.AppendLine("Key conventions:");
        builder.AppendLine();
        builder.AppendLine("- `Ux` and `Uy` are global nodal translations.");
        builder.AppendLine("- `Rz` is the nodal rotation about the out-of-plane Z axis.");
        builder.AppendLine("- `N`, `V` and `M` are local member axial force, shear force and bending moment.");
        builder.AppendLine("- Member diagram positions are reported both as normalized position `0..1` and distance `x` from the start node.");
        builder.AppendLine("- Positive and negative signs follow the solver sign convention; for design interpretation, always check the model orientation and support layout.");
        builder.AppendLine();
    }

    private static void WriteExecutiveSummary(
        StringBuilder builder,
        StructuralModel model,
        StructuralAnalysisSummary summary,
        IReadOnlyList<DeflectionCheckResult> deflectionChecks,
        MarkdownReportOptions options)
    {
        if (!options.IncludeModelStatistics)
        {
            return;
        }

        builder.AppendLine("## Executive summary");
        builder.AppendLine();
        builder.AppendLine("### Model size");
        builder.AppendLine();
        builder.AppendLine("| Item | Count |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Nodes | {model.Nodes.Count} |");
        builder.AppendLine($"| Members | {model.Members.Count} |");
        builder.AppendLine($"| Supports | {model.Supports.Count} |");
        builder.AppendLine($"| Load cases | {model.LoadCases.Count} |");
        builder.AppendLine($"| Load combinations | {model.LoadCombinations.Count} |");
        builder.AppendLine($"| Loads | {model.Loads.Count} |");
        builder.AppendLine();

        builder.AppendLine("### Governing absolute values");
        builder.AppendLine();
        builder.AppendLine("| Result | Value | Location |");
        builder.AppendLine("|---|---:|---|");
        builder.AppendLine($"| Max \\|Ux\\| [m] | {Format(summary.MaxAbsUx.Value)} | node `{Display(summary.MaxAbsUx.EntityId)}` |");
        builder.AppendLine($"| Max \\|Uy\\| [m] | {Format(summary.MaxAbsUy.Value)} | node `{Display(summary.MaxAbsUy.EntityId)}` |");
        builder.AppendLine($"| Max \\|M\\| [kNm] | {Format(summary.MaxAbsBendingMoment.Value)} | member `{Display(summary.MaxAbsBendingMoment.MemberId)}`, x = {Format(summary.MaxAbsBendingMoment.Distance)} m |");
        builder.AppendLine();

        if (deflectionChecks.Count > 0)
        {
            int passing = deflectionChecks.Count(check => check.IsPass);
            int failing = deflectionChecks.Count - passing;
            DeflectionCheckResult governing = deflectionChecks
                .OrderByDescending(check => check.UtilizationRatio)
                .First();

            builder.AppendLine("### Preliminary deflection-check summary");
            builder.AppendLine();
            builder.AppendLine("| Item | Value |");
            builder.AppendLine("|---|---:|");
            builder.AppendLine($"| Checks passing | {passing} |");
            builder.AppendLine($"| Checks failing | {failing} |");
            builder.AppendLine($"| Governing utilization | {Format(governing.UtilizationRatio)} |");
            builder.AppendLine($"| Governing member | `{governing.MemberId}` |");
            builder.AppendLine();
        }
    }

    private static void WriteUnits(StringBuilder builder)
    {
        builder.AppendLine("## Units");
        builder.AppendLine();
        builder.AppendLine("StructuralSolver2D uses fixed coherent internal units:");
        builder.AppendLine();
        builder.AppendLine("| Quantity | Unit |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine("| Length | m |");
        builder.AppendLine("| Force | kN |");
        builder.AppendLine("| Moment | kNm |");
        builder.AppendLine("| Elastic modulus | kN/m² |");
        builder.AppendLine("| Area | m² |");
        builder.AppendLine("| Second moment of area | m⁴ |");
        builder.AppendLine();
    }

    private static void WriteModel(StringBuilder builder, StructuralModel model)
    {
        builder.AppendLine("## Model");
        builder.AppendLine();

        WriteNodes(builder, model.Nodes);
        WriteMaterials(builder, model.Materials);
        WriteSections(builder, model.Sections);
        WriteMembers(builder, model);
        WriteSupports(builder, model.Supports);
        WriteLoadCases(builder, model.LoadCases);
        WriteLoadCombinations(builder, model.LoadCombinations);
        WriteLoads(builder, model.Loads);
    }

    private static void WriteNodes(StringBuilder builder, IEnumerable<StructuralNode> nodes)
    {
        builder.AppendLine("### Nodes");
        builder.AppendLine();
        builder.AppendLine("| Id | X [m] | Y [m] | Label |");
        builder.AppendLine("|---|---:|---:|---|");

        foreach (StructuralNode node in nodes)
        {
            builder.AppendLine($"| `{node.Id}` | {Format(node.X)} | {Format(node.Y)} | {Text(node.Label)} |");
        }

        builder.AppendLine();
    }

    private static void WriteMaterials(StringBuilder builder, IEnumerable<StructuralMaterial> materials)
    {
        builder.AppendLine("### Materials");
        builder.AppendLine();
        builder.AppendLine("| Id | Name | E [kN/m²] | Unit weight [kN/m³] |");
        builder.AppendLine("|---|---|---:|---:|");

        foreach (StructuralMaterial material in materials)
        {
            builder.AppendLine($"| `{material.Id}` | {Text(material.Name)} | {Format(material.ElasticModulus)} | {FormatOptional(material.UnitWeight)} |");
        }

        builder.AppendLine();
    }

    private static void WriteSections(StringBuilder builder, IEnumerable<StructuralSection> sections)
    {
        builder.AppendLine("### Sections");
        builder.AppendLine();
        builder.AppendLine("| Id | Name | Area [m²] | I [m⁴] | Height [m] | Width [m] |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|");

        foreach (StructuralSection section in sections)
        {
            builder.AppendLine($"| `{section.Id}` | {Text(section.Name)} | {Format(section.Area)} | {Format(section.MomentOfInertia)} | {FormatOptional(section.Height)} | {FormatOptional(section.Width)} |");
        }

        builder.AppendLine();
    }

    private static void WriteMembers(StringBuilder builder, StructuralModel model)
    {
        var nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);

        builder.AppendLine("### Members");
        builder.AppendLine();
        builder.AppendLine("| Id | Type | Start | End | Material | Section | Length [m] | Start M release | End M release | Label |");
        builder.AppendLine("|---|---|---|---|---|---|---:|---:|---:|---|");

        foreach (StructuralMember member in model.Members)
        {
            string length = "-";
            if (nodes.TryGetValue(member.StartNodeId, out StructuralNode? startNode) &&
                nodes.TryGetValue(member.EndNodeId, out StructuralNode? endNode))
            {
                length = Format(StructuralMember.GetLength(startNode, endNode));
            }

            builder.AppendLine($"| `{member.Id}` | {member.Type} | `{member.StartNodeId}` | `{member.EndNodeId}` | `{member.MaterialId}` | `{member.SectionId}` | {length} | {Bool(member.ReleaseStartMoment)} | {Bool(member.ReleaseEndMoment)} | {Text(member.Label)} |");
        }

        builder.AppendLine();
    }

    private static void WriteSupports(StringBuilder builder, IEnumerable<StructuralSupport> supports)
    {
        builder.AppendLine("### Supports");
        builder.AppendLine();
        builder.AppendLine("| Id | Node | Type | Ux | Uy | Rz | Label |");
        builder.AppendLine("|---|---|---|---:|---:|---:|---|");

        foreach (StructuralSupport support in supports)
        {
            builder.AppendLine($"| `{support.Id}` | `{support.NodeId}` | {support.Type} | {Bool(support.RestrainedUx)} | {Bool(support.RestrainedUy)} | {Bool(support.RestrainedRz)} | {Text(support.Label)} |");
        }

        builder.AppendLine();
    }

    private static void WriteLoadCases(StringBuilder builder, IEnumerable<StructuralLoadCase> loadCases)
    {
        builder.AppendLine("### Load cases");
        builder.AppendLine();
        builder.AppendLine("| Id | Name | Description |");
        builder.AppendLine("|---|---|---|");

        foreach (StructuralLoadCase loadCase in loadCases)
        {
            builder.AppendLine($"| `{loadCase.Id}` | {Text(loadCase.Name)} | {Text(loadCase.Description)} |");
        }

        builder.AppendLine();
    }

    private static void WriteLoadCombinations(StringBuilder builder, IEnumerable<StructuralLoadCombination> combinations)
    {
        builder.AppendLine("### Load combinations");
        builder.AppendLine();
        builder.AppendLine("| Id | Name | Expression | Description |");
        builder.AppendLine("|---|---|---|---|");

        foreach (StructuralLoadCombination combination in combinations)
        {
            string expression = combination.Terms.Count == 0
                ? "-"
                : string.Join(" + ", combination.Terms.Select(term => $"{Format(term.Factor)} `{term.LoadCaseId}`"));

            builder.AppendLine($"| `{combination.Id}` | {Text(combination.Name)} | {expression} | {Text(combination.Description)} |");
        }

        builder.AppendLine();
    }

    private static void WriteLoads(StringBuilder builder, IEnumerable<StructuralLoad> loads)
    {
        builder.AppendLine("### Loads");
        builder.AppendLine();
        builder.AppendLine("| Id | Load case | Type | Target | Direction | Value | End value | Position | Label |");
        builder.AppendLine("|---|---|---|---|---|---:|---:|---:|---|");

        foreach (StructuralLoad load in loads)
        {
            string target = load.TargetType == Core.Model.Enums.StructuralLoadTargetType.Model
                ? "model"
                : $"{load.TargetType}: `{load.TargetId}`";

            builder.AppendLine($"| `{load.Id}` | `{load.LoadCaseId}` | {load.Type} | {target} | {load.Direction} | {Format(load.Value)} | {FormatOptional(load.EndValue)} | {FormatOptional(load.Position)} | {Text(load.Label)} |");
        }

        builder.AppendLine();
    }

    private static void WriteResults(StringBuilder builder, StructuralAnalysisResult result, StructuralAnalysisSummary summary)
    {
        builder.AppendLine("## Results");
        builder.AppendLine();

        WriteDisplacements(builder, result.Displacements);
        WriteReactions(builder, result.Reactions);
        WriteMemberEndForces(builder, result.MemberEndForces);
        WriteSummary(builder, summary);
    }

    private static void WriteDisplacements(StringBuilder builder, IReadOnlyList<NodalDisplacementResult> displacements)
    {
        builder.AppendLine("### Nodal displacements");
        builder.AppendLine();
        builder.AppendLine("| Node | Ux [m] | Uy [m] | Rz [rad] |");
        builder.AppendLine("|---|---:|---:|---:|");

        foreach (NodalDisplacementResult displacement in displacements)
        {
            builder.AppendLine($"| `{displacement.NodeId}` | {Format(displacement.Ux)} | {Format(displacement.Uy)} | {Format(displacement.Rz)} |");
        }

        builder.AppendLine();
    }

    private static void WriteReactions(StringBuilder builder, IReadOnlyList<SupportReactionResult> reactions)
    {
        builder.AppendLine("### Support reactions");
        builder.AppendLine();
        builder.AppendLine("| Support | Node | Fx [kN] | Fy [kN] | Mz [kNm] |");
        builder.AppendLine("|---|---|---:|---:|---:|");

        foreach (SupportReactionResult reaction in reactions)
        {
            builder.AppendLine($"| `{reaction.SupportId}` | `{reaction.NodeId}` | {Format(reaction.Fx)} | {Format(reaction.Fy)} | {Format(reaction.Mz)} |");
        }

        builder.AppendLine();
    }

    private static void WriteMemberEndForces(StringBuilder builder, IReadOnlyList<MemberEndForceResult> memberEndForces)
    {
        builder.AppendLine("### Local member end forces");
        builder.AppendLine();
        builder.AppendLine("| Member | N1 [kN] | V1 [kN] | M1 [kNm] | N2 [kN] | V2 [kN] | M2 [kNm] |");
        builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|");

        foreach (MemberEndForceResult force in memberEndForces)
        {
            builder.AppendLine($"| `{force.MemberId}` | {Format(force.StartAxial)} | {Format(force.StartShear)} | {Format(force.StartMoment)} | {Format(force.EndAxial)} | {Format(force.EndShear)} | {Format(force.EndMoment)} |");
        }

        builder.AppendLine();
    }

    private static void WriteSummary(StringBuilder builder, StructuralAnalysisSummary summary)
    {
        builder.AppendLine("### Maximum absolute results");
        builder.AppendLine();
        builder.AppendLine("| Quantity | Value | Location |");
        builder.AppendLine("|---|---:|---|");
        builder.AppendLine($"| Max \\|Ux\\| [m] | {Format(summary.MaxAbsUx.Value)} | node `{Display(summary.MaxAbsUx.EntityId)}` |");
        builder.AppendLine($"| Max \\|Uy\\| [m] | {Format(summary.MaxAbsUy.Value)} | node `{Display(summary.MaxAbsUy.EntityId)}` |");
        builder.AppendLine($"| Max \\|Rz\\| [rad] | {Format(summary.MaxAbsRz.Value)} | node `{Display(summary.MaxAbsRz.EntityId)}` |");
        builder.AppendLine($"| Max \\|Fx\\| [kN] | {Format(summary.MaxAbsReactionFx.Value)} | support `{Display(summary.MaxAbsReactionFx.EntityId)}` |");
        builder.AppendLine($"| Max \\|Fy\\| [kN] | {Format(summary.MaxAbsReactionFy.Value)} | support `{Display(summary.MaxAbsReactionFy.EntityId)}` |");
        builder.AppendLine($"| Max \\|Mz\\| [kNm] | {Format(summary.MaxAbsReactionMz.Value)} | support `{Display(summary.MaxAbsReactionMz.EntityId)}` |");
        builder.AppendLine($"| Max \\|N\\| [kN] | {Format(summary.MaxAbsNormalForce.Value)} | member `{Display(summary.MaxAbsNormalForce.MemberId)}`, x = {Format(summary.MaxAbsNormalForce.Distance)} m |");
        builder.AppendLine($"| Max \\|V\\| [kN] | {Format(summary.MaxAbsShearForce.Value)} | member `{Display(summary.MaxAbsShearForce.MemberId)}`, x = {Format(summary.MaxAbsShearForce.Distance)} m |");
        builder.AppendLine($"| Max \\|M\\| [kNm] | {Format(summary.MaxAbsBendingMoment.Value)} | member `{Display(summary.MaxAbsBendingMoment.MemberId)}`, x = {Format(summary.MaxAbsBendingMoment.Distance)} m |");
        builder.AppendLine();
    }

    private static void WriteInternalForceDiagrams(
        StringBuilder builder,
        IReadOnlyList<MemberInternalForceDiagram> diagrams,
        MarkdownReportOptions options)
    {
        builder.AppendLine("## Internal force diagrams");
        builder.AppendLine();

        if (diagrams.Count == 0)
        {
            builder.AppendLine("No sampled internal-force diagrams are available.");
            builder.AppendLine();
            return;
        }

        foreach (MemberInternalForceDiagram diagram in diagrams)
        {
            builder.AppendLine($"### Member `{diagram.MemberId}`");
            builder.AppendLine();
            builder.AppendLine($"Length: **{Format(diagram.Length)} m**");
            builder.AppendLine();

            if (!options.IncludeInternalForceSamples)
            {
                builder.AppendLine("Internal-force samples are omitted by report options.");
                builder.AppendLine();
                continue;
            }

            builder.AppendLine("| Position | x [m] | N [kN] | V [kN] | M [kNm] |");
            builder.AppendLine("|---:|---:|---:|---:|---:|");

            foreach (MemberInternalForceSample sample in diagram.Samples.Take(Math.Max(0, options.MaxSamplesPerMember)))
            {
                builder.AppendLine($"| {Format(sample.Position)} | {Format(sample.Distance)} | {Format(sample.NormalForce)} | {Format(sample.ShearForce)} | {Format(sample.BendingMoment)} |");
            }

            if (diagram.Samples.Count > options.MaxSamplesPerMember)
            {
                builder.AppendLine($"| ... | ... | ... | ... | ... |");
            }

            builder.AppendLine();
        }
    }


    private static void WriteCharacteristicPoints(
        StringBuilder builder,
        IReadOnlyList<MemberInternalForceDiagram> diagrams,
        MarkdownReportOptions options)
    {
        builder.AppendLine("## Characteristic internal-force points");
        builder.AppendLine();

        if (diagrams.Count == 0)
        {
            builder.AppendLine("No internal-force diagrams are available for characteristic point detection.");
            builder.AppendLine();
            return;
        }

        if (!options.IncludeCharacteristicPoints)
        {
            builder.AppendLine("Characteristic internal-force points are omitted by report options.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("The values below are detected from sampled N/V/M diagrams. Zero crossings between adjacent samples are linearly interpolated; exact analytical characteristic points may require dedicated closed-form post-processing for some load configurations.");
        builder.AppendLine();

        Frame2DInternalForceCharacteristicFinder finder = new();
        IReadOnlyList<MemberInternalForceCharacteristics> characteristics = finder.FindAll(diagrams);

        foreach (MemberInternalForceCharacteristics memberCharacteristics in characteristics)
        {
            builder.AppendLine($"### Member `{memberCharacteristics.MemberId}`");
            builder.AppendLine();
            builder.AppendLine($"Length: **{Format(memberCharacteristics.Length)} m**");
            builder.AppendLine();

            if (memberCharacteristics.Points.Count == 0)
            {
                builder.AppendLine("No characteristic points were detected.");
                builder.AppendLine();
                continue;
            }

            builder.AppendLine("| Kind | Quantity | Position | x [m] | Value | Description |");
            builder.AppendLine("|---|---|---:|---:|---:|---|");

            foreach (InternalForceCharacteristicPoint point in memberCharacteristics.Points.Take(Math.Max(0, options.MaxCharacteristicPointsPerMember)))
            {
                builder.AppendLine($"| {point.Kind} | {point.Quantity} | {Format(point.Position)} | {Format(point.Distance)} | {Format(point.Value)} | {Text(point.Description)} |");
            }

            if (memberCharacteristics.Points.Count > options.MaxCharacteristicPointsPerMember)
            {
                builder.AppendLine("| ... | ... | ... | ... | ... | ... |");
            }

            builder.AppendLine();
        }
    }

    private static void WriteDisplacementDiagrams(
        StringBuilder builder,
        IReadOnlyList<MemberDisplacementDiagram> displacementDiagrams,
        MarkdownReportOptions options)
    {
        builder.AppendLine("## Deformed shape samples");
        builder.AppendLine();

        if (displacementDiagrams.Count == 0)
        {
            builder.AppendLine("No sampled displacement diagrams are available.");
            builder.AppendLine();
            return;
        }

        if (!options.IncludeDisplacementSamples)
        {
            builder.AppendLine("Displacement samples are omitted by report options.");
            builder.AppendLine();
            return;
        }

        builder.AppendLine("The values below are finite-element interpolated displacements from nodal results. They are suitable for drawing the deformed shape; internal deflections under distributed loads are not always identical to closed-form beam solutions unless the checked point is explicitly modeled as a node.");
        builder.AppendLine();

        foreach (MemberDisplacementDiagram diagram in displacementDiagrams)
        {
            builder.AppendLine($"### Member `{diagram.MemberId}`");
            builder.AppendLine();
            builder.AppendLine($"Length: **{Format(diagram.Length)} m**");
            builder.AppendLine();
            builder.AppendLine("| Position | x [m] | u local [m] | v local [m] | rz local [rad] | Ux global [m] | Uy global [m] |");
            builder.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");

            foreach (MemberDisplacementSample sample in diagram.Samples.Take(Math.Max(0, options.MaxDisplacementSamplesPerMember)))
            {
                builder.AppendLine($"| {Format(sample.NormalizedPosition)} | {Format(sample.Distance)} | {Format(sample.LocalUx)} | {Format(sample.LocalUy)} | {Format(sample.LocalRz)} | {Format(sample.GlobalUx)} | {Format(sample.GlobalUy)} |");
            }

            if (diagram.Samples.Count > options.MaxDisplacementSamplesPerMember)
            {
                builder.AppendLine("| ... | ... | ... | ... | ... | ... | ... |");
            }

            builder.AppendLine();
        }
    }

    private static void WriteDeflectionChecks(
        StringBuilder builder,
        IReadOnlyList<DeflectionCheckResult> deflectionChecks,
        MarkdownReportOptions options)
    {
        builder.AppendLine("## Preliminary serviceability deflection checks");
        builder.AppendLine();

        if (!options.IncludeDeflectionChecks)
        {
            builder.AppendLine("Preliminary deflection checks are omitted by report options.");
            builder.AppendLine();
            return;
        }

        if (deflectionChecks.Count == 0)
        {
            builder.AppendLine("No preliminary deflection checks were supplied.");
            builder.AppendLine();
            return;
        }

        if (options.IncludeEducationalExplanations)
        {
            builder.AppendLine("These checks compare the maximum sampled displacement component with a simple limit of the form `L / denominator`. They are useful as an early serviceability indicator, but they are not a complete normative SLE verification.");
            builder.AppendLine();
        }

        builder.AppendLine("| Member | Direction | Limit | L [m] | Allowed [m] | Max abs [m] | Signed critical [m] | x [m] | Utilization | Status |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|---:|---|");

        foreach (DeflectionCheckResult check in deflectionChecks.OrderByDescending(check => check.UtilizationRatio))
        {
            builder.AppendLine($"| `{check.MemberId}` | {check.Direction} | L/{Format(check.LimitDenominator)} | {Format(check.ReferenceLength)} | {Format(check.AllowedDeflection)} | {Format(check.MaxAbsDeflection)} | {Format(check.SignedDeflectionAtCriticalSample)} | {Format(check.Distance)} | {Format(check.UtilizationRatio)} | {check.Status} |");
        }

        builder.AppendLine();
    }

    private static void WriteNotes(StringBuilder builder)
    {
        builder.AppendLine("## Notes and limitations");
        builder.AppendLine();
        builder.AppendLine("- The report is generated from a 2D frame linear static analysis result.");
        builder.AppendLine("- Internal forces and characteristic points are derived from sampled values; exact extrema may require analytical post-processing for some load configurations.");
        builder.AppendLine("- Deformed-shape samples use finite-element interpolation of nodal displacements; for benchmark deflection checks, critical points should be modeled as explicit nodes when closed-form comparison is required.");
        builder.AppendLine("- The current scope is analysis of simple plane structural schemes, not complete normative design verification.");
        builder.AppendLine("- Results must be checked by a qualified professional before any practical engineering use.");
        builder.AppendLine();
    }

    private static string Format(double value) =>
        value.ToString("0.000000", CultureInfo.InvariantCulture);

    private static string FormatOptional(double? value) =>
        value.HasValue ? Format(value.Value) : "-";

    private static string Bool(bool value) => value ? "yes" : "no";

    private static string Display(string value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static string Text(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
