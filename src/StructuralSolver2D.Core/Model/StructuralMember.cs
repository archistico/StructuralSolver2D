using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents a one-dimensional structural member between two nodes.
/// </summary>
/// <param name="Id">Unique member identifier.</param>
/// <param name="StartNodeId">Identifier of the start node.</param>
/// <param name="EndNodeId">Identifier of the end node.</param>
/// <param name="MaterialId">Identifier of the assigned material.</param>
/// <param name="SectionId">Identifier of the assigned section.</param>
/// <param name="Type">Structural member type.</param>
/// <param name="Label">Optional user-facing label.</param>
public sealed record StructuralMember(
    string Id,
    string StartNodeId,
    string EndNodeId,
    string MaterialId,
    string SectionId,
    MemberType Type = MemberType.Frame2D,
    string? Label = null)
{
    /// <summary>
    /// Computes the member length from its end nodes.
    /// Internal length unit: meter [m].
    /// </summary>
    /// <param name="startNode">Start node.</param>
    /// <param name="endNode">End node.</param>
    /// <returns>Member length in meters.</returns>
    public static double GetLength(StructuralNode startNode, StructuralNode endNode)
    {
        double dx = endNode.X - startNode.X;
        double dy = endNode.Y - startNode.Y;

        return Math.Sqrt((dx * dx) + (dy * dy));
    }
}
