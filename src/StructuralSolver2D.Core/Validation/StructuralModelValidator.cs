using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Validation;

/// <summary>
/// Validates a structural model before it is sent to the analysis engine.
/// </summary>
public sealed class StructuralModelValidator
{
    /// <summary>
    /// Default tolerance used to detect zero-length members, expressed in meters.
    /// </summary>
    public const double DefaultLengthTolerance = 1e-9;

    private readonly double lengthTolerance;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralModelValidator"/> class.
    /// </summary>
    /// <param name="lengthTolerance">Tolerance used to detect zero-length members, in meters.</param>
    public StructuralModelValidator(double lengthTolerance = DefaultLengthTolerance)
    {
        if (lengthTolerance <= 0 || !double.IsFinite(lengthTolerance))
        {
            throw new ArgumentOutOfRangeException(nameof(lengthTolerance), "Length tolerance must be a finite positive value.");
        }

        this.lengthTolerance = lengthTolerance;
    }

    /// <summary>
    /// Validates the supplied structural model.
    /// </summary>
    /// <param name="model">Model to validate.</param>
    /// <returns>Validation result.</returns>
    public StructuralModelValidationResult Validate(StructuralModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        List<StructuralModelValidationIssue> issues = new();

        ValidateIdentifiers(model.Nodes.Select(node => node.Id), "NODE_DUPLICATE_ID", "Duplicate node id.", issues);
        ValidateIdentifiers(model.Members.Select(member => member.Id), "MEMBER_DUPLICATE_ID", "Duplicate member id.", issues);
        ValidateIdentifiers(model.Materials.Select(material => material.Id), "MATERIAL_DUPLICATE_ID", "Duplicate material id.", issues);
        ValidateIdentifiers(model.Sections.Select(section => section.Id), "SECTION_DUPLICATE_ID", "Duplicate section id.", issues);
        ValidateIdentifiers(model.Supports.Select(support => support.Id), "SUPPORT_DUPLICATE_ID", "Duplicate support id.", issues);
        ValidateIdentifiers(model.LoadCases.Select(loadCase => loadCase.Id), "LOAD_CASE_DUPLICATE_ID", "Duplicate load case id.", issues);
        ValidateIdentifiers(model.Loads.Select(load => load.Id), "LOAD_DUPLICATE_ID", "Duplicate load id.", issues);
        ValidateIdentifiers(model.LoadCombinations.Select(combination => combination.Id), "LOAD_COMBINATION_DUPLICATE_ID", "Duplicate load combination id.", issues);

        ValidateRequiredIds(model, issues);
        ValidateNodes(model, issues);
        ValidateMaterials(model, issues);
        ValidateSections(model, issues);
        ValidateMembers(model, issues);
        ValidateSupports(model, issues);
        ValidateLoadCases(model, issues);
        ValidateLoads(model, issues);
        ValidateLoadCombinations(model, issues);

        return issues.Count == 0
            ? StructuralModelValidationResult.Success
            : new StructuralModelValidationResult(issues);
    }

    private static void ValidateRequiredIds(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        foreach (StructuralNode node in model.Nodes.Where(node => string.IsNullOrWhiteSpace(node.Id)))
        {
            AddError(issues, "NODE_EMPTY_ID", "Node id cannot be empty.");
        }

        foreach (StructuralMember member in model.Members.Where(member => string.IsNullOrWhiteSpace(member.Id)))
        {
            AddError(issues, "MEMBER_EMPTY_ID", "Member id cannot be empty.");
        }

        foreach (StructuralMaterial material in model.Materials.Where(material => string.IsNullOrWhiteSpace(material.Id)))
        {
            AddError(issues, "MATERIAL_EMPTY_ID", "Material id cannot be empty.");
        }

        foreach (StructuralSection section in model.Sections.Where(section => string.IsNullOrWhiteSpace(section.Id)))
        {
            AddError(issues, "SECTION_EMPTY_ID", "Section id cannot be empty.");
        }

        foreach (StructuralSupport support in model.Supports.Where(support => string.IsNullOrWhiteSpace(support.Id)))
        {
            AddError(issues, "SUPPORT_EMPTY_ID", "Support id cannot be empty.");
        }

        foreach (StructuralLoadCase loadCase in model.LoadCases.Where(loadCase => string.IsNullOrWhiteSpace(loadCase.Id)))
        {
            AddError(issues, "LOAD_CASE_EMPTY_ID", "Load case id cannot be empty.");
        }

        foreach (StructuralLoad load in model.Loads.Where(load => string.IsNullOrWhiteSpace(load.Id)))
        {
            AddError(issues, "LOAD_EMPTY_ID", "Load id cannot be empty.");
        }

        foreach (StructuralLoadCombination combination in model.LoadCombinations.Where(combination => string.IsNullOrWhiteSpace(combination.Id)))
        {
            AddError(issues, "LOAD_COMBINATION_EMPTY_ID", "Load combination id cannot be empty.");
        }
    }

    private static void ValidateNodes(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        foreach (StructuralNode node in model.Nodes)
        {
            if (!double.IsFinite(node.X) || !double.IsFinite(node.Y))
            {
                AddError(issues, "NODE_INVALID_COORDINATE", $"Node '{node.Id}' has invalid coordinates.", node.Id);
            }
        }
    }

    private static void ValidateMaterials(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        foreach (StructuralMaterial material in model.Materials)
        {
            if (string.IsNullOrWhiteSpace(material.Name))
            {
                AddError(issues, "MATERIAL_EMPTY_NAME", $"Material '{material.Id}' has an empty name.", material.Id);
            }

            if (material.ElasticModulus <= 0 || !double.IsFinite(material.ElasticModulus))
            {
                AddError(issues, "MATERIAL_INVALID_ELASTIC_MODULUS", $"Material '{material.Id}' has invalid elastic modulus.", material.Id);
            }

            if (material.UnitWeight is not null && (material.UnitWeight <= 0 || !double.IsFinite(material.UnitWeight.Value)))
            {
                AddError(issues, "MATERIAL_INVALID_UNIT_WEIGHT", $"Material '{material.Id}' has invalid unit weight.", material.Id);
            }
        }
    }

    private static void ValidateSections(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        foreach (StructuralSection section in model.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Name))
            {
                AddError(issues, "SECTION_EMPTY_NAME", $"Section '{section.Id}' has an empty name.", section.Id);
            }

            if (section.Area <= 0 || !double.IsFinite(section.Area))
            {
                AddError(issues, "SECTION_INVALID_AREA", $"Section '{section.Id}' has invalid area.", section.Id);
            }

            if (section.MomentOfInertia <= 0 || !double.IsFinite(section.MomentOfInertia))
            {
                AddError(issues, "SECTION_INVALID_INERTIA", $"Section '{section.Id}' has invalid moment of inertia.", section.Id);
            }
        }
    }

    private void ValidateMembers(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        HashSet<string> nodeIds = model.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> materialIds = model.Materials.Select(material => material.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> sectionIds = model.Sections.Select(section => section.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, StructuralNode> nodesById = model.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Id))
            .GroupBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (StructuralMember member in model.Members)
        {
            if (!nodeIds.Contains(member.StartNodeId))
            {
                AddError(issues, "MEMBER_START_NODE_NOT_FOUND", $"Member '{member.Id}' references missing start node '{member.StartNodeId}'.", member.Id);
            }

            if (!nodeIds.Contains(member.EndNodeId))
            {
                AddError(issues, "MEMBER_END_NODE_NOT_FOUND", $"Member '{member.Id}' references missing end node '{member.EndNodeId}'.", member.Id);
            }

            if (!materialIds.Contains(member.MaterialId))
            {
                AddError(issues, "MEMBER_MATERIAL_NOT_FOUND", $"Member '{member.Id}' references missing material '{member.MaterialId}'.", member.Id);
            }

            if (!sectionIds.Contains(member.SectionId))
            {
                AddError(issues, "MEMBER_SECTION_NOT_FOUND", $"Member '{member.Id}' references missing section '{member.SectionId}'.", member.Id);
            }

            if (nodesById.TryGetValue(member.StartNodeId, out StructuralNode? startNode) &&
                nodesById.TryGetValue(member.EndNodeId, out StructuralNode? endNode))
            {
                double length = StructuralMember.GetLength(startNode, endNode);
                if (length <= lengthTolerance)
                {
                    AddError(issues, "MEMBER_ZERO_LENGTH", $"Member '{member.Id}' has zero or near-zero length.", member.Id);
                }
            }
        }
    }

    private static void ValidateSupports(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        HashSet<string> nodeIds = model.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (StructuralSupport support in model.Supports)
        {
            if (!nodeIds.Contains(support.NodeId))
            {
                AddError(issues, "SUPPORT_NODE_NOT_FOUND", $"Support '{support.Id}' references missing node '{support.NodeId}'.", support.Id);
            }

            if (!double.IsFinite(support.OrientationDegrees))
            {
                AddError(issues, "SUPPORT_INVALID_ORIENTATION", $"Support '{support.Id}' has invalid orientation angle.", support.Id);
            }

            if (!support.RestrainedUx && !support.RestrainedUy && !support.RestrainedRz)
            {
                AddWarning(issues, "SUPPORT_WITHOUT_RESTRAINTS", $"Support '{support.Id}' does not restrain any degree of freedom.", support.Id);
            }
        }
    }

    private static void ValidateLoadCases(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        foreach (StructuralLoadCase loadCase in model.LoadCases)
        {
            if (string.IsNullOrWhiteSpace(loadCase.Name))
            {
                AddError(issues, "LOAD_CASE_EMPTY_NAME", $"Load case '{loadCase.Id}' has an empty name.", loadCase.Id);
            }
        }
    }

    private static void ValidateLoads(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        HashSet<string> loadCaseIds = model.LoadCases.Select(loadCase => loadCase.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> nodeIds = model.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> memberIds = model.Members.Select(member => member.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (StructuralLoad load in model.Loads)
        {
            if (!loadCaseIds.Contains(load.LoadCaseId))
            {
                AddError(issues, "LOAD_CASE_NOT_FOUND", $"Load '{load.Id}' references missing load case '{load.LoadCaseId}'.", load.Id);
            }

            if (!double.IsFinite(load.Value))
            {
                AddError(issues, "LOAD_INVALID_VALUE", $"Load '{load.Id}' has an invalid value.", load.Id);
            }

            if (!Enum.IsDefined(load.Type))
            {
                AddError(issues, "LOAD_INVALID_TYPE", $"Load '{load.Id}' has an invalid load type.", load.Id);
                continue;
            }

            if (!Enum.IsDefined(load.TargetType))
            {
                AddError(issues, "LOAD_INVALID_TARGET_TYPE", $"Load '{load.Id}' has an invalid target type.", load.Id);
                continue;
            }

            if (!Enum.IsDefined(load.Direction))
            {
                AddError(issues, "LOAD_INVALID_DIRECTION", $"Load '{load.Id}' has an invalid direction.", load.Id);
                continue;
            }

            switch (load.Type)
            {
                case StructuralLoadType.NodalForce:
                    ValidateNonZeroValue(load, issues);
                    ValidateNodalLoad(load, nodeIds, issues);
                    ValidateDirection(load, issues, StructuralLoadDirection.GlobalX, StructuralLoadDirection.GlobalY);
                    ValidateNoPosition(load, issues);
                    ValidateNoEndValue(load, issues);
                    break;

                case StructuralLoadType.NodalMoment:
                    ValidateNonZeroValue(load, issues);
                    ValidateNodalLoad(load, nodeIds, issues);
                    ValidateDirection(load, issues, StructuralLoadDirection.MomentZ);
                    ValidateNoPosition(load, issues);
                    ValidateNoEndValue(load, issues);
                    break;

                case StructuralLoadType.UniformDistributedLoad:
                    ValidateNonZeroValue(load, issues);
                    ValidateMemberLoad(load, memberIds, issues);
                    ValidateForceDirection(load, issues);
                    ValidateNoPosition(load, issues);
                    ValidateNoEndValue(load, issues);
                    break;

                case StructuralLoadType.PointLoadOnMember:
                    ValidateNonZeroValue(load, issues);
                    ValidateMemberLoad(load, memberIds, issues);
                    ValidateForceDirection(load, issues);
                    ValidateNormalizedPosition(load, issues);
                    ValidateNoEndValue(load, issues);
                    break;

                case StructuralLoadType.LinearDistributedLoad:
                    ValidateMemberLoad(load, memberIds, issues);
                    ValidateForceDirection(load, issues);
                    ValidateNoPosition(load, issues);
                    ValidateEndValue(load, issues);
                    break;

                case StructuralLoadType.SelfWeight:
                    ValidateNonZeroValue(load, issues);
                    ValidateSelfWeightLoad(load, issues);
                    ValidateNoPosition(load, issues);
                    ValidateNoEndValue(load, issues);
                    break;
            }
        }
    }

    private static void ValidateLoadCombinations(StructuralModel model, List<StructuralModelValidationIssue> issues)
    {
        HashSet<string> loadCaseIds = model.LoadCases.Select(loadCase => loadCase.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (StructuralLoadCombination combination in model.LoadCombinations)
        {
            if (string.IsNullOrWhiteSpace(combination.Name))
            {
                AddError(issues, "LOAD_COMBINATION_EMPTY_NAME", $"Load combination '{combination.Id}' has an empty name.", combination.Id);
            }

            if (combination.Terms.Count == 0)
            {
                AddError(issues, "LOAD_COMBINATION_WITHOUT_TERMS", $"Load combination '{combination.Id}' has no terms.", combination.Id);
                continue;
            }

            IEnumerable<string> duplicateTermLoadCases = combination.Terms
                .Where(term => !string.IsNullOrWhiteSpace(term.LoadCaseId))
                .GroupBy(term => term.LoadCaseId, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);

            foreach (string duplicateLoadCaseId in duplicateTermLoadCases)
            {
                AddError(issues, "LOAD_COMBINATION_DUPLICATE_TERM", $"Load combination '{combination.Id}' references load case '{duplicateLoadCaseId}' more than once.", combination.Id);
            }

            foreach (StructuralLoadCombinationTerm term in combination.Terms)
            {
                if (string.IsNullOrWhiteSpace(term.LoadCaseId))
                {
                    AddError(issues, "LOAD_COMBINATION_TERM_EMPTY_LOAD_CASE", $"Load combination '{combination.Id}' has a term without load case id.", combination.Id);
                    continue;
                }

                if (!loadCaseIds.Contains(term.LoadCaseId))
                {
                    AddError(issues, "LOAD_COMBINATION_LOAD_CASE_NOT_FOUND", $"Load combination '{combination.Id}' references missing load case '{term.LoadCaseId}'.", combination.Id);
                }

                if (term.Factor == 0 || !double.IsFinite(term.Factor))
                {
                    AddError(issues, "LOAD_COMBINATION_INVALID_FACTOR", $"Load combination '{combination.Id}' has an invalid factor for load case '{term.LoadCaseId}'.", combination.Id);
                }
            }
        }
    }


    private static void ValidateNonZeroValue(StructuralLoad load, List<StructuralModelValidationIssue> issues)
    {
        if (load.Value == 0)
        {
            AddError(issues, "LOAD_INVALID_VALUE", $"Load '{load.Id}' has a zero value.", load.Id);
        }
    }

    private static void ValidateEndValue(StructuralLoad load, List<StructuralModelValidationIssue> issues)
    {
        if (load.EndValue is null)
        {
            AddError(issues, "LOAD_END_VALUE_REQUIRED", $"Load '{load.Id}' requires an end value.", load.Id);
            return;
        }

        if (!double.IsFinite(load.EndValue.Value))
        {
            AddError(issues, "LOAD_INVALID_END_VALUE", $"Load '{load.Id}' has an invalid end value.", load.Id);
        }

        if (load.Value == 0 && load.EndValue.Value == 0)
        {
            AddError(issues, "LOAD_INVALID_VALUE", $"Load '{load.Id}' has zero start and end values.", load.Id);
        }
    }

    private static void ValidateNoEndValue(StructuralLoad load, List<StructuralModelValidationIssue> issues)
    {
        if (load.EndValue is not null)
        {
            AddError(issues, "LOAD_END_VALUE_NOT_ALLOWED", $"Load '{load.Id}' must not define an end value.", load.Id);
        }
    }

    private static void ValidateNodalLoad(
        StructuralLoad load,
        HashSet<string> nodeIds,
        List<StructuralModelValidationIssue> issues)
    {
        if (load.TargetType != StructuralLoadTargetType.Node)
        {
            AddError(issues, "LOAD_INVALID_TARGET_TYPE_FOR_TYPE", $"Load '{load.Id}' must target a node.", load.Id);
            return;
        }

        if (!nodeIds.Contains(load.TargetId))
        {
            AddError(issues, "LOAD_TARGET_NODE_NOT_FOUND", $"Load '{load.Id}' references missing node '{load.TargetId}'.", load.Id);
        }
    }

    private static void ValidateMemberLoad(
        StructuralLoad load,
        HashSet<string> memberIds,
        List<StructuralModelValidationIssue> issues)
    {
        if (load.TargetType != StructuralLoadTargetType.Member)
        {
            AddError(issues, "LOAD_INVALID_TARGET_TYPE_FOR_TYPE", $"Load '{load.Id}' must target a member.", load.Id);
            return;
        }

        if (!memberIds.Contains(load.TargetId))
        {
            AddError(issues, "LOAD_TARGET_MEMBER_NOT_FOUND", $"Load '{load.Id}' references missing member '{load.TargetId}'.", load.Id);
        }
    }

    private static void ValidateSelfWeightLoad(StructuralLoad load, List<StructuralModelValidationIssue> issues)
    {
        if (load.TargetType != StructuralLoadTargetType.Model)
        {
            AddError(issues, "LOAD_INVALID_TARGET_TYPE_FOR_TYPE", $"Self-weight load '{load.Id}' must target the model.", load.Id);
        }

        if (!string.IsNullOrWhiteSpace(load.TargetId))
        {
            AddError(issues, "LOAD_TARGET_ID_NOT_ALLOWED", $"Self-weight load '{load.Id}' must not reference a specific target id.", load.Id);
        }

        ValidateDirection(load, issues, StructuralLoadDirection.GlobalY, StructuralLoadDirection.LocalY);
    }

    private static void ValidateForceDirection(StructuralLoad load, List<StructuralModelValidationIssue> issues) =>
        ValidateDirection(
            load,
            issues,
            StructuralLoadDirection.GlobalX,
            StructuralLoadDirection.GlobalY,
            StructuralLoadDirection.LocalX,
            StructuralLoadDirection.LocalY);

    private static void ValidateDirection(
        StructuralLoad load,
        List<StructuralModelValidationIssue> issues,
        params StructuralLoadDirection[] allowedDirections)
    {
        if (!allowedDirections.Contains(load.Direction))
        {
            AddError(issues, "LOAD_INVALID_DIRECTION_FOR_TYPE", $"Load '{load.Id}' has a direction incompatible with its load type.", load.Id);
        }
    }

    private static void ValidateNoPosition(StructuralLoad load, List<StructuralModelValidationIssue> issues)
    {
        if (load.Position is not null)
        {
            AddError(issues, "LOAD_POSITION_NOT_ALLOWED", $"Load '{load.Id}' must not define a member position.", load.Id);
        }
    }

    private static void ValidateNormalizedPosition(StructuralLoad load, List<StructuralModelValidationIssue> issues)
    {
        if (load.Position is null)
        {
            AddError(issues, "LOAD_POSITION_REQUIRED", $"Load '{load.Id}' requires a normalized member position.", load.Id);
            return;
        }

        if (load.Position < 0 || load.Position > 1 || !double.IsFinite(load.Position.Value))
        {
            AddError(issues, "LOAD_POSITION_OUT_OF_RANGE", $"Load '{load.Id}' position must be between 0.0 and 1.0.", load.Id);
        }
    }

    private static void ValidateIdentifiers(
        IEnumerable<string> identifiers,
        string code,
        string message,
        List<StructuralModelValidationIssue> issues)
    {
        IEnumerable<string> duplicates = identifiers
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (string duplicate in duplicates)
        {
            AddError(issues, code, $"{message} Value: '{duplicate}'.", duplicate);
        }
    }

    private static void AddError(
        List<StructuralModelValidationIssue> issues,
        string code,
        string message,
        string? entityId = null) =>
        issues.Add(new StructuralModelValidationIssue(StructuralModelValidationSeverity.Error, code, message, entityId));

    private static void AddWarning(
        List<StructuralModelValidationIssue> issues,
        string code,
        string message,
        string? entityId = null) =>
        issues.Add(new StructuralModelValidationIssue(StructuralModelValidationSeverity.Warning, code, message, entityId));
}
