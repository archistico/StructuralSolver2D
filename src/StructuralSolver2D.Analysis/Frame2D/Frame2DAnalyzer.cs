using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Solvers;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Frame2D;

/// <summary>
/// Performs a first-order linear elastic analysis of a plane structure made of 2D frame elements.
/// Each node has three degrees of freedom: Ux, Uy and Rz.
/// </summary>
public sealed class Frame2DAnalyzer
{
    private const int DofsPerNode = 3;

    /// <summary>
    /// Analyzes the supplied structural model for one load case.
    /// </summary>
    /// <param name="model">Structural model to analyze.</param>
    /// <param name="loadCaseId">Load case identifier.</param>
    /// <returns>Analysis result.</returns>
    /// <exception cref="StructuralAnalysisException">Thrown when the model cannot be analyzed.</exception>
    public StructuralAnalysisResult Analyze(StructuralModel model, string loadCaseId)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(loadCaseId))
        {
            throw new ArgumentException("Load case id cannot be empty.", nameof(loadCaseId));
        }

        ValidateModel(model);

        if (!model.LoadCases.Any(loadCase => string.Equals(loadCase.Id, loadCaseId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new StructuralAnalysisException($"Load case '{loadCaseId}' was not found in the structural model.");
        }

        if (model.Members.Any(member => member.Type != MemberType.Frame2D))
        {
            throw new StructuralAnalysisException("The current analyzer supports only Frame2D members.");
        }

        Dictionary<string, int> nodeIndexById = BuildNodeIndex(model);
        int totalDofCount = model.Nodes.Count * DofsPerNode;
        double[,] globalStiffness = new double[totalDofCount, totalDofCount];
        double[] globalLoadVector = new double[totalDofCount];
        Dictionary<string, double[]> memberEquivalentLocalLoads = new(StringComparer.OrdinalIgnoreCase);

        AssembleGlobalStiffness(model, nodeIndexById, globalStiffness);
        AssembleLoads(model, loadCaseId, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads);

        bool[] restrainedDofs = BuildRestrainedDofMask(model, nodeIndexById, totalDofCount);
        List<int> freeDofs = Enumerable.Range(0, totalDofCount).Where(index => !restrainedDofs[index]).ToList();

        if (freeDofs.Count == 0)
        {
            throw new StructuralAnalysisException("The model has no free degrees of freedom to solve.");
        }

        double[,] reducedStiffness = ExtractSubmatrix(globalStiffness, freeDofs, freeDofs);
        double[] reducedLoadVector = ExtractSubvector(globalLoadVector, freeDofs);
        double[] reducedDisplacements = DenseLinearSystemSolver.Solve(reducedStiffness, reducedLoadVector);
        double[] globalDisplacements = new double[totalDofCount];

        for (int index = 0; index < freeDofs.Count; index++)
        {
            globalDisplacements[freeDofs[index]] = reducedDisplacements[index];
        }

        double[] globalResidual = Subtract(Multiply(globalStiffness, globalDisplacements), globalLoadVector);

        return new StructuralAnalysisResult(
            loadCaseId,
            BuildNodalDisplacementResults(model, nodeIndexById, globalDisplacements),
            BuildSupportReactionResults(model, nodeIndexById, globalResidual),
            BuildMemberEndForceResults(model, nodeIndexById, globalDisplacements, memberEquivalentLocalLoads));
    }

    private static void ValidateModel(StructuralModel model)
    {
        StructuralModelValidationResult validationResult = new StructuralModelValidator().Validate(model);
        if (!validationResult.IsValid)
        {
            throw new StructuralAnalysisException("The structural model is not valid and cannot be analyzed.", validationResult.Issues);
        }
    }

    private static Dictionary<string, int> BuildNodeIndex(StructuralModel model) =>
        model.Nodes
            .Select((node, index) => new { node.Id, Index = index })
            .ToDictionary(item => item.Id, item => item.Index, StringComparer.OrdinalIgnoreCase);

    private static void AssembleGlobalStiffness(
        StructuralModel model,
        Dictionary<string, int> nodeIndexById,
        double[,] globalStiffness)
    {
        Dictionary<string, StructuralNode> nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralMaterial> materials = model.Materials.ToDictionary(material => material.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralSection> sections = model.Sections.ToDictionary(section => section.Id, StringComparer.OrdinalIgnoreCase);

        foreach (StructuralMember member in model.Members)
        {
            StructuralNode startNode = nodes[member.StartNodeId];
            StructuralNode endNode = nodes[member.EndNodeId];
            StructuralMaterial material = materials[member.MaterialId];
            StructuralSection section = sections[member.SectionId];
            MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);

            double[,] localStiffness = Frame2DElementMatrices.BuildLocalStiffness(
                material.ElasticModulus,
                section.Area,
                section.MomentOfInertia,
                geometry.Length);

            double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
            double[,] globalElementStiffness = Frame2DElementMatrices.TransformStiffnessToGlobal(localStiffness, transformation);
            int[] dofs = GetMemberDofs(member, nodeIndexById);

            AddElementMatrix(globalStiffness, globalElementStiffness, dofs);
        }
    }

    private static void AssembleLoads(
        StructuralModel model,
        string loadCaseId,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
    {
        Dictionary<string, StructuralMember> members = model.Members.ToDictionary(member => member.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralNode> nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);

        foreach (StructuralLoad load in model.Loads.Where(load => string.Equals(load.LoadCaseId, loadCaseId, StringComparison.OrdinalIgnoreCase)))
        {
            switch (load.Type)
            {
                case StructuralLoadType.NodalForce:
                    AddNodalForce(load, nodeIndexById, globalLoadVector);
                    break;

                case StructuralLoadType.NodalMoment:
                    AddNodalMoment(load, nodeIndexById, globalLoadVector);
                    break;

                case StructuralLoadType.UniformDistributedLoad:
                    AddUniformDistributedLoad(load, members, nodes, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads);
                    break;

                case StructuralLoadType.PointLoadOnMember:
                    AddPointLoadOnMember(load, members, nodes, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads);
                    break;

                case StructuralLoadType.SelfWeight:
                    throw new StructuralAnalysisException("Self-weight loads are not supported by this first Frame2D analysis milestone.");
            }
        }
    }

    private static void AddNodalForce(
        StructuralLoad load,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector)
    {
        int nodeBaseDof = GetNodeBaseDof(nodeIndexById[load.TargetId]);

        if (load.Direction == StructuralLoadDirection.GlobalX)
        {
            globalLoadVector[nodeBaseDof] += load.Value;
        }
        else if (load.Direction == StructuralLoadDirection.GlobalY)
        {
            globalLoadVector[nodeBaseDof + 1] += load.Value;
        }
        else
        {
            throw new StructuralAnalysisException($"Unsupported nodal force direction '{load.Direction}'.");
        }
    }

    private static void AddNodalMoment(
        StructuralLoad load,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector)
    {
        int nodeBaseDof = GetNodeBaseDof(nodeIndexById[load.TargetId]);
        globalLoadVector[nodeBaseDof + 2] += load.Value;
    }

    private static void AddUniformDistributedLoad(
        StructuralLoad load,
        Dictionary<string, StructuralMember> members,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
    {
        StructuralMember member = members[load.TargetId];
        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);

        (double localXValue, double localYValue) = ResolveUniformLoadInLocalCoordinates(load, geometry);
        double[] localLoad = Frame2DElementMatrices.BuildUniformLocalLoad(localXValue, localYValue, geometry.Length);
        double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
        double[] globalLoad = Frame2DElementMatrices.TransformLoadToGlobal(localLoad, transformation);
        int[] dofs = GetMemberDofs(member, nodeIndexById);

        AddElementVector(globalLoadVector, globalLoad, dofs);

        if (!memberEquivalentLocalLoads.TryGetValue(member.Id, out double[]? existingLocalLoad))
        {
            existingLocalLoad = new double[6];
            memberEquivalentLocalLoads[member.Id] = existingLocalLoad;
        }

        for (int index = 0; index < localLoad.Length; index++)
        {
            existingLocalLoad[index] += localLoad[index];
        }
    }


    private static void AddPointLoadOnMember(
        StructuralLoad load,
        Dictionary<string, StructuralMember> members,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
    {
        if (!load.Position.HasValue)
        {
            throw new StructuralAnalysisException($"Point load '{load.Id}' has no normalized position.");
        }

        StructuralMember member = members[load.TargetId];
        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);

        (double localXValue, double localYValue) = ResolveConcentratedLoadInLocalCoordinates(load, geometry);
        double[] localLoad = Frame2DElementMatrices.BuildPointLocalLoad(localXValue, localYValue, geometry.Length, load.Position.Value);
        double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
        double[] globalLoad = Frame2DElementMatrices.TransformLoadToGlobal(localLoad, transformation);
        int[] dofs = GetMemberDofs(member, nodeIndexById);

        AddElementVector(globalLoadVector, globalLoad, dofs);

        if (!memberEquivalentLocalLoads.TryGetValue(member.Id, out double[]? existingLocalLoad))
        {
            existingLocalLoad = new double[6];
            memberEquivalentLocalLoads[member.Id] = existingLocalLoad;
        }

        for (int index = 0; index < localLoad.Length; index++)
        {
            existingLocalLoad[index] += localLoad[index];
        }
    }

    private static (double LocalX, double LocalY) ResolveConcentratedLoadInLocalCoordinates(
        StructuralLoad load,
        MemberGeometry geometry) =>
        load.Direction switch
        {
            StructuralLoadDirection.LocalX => (load.Value, 0),
            StructuralLoadDirection.LocalY => (0, load.Value),
            StructuralLoadDirection.GlobalX => (geometry.Cosine * load.Value, -geometry.Sine * load.Value),
            StructuralLoadDirection.GlobalY => (geometry.Sine * load.Value, geometry.Cosine * load.Value),
            _ => throw new StructuralAnalysisException($"Unsupported point load direction '{load.Direction}'.")
        };

    private static (double LocalX, double LocalY) ResolveUniformLoadInLocalCoordinates(
        StructuralLoad load,
        MemberGeometry geometry) =>
        load.Direction switch
        {
            StructuralLoadDirection.LocalX => (load.Value, 0),
            StructuralLoadDirection.LocalY => (0, load.Value),
            StructuralLoadDirection.GlobalX => (geometry.Cosine * load.Value, -geometry.Sine * load.Value),
            StructuralLoadDirection.GlobalY => (geometry.Sine * load.Value, geometry.Cosine * load.Value),
            _ => throw new StructuralAnalysisException($"Unsupported uniform load direction '{load.Direction}'.")
        };

    private static bool[] BuildRestrainedDofMask(
        StructuralModel model,
        Dictionary<string, int> nodeIndexById,
        int totalDofCount)
    {
        bool[] restrainedDofs = new bool[totalDofCount];

        foreach (StructuralSupport support in model.Supports)
        {
            int nodeBaseDof = GetNodeBaseDof(nodeIndexById[support.NodeId]);

            restrainedDofs[nodeBaseDof] |= support.RestrainedUx;
            restrainedDofs[nodeBaseDof + 1] |= support.RestrainedUy;
            restrainedDofs[nodeBaseDof + 2] |= support.RestrainedRz;
        }

        return restrainedDofs;
    }

    private static IReadOnlyList<NodalDisplacementResult> BuildNodalDisplacementResults(
        StructuralModel model,
        Dictionary<string, int> nodeIndexById,
        double[] globalDisplacements) =>
        model.Nodes
            .Select(node =>
            {
                int nodeBaseDof = GetNodeBaseDof(nodeIndexById[node.Id]);
                return new NodalDisplacementResult(
                    node.Id,
                    globalDisplacements[nodeBaseDof],
                    globalDisplacements[nodeBaseDof + 1],
                    globalDisplacements[nodeBaseDof + 2]);
            })
            .ToList();

    private static IReadOnlyList<SupportReactionResult> BuildSupportReactionResults(
        StructuralModel model,
        Dictionary<string, int> nodeIndexById,
        double[] globalResidual) =>
        model.Supports
            .Select(support =>
            {
                int nodeBaseDof = GetNodeBaseDof(nodeIndexById[support.NodeId]);
                return new SupportReactionResult(
                    support.Id,
                    support.NodeId,
                    support.RestrainedUx ? globalResidual[nodeBaseDof] : 0,
                    support.RestrainedUy ? globalResidual[nodeBaseDof + 1] : 0,
                    support.RestrainedRz ? globalResidual[nodeBaseDof + 2] : 0);
            })
            .ToList();

    private static IReadOnlyList<MemberEndForceResult> BuildMemberEndForceResults(
        StructuralModel model,
        Dictionary<string, int> nodeIndexById,
        double[] globalDisplacements,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
    {
        Dictionary<string, StructuralNode> nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralMaterial> materials = model.Materials.ToDictionary(material => material.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralSection> sections = model.Sections.ToDictionary(section => section.Id, StringComparer.OrdinalIgnoreCase);
        List<MemberEndForceResult> results = new();

        foreach (StructuralMember member in model.Members)
        {
            StructuralNode startNode = nodes[member.StartNodeId];
            StructuralNode endNode = nodes[member.EndNodeId];
            StructuralMaterial material = materials[member.MaterialId];
            StructuralSection section = sections[member.SectionId];
            MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);

            double[,] localStiffness = Frame2DElementMatrices.BuildLocalStiffness(
                material.ElasticModulus,
                section.Area,
                section.MomentOfInertia,
                geometry.Length);

            double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
            int[] dofs = GetMemberDofs(member, nodeIndexById);
            double[] memberGlobalDisplacements = ExtractSubvector(globalDisplacements, dofs);
            double[] memberLocalDisplacements = Frame2DElementMatrices.TransformDisplacementToLocal(memberGlobalDisplacements, transformation);
            double[] elasticEndForces = Frame2DElementMatrices.Multiply(localStiffness, memberLocalDisplacements);
            memberEquivalentLocalLoads.TryGetValue(member.Id, out double[]? equivalentLocalLoad);
            equivalentLocalLoad ??= new double[6];
            double[] localEndForces = Subtract(elasticEndForces, equivalentLocalLoad);

            results.Add(new MemberEndForceResult(
                member.Id,
                localEndForces[0],
                localEndForces[1],
                localEndForces[2],
                localEndForces[3],
                localEndForces[4],
                localEndForces[5]));
        }

        return results;
    }

    private static int[] GetMemberDofs(StructuralMember member, Dictionary<string, int> nodeIndexById)
    {
        int startBaseDof = GetNodeBaseDof(nodeIndexById[member.StartNodeId]);
        int endBaseDof = GetNodeBaseDof(nodeIndexById[member.EndNodeId]);

        return new[]
        {
            startBaseDof,
            startBaseDof + 1,
            startBaseDof + 2,
            endBaseDof,
            endBaseDof + 1,
            endBaseDof + 2
        };
    }

    private static int GetNodeBaseDof(int nodeIndex) => nodeIndex * DofsPerNode;

    private static void AddElementMatrix(double[,] globalMatrix, double[,] elementMatrix, int[] dofs)
    {
        for (int row = 0; row < dofs.Length; row++)
        {
            for (int column = 0; column < dofs.Length; column++)
            {
                globalMatrix[dofs[row], dofs[column]] += elementMatrix[row, column];
            }
        }
    }

    private static void AddElementVector(double[] globalVector, double[] elementVector, int[] dofs)
    {
        for (int index = 0; index < dofs.Length; index++)
        {
            globalVector[dofs[index]] += elementVector[index];
        }
    }

    private static double[,] ExtractSubmatrix(double[,] matrix, IReadOnlyList<int> rowIndices, IReadOnlyList<int> columnIndices)
    {
        double[,] result = new double[rowIndices.Count, columnIndices.Count];

        for (int row = 0; row < rowIndices.Count; row++)
        {
            for (int column = 0; column < columnIndices.Count; column++)
            {
                result[row, column] = matrix[rowIndices[row], columnIndices[column]];
            }
        }

        return result;
    }

    private static double[] ExtractSubvector(double[] vector, IReadOnlyList<int> indices)
    {
        double[] result = new double[indices.Count];

        for (int index = 0; index < indices.Count; index++)
        {
            result[index] = vector[indices[index]];
        }

        return result;
    }

    private static double[] Multiply(double[,] matrix, double[] vector)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);

        if (columns != vector.Length)
        {
            throw new ArgumentException("Matrix and vector dimensions are not compatible.", nameof(vector));
        }

        double[] result = new double[rows];

        for (int row = 0; row < rows; row++)
        {
            double sum = 0;
            for (int column = 0; column < columns; column++)
            {
                sum += matrix[row, column] * vector[column];
            }

            result[row] = sum;
        }

        return result;
    }

    private static double[] Subtract(double[] left, double[] right)
    {
        if (left.Length != right.Length)
        {
            throw new ArgumentException("Vector dimensions are not compatible.", nameof(right));
        }

        double[] result = new double[left.Length];

        for (int index = 0; index < left.Length; index++)
        {
            result[index] = left[index] - right[index];
        }

        return result;
    }

    private sealed record MemberGeometry(double Length, double Cosine, double Sine)
    {
        public static MemberGeometry FromNodes(StructuralNode startNode, StructuralNode endNode)
        {
            double dx = endNode.X - startNode.X;
            double dy = endNode.Y - startNode.Y;
            double length = Math.Sqrt((dx * dx) + (dy * dy));

            return new MemberGeometry(length, dx / length, dy / length);
        }
    }
}
