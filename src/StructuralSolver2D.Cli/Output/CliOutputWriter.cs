using System.Globalization;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Cli.Examples;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Cli.Output;

/// <summary>
/// Writes human-readable command-line output for the minimal CLI.
/// </summary>
public static class CliOutputWriter
{
    /// <summary>
    /// Writes command usage and available examples.
    /// </summary>
    public static void WriteHelp(string applicationName, IReadOnlyList<CliExampleInfo> examples)
    {
        Console.WriteLine(applicationName);
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project src/StructuralSolver2D.Cli -- example <name>");
        Console.WriteLine("  dotnet run --project src/StructuralSolver2D.Cli -- analyze <file.json> [loadCaseId|combinationId]");
        Console.WriteLine("  dotnet run --project src/StructuralSolver2D.Cli -- report <file.json> <output.md> [loadCaseId|combinationId]");
        Console.WriteLine();
        WriteAvailableExamples(examples);
    }

    /// <summary>
    /// Writes all available examples.
    /// </summary>
    public static void WriteAvailableExamples(IReadOnlyList<CliExampleInfo> examples)
    {
        Console.WriteLine("Available examples:");
        foreach (CliExampleInfo example in examples)
        {
            Console.WriteLine($"  {example.Name,-28} {example.Title}");
            Console.WriteLine($"  {new string(' ', 28)} {example.Description}");
        }
    }

    /// <summary>
    /// Writes a completed analysis result for a built-in example.
    /// </summary>
    public static void WriteAnalysisResult(
        string applicationName,
        CliExample example,
        StructuralAnalysisResult result,
        StructuralAnalysisSummary summary) =>
        WriteAnalysisResult(
            applicationName,
            $"Example: {example.Name}",
            example.Title,
            example.Description,
            result,
            summary);

    /// <summary>
    /// Writes a completed analysis result.
    /// </summary>
    public static void WriteAnalysisResult(
        string applicationName,
        string sourceLabel,
        string title,
        string description,
        StructuralAnalysisResult result,
        StructuralAnalysisSummary summary)
    {
        Console.WriteLine(applicationName);
        Console.WriteLine();
        Console.WriteLine(sourceLabel);
        Console.WriteLine($"Title:   {title}");
        Console.WriteLine($"Scheme:  {description}");
        Console.WriteLine($"Analysis id: {result.LoadCaseId}");
        Console.WriteLine();
        Console.WriteLine("Analysis completed.");
        Console.WriteLine();

        WriteDisplacements(result.Displacements);
        Console.WriteLine();
        WriteReactions(result.Reactions);
        Console.WriteLine();
        WriteMemberEndForces(result.MemberEndForces);
        Console.WriteLine();
        WriteSummary(summary);
    }

    /// <summary>
    /// Writes an error message.
    /// </summary>
    public static void WriteError(string message)
    {
        Console.Error.WriteLine("Error:");
        Console.Error.WriteLine($"  {message}");
    }

    /// <summary>
    /// Writes validation issues returned by the model validator.
    /// </summary>
    public static void WriteValidationIssues(IReadOnlyList<StructuralModelValidationIssue> issues)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Validation issues:");
        foreach (StructuralModelValidationIssue issue in issues)
        {
            Console.Error.WriteLine($"  [{issue.Severity}] {issue.Code}: {issue.Message}");
        }
    }

    private static void WriteDisplacements(IReadOnlyList<NodalDisplacementResult> displacements)
    {
        Console.WriteLine("Displacements:");
        Console.WriteLine("  Node           Ux [m]          Uy [m]        Rz [rad]");
        foreach (NodalDisplacementResult displacement in displacements)
        {
            Console.WriteLine(
                $"  {displacement.NodeId,-6} {Format(displacement.Ux),14} {Format(displacement.Uy),14} {Format(displacement.Rz),14}");
        }
    }

    private static void WriteReactions(IReadOnlyList<SupportReactionResult> reactions)
    {
        Console.WriteLine("Reactions:");
        Console.WriteLine("  Support Node        Fx [kN]        Fy [kN]       Mz [kNm]");
        foreach (SupportReactionResult reaction in reactions)
        {
            Console.WriteLine(
                $"  {reaction.SupportId,-7} {reaction.NodeId,-6} {Format(reaction.Fx),14} {Format(reaction.Fy),14} {Format(reaction.Mz),14}");
        }
    }

    private static void WriteMemberEndForces(IReadOnlyList<MemberEndForceResult> memberEndForces)
    {
        Console.WriteLine("Local member end forces:");
        Console.WriteLine("  Member      N1 [kN]       V1 [kN]      M1 [kNm]       N2 [kN]       V2 [kN]      M2 [kNm]");
        foreach (MemberEndForceResult force in memberEndForces)
        {
            Console.WriteLine(
                $"  {force.MemberId,-6} {Format(force.StartAxial),14} {Format(force.StartShear),14} {Format(force.StartMoment),14} {Format(force.EndAxial),14} {Format(force.EndShear),14} {Format(force.EndMoment),14}");
        }
    }

    private static void WriteSummary(StructuralAnalysisSummary summary)
    {
        Console.WriteLine("Maximum absolute results:");
        Console.WriteLine($"  Max |Ux| = {Format(summary.MaxAbsUx.Value)} m       at node {DisplayEntity(summary.MaxAbsUx.EntityId)}");
        Console.WriteLine($"  Max |Uy| = {Format(summary.MaxAbsUy.Value)} m       at node {DisplayEntity(summary.MaxAbsUy.EntityId)}");
        Console.WriteLine($"  Max |Rz| = {Format(summary.MaxAbsRz.Value)} rad     at node {DisplayEntity(summary.MaxAbsRz.EntityId)}");
        Console.WriteLine($"  Max |Fx| = {Format(summary.MaxAbsReactionFx.Value)} kN      at support {DisplayEntity(summary.MaxAbsReactionFx.EntityId)}");
        Console.WriteLine($"  Max |Fy| = {Format(summary.MaxAbsReactionFy.Value)} kN      at support {DisplayEntity(summary.MaxAbsReactionFy.EntityId)}");
        Console.WriteLine($"  Max |Mz| = {Format(summary.MaxAbsReactionMz.Value)} kNm     at support {DisplayEntity(summary.MaxAbsReactionMz.EntityId)}");
        Console.WriteLine($"  Max |N|  = {Format(summary.MaxAbsNormalForce.Value)} kN      at member {DisplayEntity(summary.MaxAbsNormalForce.MemberId)}, x = {Format(summary.MaxAbsNormalForce.Distance)} m");
        Console.WriteLine($"  Max |V|  = {Format(summary.MaxAbsShearForce.Value)} kN      at member {DisplayEntity(summary.MaxAbsShearForce.MemberId)}, x = {Format(summary.MaxAbsShearForce.Distance)} m");
        Console.WriteLine($"  Max |M|  = {Format(summary.MaxAbsBendingMoment.Value)} kNm     at member {DisplayEntity(summary.MaxAbsBendingMoment.MemberId)}, x = {Format(summary.MaxAbsBendingMoment.Distance)} m");
    }

    private static string Format(double value) =>
        value.ToString("0.000000", CultureInfo.InvariantCulture);

    private static string DisplayEntity(string entityId) =>
        string.IsNullOrWhiteSpace(entityId) ? "-" : entityId;
}
