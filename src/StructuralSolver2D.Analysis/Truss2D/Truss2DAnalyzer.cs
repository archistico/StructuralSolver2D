using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Constraints;
using StructuralSolver2D.Analysis.Solvers;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.Truss2D;

/// <summary>
/// Performs a first-order linear elastic analysis of a plane truss made of axial-only 2D truss members.
/// Each node has two active degrees of freedom: Ux and Uy. Rz is ignored and reported as zero.
/// </summary>
public sealed class Truss2DAnalyzer
{
    private const int DofsPerNode = 2;

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
        ValidateTrussOnlyModel(model);

        if (!model.LoadCases.Any(loadCase => string.Equals(loadCase.Id, loadCaseId, StringComparison.OrdinalIgnoreCase)))
        {
            throw new StructuralAnalysisException($"Load case '{loadCaseId}' was not found in the structural model.");
        }

        return AnalyzeFactoredLoadCases(
            model,
            loadCaseId,
            new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
            {
                [loadCaseId] = 1.0,
            });
    }

    /// <summary>
    /// Analyzes the supplied structural model for one user-defined manual load combination.
    /// </summary>
    /// <param name="model">Structural model to analyze.</param>
    /// <param name="combinationId">Load combination identifier.</param>
    /// <returns>Analysis result whose <see cref="StructuralAnalysisResult.LoadCaseId"/> contains the combination id.</returns>
    /// <exception cref="StructuralAnalysisException">Thrown when the model cannot be analyzed.</exception>
    public StructuralAnalysisResult AnalyzeCombination(StructuralModel model, string combinationId)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(combinationId))
        {
            throw new ArgumentException("Load combination id cannot be empty.", nameof(combinationId));
        }

        ValidateModel(model);
        ValidateTrussOnlyModel(model);

        StructuralLoadCombination combination = model.LoadCombinations.FirstOrDefault(
            combination => string.Equals(combination.Id, combinationId, StringComparison.OrdinalIgnoreCase))
            ?? throw new StructuralAnalysisException($"Load combination '{combinationId}' was not found in the structural model.");

        Dictionary<string, double> factors = combination.Terms.ToDictionary(
            term => term.LoadCaseId,
            term => term.Factor,
            StringComparer.OrdinalIgnoreCase);

        return AnalyzeFactoredLoadCases(model, combinationId, factors);
    }

    private static StructuralAnalysisResult AnalyzeFactoredLoadCases(
        StructuralModel model,
        string resultId,
        IReadOnlyDictionary<string, double> loadCaseFactors)
    {
        Dictionary<string, int> nodeIndexById = BuildNodeIndex(model);
        int totalDofCount = model.Nodes.Count * DofsPerNode;
        double[,] globalStiffness = new double[totalDofCount, totalDofCount];
        double[] globalLoadVector = new double[totalDofCount];

        AssembleGlobalStiffness(model, nodeIndexById, globalStiffness);
        AssembleLoads(model, loadCaseFactors, nodeIndexById, globalLoadVector);

        double[] globalDisplacements = SupportConstraintSystem.SolveConstrainedSystem(
            model,
            nodeIndexById,
            DofsPerNode,
            totalDofCount,
            globalStiffness,
            globalLoadVector,
            includeRotationalDof: false,
            addInactiveDofConstraints: false);

        double[] globalResidual = Subtract(Multiply(globalStiffness, globalDisplacements), globalLoadVector);

        return new StructuralAnalysisResult(
            resultId,
            BuildNodalDisplacementResults(model, nodeIndexById, globalDisplacements),
            SupportConstraintSystem.BuildSupportReactionResults(model, nodeIndexById, DofsPerNode, globalResidual, includeRotationalDof: false),
            BuildMemberEndForceResults(model, nodeIndexById, globalDisplacements));
    }

    private static void ValidateModel(StructuralModel model)
    {
        StructuralModelValidationResult validationResult = new StructuralModelValidator().Validate(model);
        if (!validationResult.IsValid)
        {
            string issueSummary = string.Join("; ", validationResult.Issues.Take(5).Select(issue => issue.Message));
            throw new StructuralAnalysisException(
                $"The structural model is not valid and cannot be analyzed. First issues: {issueSummary}",
                validationResult.Issues);
        }
    }

    private static void ValidateTrussOnlyModel(StructuralModel model)
    {
        var unsupportedMembers = model.Members
            .Where(member => member.Type != MemberType.Truss2D)
            .Select(member => $"{member.Id} ({member.Type})")
            .Take(10)
            .ToList();

        if (unsupportedMembers.Count > 0)
        {
            throw new StructuralAnalysisException(
                "The Truss2D analyzer supports only Truss2D members. " +
                $"Unsupported members: {string.Join(", ", unsupportedMembers)}. " +
                "Use Frame2DAnalyzer for pure frame models. Mixed Frame2D/Truss2D models are not supported yet.");
        }

        var unsupportedLoads = model.Loads
            .Where(load =>
                load.Type != StructuralLoadType.NodalForce &&
                model.LoadCases.Any(loadCase => string.Equals(loadCase.Id, load.LoadCaseId, StringComparison.OrdinalIgnoreCase)))
            .Select(load => $"{load.Id} ({load.Type})")
            .Take(10)
            .ToList();

        if (unsupportedLoads.Count > 0)
        {
            throw new StructuralAnalysisException(
                "The Truss2D analyzer currently supports only nodal force loads. " +
                $"Unsupported loads: {string.Join(", ", unsupportedLoads)}.");
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

            double[,] elementStiffness = Truss2DElementMatrices.BuildGlobalStiffness(
                material.ElasticModulus,
                section.Area,
                geometry.Length,
                geometry.Cosine,
                geometry.Sine);

            int[] dofs = GetMemberDofs(member, nodeIndexById);
            AddElementMatrix(globalStiffness, elementStiffness, dofs);
        }
    }

    private static void AssembleLoads(
        StructuralModel model,
        IReadOnlyDictionary<string, double> loadCaseFactors,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector)
    {
        foreach (StructuralLoad load in model.Loads.Where(load => loadCaseFactors.ContainsKey(load.LoadCaseId)))
        {
            if (load.Type != StructuralLoadType.NodalForce)
            {
                throw new StructuralAnalysisException($"Load '{load.Id}' has type '{load.Type}', which is not supported by the Truss2D analyzer. Only nodal force loads are supported.");
            }

            double loadFactor = loadCaseFactors[load.LoadCaseId];
            int nodeBaseDof = GetNodeBaseDof(nodeIndexById[load.TargetId]);

            if (load.Direction == StructuralLoadDirection.GlobalX)
            {
                globalLoadVector[nodeBaseDof] += load.Value * loadFactor;
            }
            else if (load.Direction == StructuralLoadDirection.GlobalY)
            {
                globalLoadVector[nodeBaseDof + 1] += load.Value * loadFactor;
            }
            else
            {
                throw new StructuralAnalysisException($"Load '{load.Id}' uses unsupported Truss2D nodal force direction '{load.Direction}'. Supported directions are GlobalX and GlobalY.");
            }
        }
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
                    0.0);
            })
            .ToList();

    private static IReadOnlyList<MemberEndForceResult> BuildMemberEndForceResults(
        StructuralModel model,
        Dictionary<string, int> nodeIndexById,
        double[] globalDisplacements)
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
            int[] dofs = GetMemberDofs(member, nodeIndexById);
            double[] memberGlobalDisplacements = ExtractSubvector(globalDisplacements, dofs);
            double axialExtension = (-geometry.Cosine * memberGlobalDisplacements[0])
                + (-geometry.Sine * memberGlobalDisplacements[1])
                + (geometry.Cosine * memberGlobalDisplacements[2])
                + (geometry.Sine * memberGlobalDisplacements[3]);
            double normalForce = material.ElasticModulus * section.Area * axialExtension / geometry.Length;

            results.Add(new MemberEndForceResult(
                member.Id,
                -normalForce,
                0.0,
                0.0,
                normalForce,
                0.0,
                0.0));
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
            endBaseDof,
            endBaseDof + 1,
        };
    }

    private static int GetNodeBaseDof(int nodeIndex) =>
        nodeIndex * DofsPerNode;

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

    private static double[] ExtractSubvector(double[] vector, IReadOnlyList<int> indexes)
    {
        double[] result = new double[indexes.Count];

        for (int index = 0; index < indexes.Count; index++)
        {
            result[index] = vector[indexes[index]];
        }

        return result;
    }

    private static double[] Multiply(double[,] matrix, double[] vector)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
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
