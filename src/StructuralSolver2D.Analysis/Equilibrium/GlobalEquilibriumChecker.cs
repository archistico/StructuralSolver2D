using StructuralSolver2D.Analysis;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Analysis.Equilibrium;

/// <summary>
/// Checks the global static equilibrium of an analysis result.
/// The checker sums external loads and support reactions and verifies that the residuals are close to zero.
/// </summary>
public sealed class GlobalEquilibriumChecker
{
    /// <summary>
    /// Computes the global equilibrium residuals for the supplied model and analysis result.
    /// The analyzed load case or load combination is inferred from <see cref="StructuralAnalysisResult.LoadCaseId"/>.
    /// </summary>
    /// <param name="model">Structural model used for the analysis.</param>
    /// <param name="result">Analysis result to check.</param>
    /// <param name="referenceX">X coordinate of the moment reference point, in meters.</param>
    /// <param name="referenceY">Y coordinate of the moment reference point, in meters.</param>
    /// <returns>Global equilibrium result.</returns>
    public GlobalEquilibriumResult Check(
        StructuralModel model,
        StructuralAnalysisResult result,
        double referenceX = 0.0,
        double referenceY = 0.0)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);

        IReadOnlyDictionary<string, double> loadCaseFactors = BuildLoadCaseFactors(model, result.LoadCaseId);
        Dictionary<string, StructuralNode> nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralMember> members = model.Members.ToDictionary(member => member.Id, StringComparer.OrdinalIgnoreCase);

        EquilibriumAccumulator applied = new();
        foreach (StructuralLoad load in model.Loads.Where(load => loadCaseFactors.ContainsKey(load.LoadCaseId)))
        {
            AddLoad(model, nodes, members, load, loadCaseFactors[load.LoadCaseId], referenceX, referenceY, applied);
        }

        EquilibriumAccumulator reactions = new();
        foreach (SupportReactionResult reaction in result.Reactions)
        {
            if (!nodes.TryGetValue(reaction.NodeId, out StructuralNode? node))
            {
                throw new StructuralAnalysisException(
                    $"Reaction '{reaction.SupportId}' references node '{reaction.NodeId}', which was not found in the structural model.");
            }

            AddForceAndMoment(
                reactions,
                reaction.Fx,
                reaction.Fy,
                reaction.Mz,
                node.X,
                node.Y,
                referenceX,
                referenceY);
        }

        return new GlobalEquilibriumResult(
            result.LoadCaseId,
            referenceX,
            referenceY,
            applied.Fx,
            applied.Fy,
            applied.Mz,
            reactions.Fx,
            reactions.Fy,
            reactions.Mz);
    }

    private static IReadOnlyDictionary<string, double> BuildLoadCaseFactors(StructuralModel model, string resultId)
    {
        StructuralLoadCombination? combination = model.LoadCombinations.FirstOrDefault(
            combination => string.Equals(combination.Id, resultId, StringComparison.OrdinalIgnoreCase));

        if (combination is not null)
        {
            return combination.Terms.ToDictionary(
                term => term.LoadCaseId,
                term => term.Factor,
                StringComparer.OrdinalIgnoreCase);
        }

        if (model.LoadCases.Any(loadCase => string.Equals(loadCase.Id, resultId, StringComparison.OrdinalIgnoreCase)))
        {
            return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                [resultId] = 1.0,
            };
        }

        throw new StructuralAnalysisException(
            $"Cannot check global equilibrium because '{resultId}' is neither a load case nor a load combination in the structural model.");
    }

    private static void AddLoad(
        StructuralModel model,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMember> members,
        StructuralLoad load,
        double factor,
        double referenceX,
        double referenceY,
        EquilibriumAccumulator accumulator)
    {
        switch (load.Type)
        {
            case StructuralLoadType.NodalForce:
                AddNodalForce(nodes, load, factor, referenceX, referenceY, accumulator);
                break;

            case StructuralLoadType.NodalMoment:
                AddNodalMoment(nodes, load, factor, referenceX, referenceY, accumulator);
                break;

            case StructuralLoadType.PointLoadOnMember:
                AddPointLoadOnMember(nodes, members, load, factor, referenceX, referenceY, accumulator);
                break;

            case StructuralLoadType.UniformDistributedLoad:
                AddUniformDistributedLoad(nodes, members, load, factor, referenceX, referenceY, accumulator);
                break;

            case StructuralLoadType.LinearDistributedLoad:
                AddLinearDistributedLoad(nodes, members, load, factor, referenceX, referenceY, accumulator);
                break;

            case StructuralLoadType.SelfWeight:
                throw new StructuralAnalysisException("Self-weight loads are not supported by the global equilibrium checker yet.");

            default:
                throw new StructuralAnalysisException($"Unsupported load type '{load.Type}' in global equilibrium checker.");
        }
    }

    private static void AddNodalForce(
        Dictionary<string, StructuralNode> nodes,
        StructuralLoad load,
        double factor,
        double referenceX,
        double referenceY,
        EquilibriumAccumulator accumulator)
    {
        StructuralNode node = GetNode(nodes, load.TargetId, load.Id);
        (double fx, double fy) = load.Direction switch
        {
            StructuralLoadDirection.GlobalX => (load.Value * factor, 0.0),
            StructuralLoadDirection.GlobalY => (0.0, load.Value * factor),
            _ => throw new StructuralAnalysisException($"Nodal force '{load.Id}' has unsupported direction '{load.Direction}'.")
        };

        AddForceAndMoment(accumulator, fx, fy, 0.0, node.X, node.Y, referenceX, referenceY);
    }

    private static void AddNodalMoment(
        Dictionary<string, StructuralNode> nodes,
        StructuralLoad load,
        double factor,
        double referenceX,
        double referenceY,
        EquilibriumAccumulator accumulator)
    {
        StructuralNode node = GetNode(nodes, load.TargetId, load.Id);
        AddForceAndMoment(accumulator, 0.0, 0.0, load.Value * factor, node.X, node.Y, referenceX, referenceY);
    }

    private static void AddPointLoadOnMember(
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMember> members,
        StructuralLoad load,
        double factor,
        double referenceX,
        double referenceY,
        EquilibriumAccumulator accumulator)
    {
        if (!load.Position.HasValue)
        {
            throw new StructuralAnalysisException($"Point load '{load.Id}' has no normalized position.");
        }

        (StructuralNode startNode, StructuralNode endNode, MemberGeometry geometry) = GetMemberGeometry(nodes, members, load.TargetId, load.Id);
        (double fx, double fy) = ResolveLoadValueInGlobalCoordinates(load.Direction, load.Value * factor, geometry);
        double positionAlongMember = geometry.Length * load.Position.Value;
        double x = startNode.X + (geometry.Cosine * positionAlongMember);
        double y = startNode.Y + (geometry.Sine * positionAlongMember);

        AddForceAndMoment(accumulator, fx, fy, 0.0, x, y, referenceX, referenceY);
    }

    private static void AddUniformDistributedLoad(
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMember> members,
        StructuralLoad load,
        double factor,
        double referenceX,
        double referenceY,
        EquilibriumAccumulator accumulator)
    {
        var (startNode, _, geometry) = GetMemberGeometry(nodes, members, load.TargetId, load.Id);
        (double startFx, double startFy) = ResolveLoadValueInGlobalCoordinates(load.Direction, load.Value * factor, geometry);

        AddLinearLoadResultant(
            accumulator,
            startNode,
            geometry,
            startFx,
            startFy,
            startFx,
            startFy,
            referenceX,
            referenceY);
    }

    private static void AddLinearDistributedLoad(
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMember> members,
        StructuralLoad load,
        double factor,
        double referenceX,
        double referenceY,
        EquilibriumAccumulator accumulator)
    {
        if (!load.EndValue.HasValue)
        {
            throw new StructuralAnalysisException($"Linear distributed load '{load.Id}' has no end value.");
        }

        var (startNode, _, geometry) = GetMemberGeometry(nodes, members, load.TargetId, load.Id);
        (double startFx, double startFy) = ResolveLoadValueInGlobalCoordinates(load.Direction, load.Value * factor, geometry);
        (double endFx, double endFy) = ResolveLoadValueInGlobalCoordinates(load.Direction, load.EndValue.Value * factor, geometry);

        AddLinearLoadResultant(
            accumulator,
            startNode,
            geometry,
            startFx,
            startFy,
            endFx,
            endFy,
            referenceX,
            referenceY);
    }

    private static void AddLinearLoadResultant(
        EquilibriumAccumulator accumulator,
        StructuralNode startNode,
        MemberGeometry geometry,
        double startFx,
        double startFy,
        double endFx,
        double endFy,
        double referenceX,
        double referenceY)
    {
        double totalFx = geometry.Length * (startFx + endFx) / 2.0;
        double totalFy = geometry.Length * (startFy + endFy) / 2.0;
        double firstMomentFxAboutStart = geometry.Length * geometry.Length * (startFx + (2.0 * endFx)) / 6.0;
        double firstMomentFyAboutStart = geometry.Length * geometry.Length * (startFy + (2.0 * endFy)) / 6.0;

        accumulator.Fx += totalFx;
        accumulator.Fy += totalFy;
        accumulator.Mz += ((startNode.X - referenceX) * totalFy) +
                          (geometry.Cosine * firstMomentFyAboutStart) -
                          ((startNode.Y - referenceY) * totalFx) -
                          (geometry.Sine * firstMomentFxAboutStart);
    }

    private static (double Fx, double Fy) ResolveLoadValueInGlobalCoordinates(
        StructuralLoadDirection direction,
        double value,
        MemberGeometry geometry) =>
        direction switch
        {
            StructuralLoadDirection.GlobalX => (value, 0.0),
            StructuralLoadDirection.GlobalY => (0.0, value),
            StructuralLoadDirection.LocalX => (geometry.Cosine * value, geometry.Sine * value),
            StructuralLoadDirection.LocalY => (-geometry.Sine * value, geometry.Cosine * value),
            _ => throw new StructuralAnalysisException($"Unsupported member load direction '{direction}'.")
        };

    private static void AddForceAndMoment(
        EquilibriumAccumulator accumulator,
        double fx,
        double fy,
        double mz,
        double x,
        double y,
        double referenceX,
        double referenceY)
    {
        accumulator.Fx += fx;
        accumulator.Fy += fy;
        accumulator.Mz += mz + ((x - referenceX) * fy) - ((y - referenceY) * fx);
    }

    private static StructuralNode GetNode(Dictionary<string, StructuralNode> nodes, string nodeId, string loadId) =>
        nodes.TryGetValue(nodeId, out StructuralNode? node)
            ? node
            : throw new StructuralAnalysisException($"Load '{loadId}' targets node '{nodeId}', which was not found.");

    private static (StructuralNode StartNode, StructuralNode EndNode, MemberGeometry Geometry) GetMemberGeometry(
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMember> members,
        string memberId,
        string loadId)
    {
        if (!members.TryGetValue(memberId, out StructuralMember? member))
        {
            throw new StructuralAnalysisException($"Load '{loadId}' targets member '{memberId}', which was not found.");
        }

        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        return (startNode, endNode, MemberGeometry.FromNodes(startNode, endNode));
    }

    private sealed class EquilibriumAccumulator
    {
        public double Fx { get; set; }
        public double Fy { get; set; }
        public double Mz { get; set; }
    }

    private sealed record MemberGeometry(double Length, double Cosine, double Sine)
    {
        public static MemberGeometry FromNodes(StructuralNode startNode, StructuralNode endNode)
        {
            double dx = endNode.X - startNode.X;
            double dy = endNode.Y - startNode.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));

            if (length <= 0.0)
            {
                throw new StructuralAnalysisException($"Member from node '{startNode.Id}' to node '{endNode.Id}' has zero length.");
            }

            return new MemberGeometry(length, dx / length, dy / length);
        }
    }
}
