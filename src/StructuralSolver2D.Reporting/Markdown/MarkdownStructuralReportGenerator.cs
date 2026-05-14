using System.Globalization;
using System.Text;
using StructuralSolver2D.Analysis.Results;
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
        MarkdownReportOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(diagrams);
        ArgumentNullException.ThrowIfNull(displacementDiagrams);
        ArgumentNullException.ThrowIfNull(summary);

        options ??= new MarkdownReportOptions();

        var builder = new StringBuilder();

        WriteHeader(builder, options, result);
        WriteUnits(builder);
        WriteModel(builder, model);
        WriteResults(builder, result, summary);
        WriteInternalForceDiagrams(builder, diagrams, options);
        WriteDisplacementDiagrams(builder, displacementDiagrams, options);
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

    private static void WriteNotes(StringBuilder builder)
    {
        builder.AppendLine("## Notes and limitations");
        builder.AppendLine();
        builder.AppendLine("- The report is generated from a 2D frame linear static analysis result.");
        builder.AppendLine("- Internal forces are sampled values; exact extrema may require analytical post-processing for some load configurations.");
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
