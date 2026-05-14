using StructuralSolver2D.Analysis.Frame2D;
using StructuralSolver2D.Analysis.Results;
using StructuralSolver2D.Analysis.Constraints;
using StructuralSolver2D.Analysis.Solvers;
using StructuralSolver2D.Analysis.Truss2D;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Analysis.PlaneStructure2D;

/// <summary>
/// Performs a first-order linear elastic analysis of a plane structure containing both Frame2D and Truss2D members.
/// Each node has three global degrees of freedom: Ux, Uy and Rz. Truss2D members contribute only to Ux and Uy.
/// </summary>
public sealed class PlaneStructureAnalyzer
{
    private const int DofsPerNode = 3;

    /// <summary>
    /// Analyzes the supplied structural model for one load case.
    /// </summary>
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
    public StructuralAnalysisResult AnalyzeCombination(StructuralModel model, string combinationId)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(combinationId))
        {
            throw new ArgumentException("Load combination id cannot be empty.", nameof(combinationId));
        }

        ValidateModel(model);

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
        Dictionary<string, double[]> memberEquivalentLocalLoads = new(StringComparer.OrdinalIgnoreCase);

        AssembleGlobalStiffness(model, nodeIndexById, globalStiffness);
        AssembleLoads(model, loadCaseFactors, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads);

        double[] globalDisplacements = SupportConstraintSystem.SolveConstrainedSystem(
            model,
            nodeIndexById,
            DofsPerNode,
            totalDofCount,
            globalStiffness,
            globalLoadVector,
            includeRotationalDof: true,
            addInactiveDofConstraints: true);

        double[] globalResidual = Subtract(Multiply(globalStiffness, globalDisplacements), globalLoadVector);

        return new StructuralAnalysisResult(
            resultId,
            BuildNodalDisplacementResults(model, nodeIndexById, globalDisplacements),
            SupportConstraintSystem.BuildSupportReactionResults(model, nodeIndexById, DofsPerNode, globalResidual, includeRotationalDof: true),
            BuildMemberEndForceResults(model, nodeIndexById, globalDisplacements, memberEquivalentLocalLoads));
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

            if (member.Type == MemberType.Frame2D)
            {
                double[,] localStiffness = Frame2DElementMatrices.BuildLocalStiffness(
                    material.ElasticModulus,
                    section.Area,
                    section.MomentOfInertia,
                    geometry.Length);
                localStiffness = Frame2DElementMatrices.ApplyMomentReleasesToStiffness(
                    localStiffness,
                    member.ReleaseStartMoment,
                    member.ReleaseEndMoment);

                double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
                double[,] globalElementStiffness = Frame2DElementMatrices.TransformStiffnessToGlobal(localStiffness, transformation);
                AddElementMatrix(globalStiffness, globalElementStiffness, GetFrameMemberDofs(member, nodeIndexById));
            }
            else if (member.Type == MemberType.Truss2D)
            {
                double[,] trussStiffness = Truss2DElementMatrices.BuildGlobalStiffness(
                    material.ElasticModulus,
                    section.Area,
                    geometry.Length,
                    geometry.Cosine,
                    geometry.Sine);
                AddElementMatrix(globalStiffness, trussStiffness, GetTrussMemberDofs(member, nodeIndexById));
            }
            else
            {
                throw new StructuralAnalysisException($"Unsupported member type '{member.Type}' for member '{member.Id}'.");
            }
        }
    }

    private static void AssembleLoads(
        StructuralModel model,
        IReadOnlyDictionary<string, double> loadCaseFactors,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
    {
        Dictionary<string, StructuralMember> members = model.Members.ToDictionary(member => member.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralNode> nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralMaterial> materials = model.Materials.ToDictionary(material => material.Id, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralSection> sections = model.Sections.ToDictionary(section => section.Id, StringComparer.OrdinalIgnoreCase);

        foreach (StructuralLoad load in model.Loads.Where(load => loadCaseFactors.ContainsKey(load.LoadCaseId)))
        {
            double loadFactor = loadCaseFactors[load.LoadCaseId];

            switch (load.Type)
            {
                case StructuralLoadType.NodalForce:
                    AddNodalForce(load, loadFactor, nodeIndexById, globalLoadVector);
                    break;

                case StructuralLoadType.NodalMoment:
                    AddNodalMoment(load, loadFactor, nodeIndexById, globalLoadVector);
                    break;

                case StructuralLoadType.UniformDistributedLoad:
                case StructuralLoadType.LinearDistributedLoad:
                case StructuralLoadType.PointLoadOnMember:
                    if (!members.TryGetValue(load.TargetId, out StructuralMember? loadedMember))
                    {
                        throw new StructuralAnalysisException($"Load '{load.Id}' targets member '{load.TargetId}', which was not found.");
                    }

                    if (loadedMember.Type != MemberType.Frame2D)
                    {
                        throw new StructuralAnalysisException(
                            $"Load '{load.Id}' has type '{load.Type}' and targets member '{loadedMember.Id}' ({loadedMember.Type}). " +
                            "Member loads are currently supported only on Frame2D members in mixed plane structures.");
                    }

                    if (load.Type == StructuralLoadType.UniformDistributedLoad)
                    {
                        AddUniformDistributedLoad(load, loadFactor, members, nodes, materials, sections, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads);
                    }
                    else if (load.Type == StructuralLoadType.LinearDistributedLoad)
                    {
                        AddLinearDistributedLoad(load, loadFactor, members, nodes, materials, sections, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads);
                    }
                    else
                    {
                        AddPointLoadOnMember(load, loadFactor, members, nodes, materials, sections, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads);
                    }

                    break;

                case StructuralLoadType.SelfWeight:
                    throw new StructuralAnalysisException("Self-weight loads are not supported by this first mixed plane-structure analysis milestone.");
            }
        }
    }

    private static void AddNodalForce(
        StructuralLoad load,
        double loadFactor,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector)
    {
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
            throw new StructuralAnalysisException($"Unsupported nodal force direction '{load.Direction}'.");
        }
    }

    private static void AddNodalMoment(
        StructuralLoad load,
        double loadFactor,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector)
    {
        int nodeBaseDof = GetNodeBaseDof(nodeIndexById[load.TargetId]);
        globalLoadVector[nodeBaseDof + 2] += load.Value * loadFactor;
    }

    private static void AddUniformDistributedLoad(
        StructuralLoad load,
        double loadFactor,
        Dictionary<string, StructuralMember> members,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMaterial> materials,
        Dictionary<string, StructuralSection> sections,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
    {
        StructuralMember member = members[load.TargetId];
        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);

        (double localXValue, double localYValue) = ResolveLoadValueInLocalCoordinates(load.Direction, load.Value, geometry, "uniform");
        localXValue *= loadFactor;
        localYValue *= loadFactor;
        double[] localLoad = Frame2DElementMatrices.BuildUniformLocalLoad(localXValue, localYValue, geometry.Length);
        localLoad = ApplyMomentReleasesToLocalLoad(member, materials, sections, geometry, localLoad);
        AddFrameLocalLoadToGlobal(member, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads, geometry, localLoad);
    }

    private static void AddLinearDistributedLoad(
        StructuralLoad load,
        double loadFactor,
        Dictionary<string, StructuralMember> members,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMaterial> materials,
        Dictionary<string, StructuralSection> sections,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
    {
        if (!load.EndValue.HasValue)
        {
            throw new StructuralAnalysisException($"Linear distributed load '{load.Id}' has no end value.");
        }

        StructuralMember member = members[load.TargetId];
        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);

        (double startLocalXValue, double startLocalYValue) = ResolveLoadValueInLocalCoordinates(load.Direction, load.Value, geometry, "linear distributed");
        (double endLocalXValue, double endLocalYValue) = ResolveLoadValueInLocalCoordinates(load.Direction, load.EndValue.Value, geometry, "linear distributed");
        double[] localLoad = Frame2DElementMatrices.BuildLinearLocalLoad(
            startLocalXValue * loadFactor,
            endLocalXValue * loadFactor,
            startLocalYValue * loadFactor,
            endLocalYValue * loadFactor,
            geometry.Length);
        localLoad = ApplyMomentReleasesToLocalLoad(member, materials, sections, geometry, localLoad);
        AddFrameLocalLoadToGlobal(member, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads, geometry, localLoad);
    }

    private static void AddPointLoadOnMember(
        StructuralLoad load,
        double loadFactor,
        Dictionary<string, StructuralMember> members,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMaterial> materials,
        Dictionary<string, StructuralSection> sections,
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

        (double localXValue, double localYValue) = ResolveLoadValueInLocalCoordinates(load.Direction, load.Value, geometry, "point");
        double[] localLoad = Frame2DElementMatrices.BuildPointLocalLoad(
            localXValue * loadFactor,
            localYValue * loadFactor,
            geometry.Length,
            load.Position.Value);
        localLoad = ApplyMomentReleasesToLocalLoad(member, materials, sections, geometry, localLoad);
        AddFrameLocalLoadToGlobal(member, nodeIndexById, globalLoadVector, memberEquivalentLocalLoads, geometry, localLoad);
    }

    private static void AddFrameLocalLoadToGlobal(
        StructuralMember member,
        Dictionary<string, int> nodeIndexById,
        double[] globalLoadVector,
        Dictionary<string, double[]> memberEquivalentLocalLoads,
        MemberGeometry geometry,
        double[] localLoad)
    {
        double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
        double[] globalLoad = Frame2DElementMatrices.TransformLoadToGlobal(localLoad, transformation);
        int[] dofs = GetFrameMemberDofs(member, nodeIndexById);

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

    private static double[] ApplyMomentReleasesToLocalLoad(
        StructuralMember member,
        Dictionary<string, StructuralMaterial> materials,
        Dictionary<string, StructuralSection> sections,
        MemberGeometry geometry,
        double[] localLoad)
    {
        if (!member.ReleaseStartMoment && !member.ReleaseEndMoment)
        {
            return localLoad;
        }

        StructuralMaterial material = materials[member.MaterialId];
        StructuralSection section = sections[member.SectionId];
        double[,] localStiffness = Frame2DElementMatrices.BuildLocalStiffness(
            material.ElasticModulus,
            section.Area,
            section.MomentOfInertia,
            geometry.Length);

        return Frame2DElementMatrices.ApplyMomentReleases(
            localStiffness,
            localLoad,
            member.ReleaseStartMoment,
            member.ReleaseEndMoment).Load;
    }

    private static (double LocalX, double LocalY) ResolveLoadValueInLocalCoordinates(
        StructuralLoadDirection direction,
        double value,
        MemberGeometry geometry,
        string loadKind) =>
        direction switch
        {
            StructuralLoadDirection.LocalX => (value, 0),
            StructuralLoadDirection.LocalY => (0, value),
            StructuralLoadDirection.GlobalX => (geometry.Cosine * value, -geometry.Sine * value),
            StructuralLoadDirection.GlobalY => (geometry.Sine * value, geometry.Cosine * value),
            _ => throw new StructuralAnalysisException($"Unsupported {loadKind} load direction '{direction}'.")
        };

    private static bool[] BuildRestrainedDofMask(
        StructuralModel model,
        Dictionary<string, int> nodeIndexById,
        int totalDofCount,
        double[,] globalStiffness,
        double[] globalLoadVector)
    {
        const double inactiveDofTolerance = 1e-12;
        bool[] restrainedDofs = new bool[totalDofCount];

        foreach (StructuralSupport support in model.Supports)
        {
            int nodeBaseDof = GetNodeBaseDof(nodeIndexById[support.NodeId]);

            restrainedDofs[nodeBaseDof] |= support.RestrainedUx;
            restrainedDofs[nodeBaseDof + 1] |= support.RestrainedUy;
            restrainedDofs[nodeBaseDof + 2] |= support.RestrainedRz;
        }

        for (int dof = 0; dof < totalDofCount; dof++)
        {
            if (restrainedDofs[dof])
            {
                continue;
            }

            bool hasStiffness = false;
            for (int column = 0; column < totalDofCount; column++)
            {
                if (Math.Abs(globalStiffness[dof, column]) > inactiveDofTolerance)
                {
                    hasStiffness = true;
                    break;
                }
            }

            if (!hasStiffness && Math.Abs(globalLoadVector[dof]) <= inactiveDofTolerance)
            {
                restrainedDofs[dof] = true;
            }
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
            if (member.Type == MemberType.Frame2D)
            {
                results.Add(BuildFrameMemberEndForce(member, nodes, materials, sections, nodeIndexById, globalDisplacements, memberEquivalentLocalLoads));
            }
            else if (member.Type == MemberType.Truss2D)
            {
                results.Add(BuildTrussMemberEndForce(member, nodes, materials, sections, nodeIndexById, globalDisplacements));
            }
        }

        return results;
    }

    private static MemberEndForceResult BuildFrameMemberEndForce(
        StructuralMember member,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMaterial> materials,
        Dictionary<string, StructuralSection> sections,
        Dictionary<string, int> nodeIndexById,
        double[] globalDisplacements,
        Dictionary<string, double[]> memberEquivalentLocalLoads)
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
        localStiffness = Frame2DElementMatrices.ApplyMomentReleasesToStiffness(
            localStiffness,
            member.ReleaseStartMoment,
            member.ReleaseEndMoment);

        double[,] transformation = Frame2DElementMatrices.BuildTransformation(geometry.Cosine, geometry.Sine);
        int[] dofs = GetFrameMemberDofs(member, nodeIndexById);
        double[] memberGlobalDisplacements = ExtractSubvector(globalDisplacements, dofs);
        double[] memberLocalDisplacements = Frame2DElementMatrices.TransformDisplacementToLocal(memberGlobalDisplacements, transformation);
        double[] elasticEndForces = Frame2DElementMatrices.Multiply(localStiffness, memberLocalDisplacements);
        memberEquivalentLocalLoads.TryGetValue(member.Id, out double[]? equivalentLocalLoad);
        equivalentLocalLoad ??= new double[6];
        double[] localEndForces = Subtract(elasticEndForces, equivalentLocalLoad);

        return new MemberEndForceResult(
            member.Id,
            localEndForces[0],
            localEndForces[1],
            localEndForces[2],
            localEndForces[3],
            localEndForces[4],
            localEndForces[5]);
    }

    private static MemberEndForceResult BuildTrussMemberEndForce(
        StructuralMember member,
        Dictionary<string, StructuralNode> nodes,
        Dictionary<string, StructuralMaterial> materials,
        Dictionary<string, StructuralSection> sections,
        Dictionary<string, int> nodeIndexById,
        double[] globalDisplacements)
    {
        StructuralNode startNode = nodes[member.StartNodeId];
        StructuralNode endNode = nodes[member.EndNodeId];
        StructuralMaterial material = materials[member.MaterialId];
        StructuralSection section = sections[member.SectionId];
        MemberGeometry geometry = MemberGeometry.FromNodes(startNode, endNode);
        int[] dofs = GetTrussMemberDofs(member, nodeIndexById);
        double[] memberGlobalDisplacements = ExtractSubvector(globalDisplacements, dofs);
        double axialExtension = (-geometry.Cosine * memberGlobalDisplacements[0])
            + (-geometry.Sine * memberGlobalDisplacements[1])
            + (geometry.Cosine * memberGlobalDisplacements[2])
            + (geometry.Sine * memberGlobalDisplacements[3]);
        double normalForce = material.ElasticModulus * section.Area * axialExtension / geometry.Length;

        return new MemberEndForceResult(
            member.Id,
            -normalForce,
            0.0,
            0.0,
            normalForce,
            0.0,
            0.0);
    }

    private static int[] GetFrameMemberDofs(StructuralMember member, Dictionary<string, int> nodeIndexById)
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

    private static int[] GetTrussMemberDofs(StructuralMember member, Dictionary<string, int> nodeIndexById)
    {
        int startBaseDof = GetNodeBaseDof(nodeIndexById[member.StartNodeId]);
        int endBaseDof = GetNodeBaseDof(nodeIndexById[member.EndNodeId]);

        return new[]
        {
            startBaseDof,
            startBaseDof + 1,
            endBaseDof,
            endBaseDof + 1
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
