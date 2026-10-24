using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Dynamics;

/// <summary>
/// Validates dynamic mass metadata without changing the static model or running a modal solver.
/// Error codes are stable and actionable. Version 1 requires distributed member masses;
/// nodal masses are additive, with one user entry per node to prevent accidental duplication.
/// </summary>
public sealed class StructuralDynamicsValidator
{
    public StructuralModelValidationResult Validate(StructuralModel model, StructuralDynamicsDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(definition);

        // Structural validation is a precondition for safe lookups and finite geometry.
        StructuralModelValidationResult baseResult = new StructuralModelValidator().Validate(model);
        if (!baseResult.IsValid) return baseResult;

        List<StructuralModelValidationIssue> issues = new();
        void Error(string code, string message, string? id = null) =>
            issues.Add(new StructuralModelValidationIssue(StructuralModelValidationSeverity.Error, code, message, id));

        if (model.Supports.Any(s => s.StiffnessUx != 0 || s.StiffnessUy != 0 || s.StiffnessRz != 0) ||
            model.Loads.Any(l => l.Type == StructuralLoadType.SupportSettlement))
            Error("DYNAMIC_M77_STATIC_ONLY", "M77 elastic supports and support settlements are supported only in first-order static linear analysis.");

        if (definition.SchemaVersion != 1)
        {
            Error("DYNAMIC_SCHEMA_UNSUPPORTED", $"Unsupported dynamics schemaVersion {definition.SchemaVersion}; supported version is 1.");
            return new StructuralModelValidationResult(issues);
        }

        if (model.Members.Count == 0)
        {
            Error("DYNAMIC_NO_MEMBERS", "Modal analysis requires at least one member with defined material mass.");
        }

        var materials = model.Materials.ToDictionary(material => material.Id, StringComparer.OrdinalIgnoreCase);
        var sections = model.Sections.ToDictionary(section => section.Id, StringComparer.OrdinalIgnoreCase);
        var nodes = model.Nodes.ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        var frameNodeIds = model.Members.Where(member => member.Type == MemberType.Frame2D)
            .SelectMany(member => new[] { member.StartNodeId, member.EndNodeId })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var connectedNodeIds = model.Members.SelectMany(member => new[] { member.StartNodeId, member.EndNodeId })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        double totalMemberMass = 0;
        foreach (StructuralMember member in model.Members)
        {
            StructuralMaterial material = materials[member.MaterialId];
            if (material.UnitWeight is not { } gamma)
            {
                Error("DYNAMIC_MISSING_UNIT_WEIGHT", $"Member '{member.Id}' uses material '{material.Id}' with no UnitWeight [kN/m³]. Specify a positive value; nodal masses do not replace distributed member mass.", member.Id);
                continue;
            }

            StructuralSection section = sections[member.SectionId];
            double length = StructuralMember.GetLength(nodes[member.StartNodeId], nodes[member.EndNodeId]);
            try
            {
                double mass = DynamicMassUnits.MemberMassFromUnitWeight(gamma, section.Area, length);
                totalMemberMass += mass;
                if (!double.IsFinite(totalMemberMass) || totalMemberMass <= 0)
                {
                    Error("DYNAMIC_MEMBER_MASS_SCALE", $"Combined member mass became non-finite/invalid at member '{member.Id}'. Check geometry and units.", member.Id);
                    break;
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                Error("DYNAMIC_MEMBER_MASS_SCALE", $"Member '{member.Id}' yields non-finite, zero or overflowed inertial mass. Check UnitWeight, section area and length.", member.Id);
            }
        }

        HashSet<string> massIds = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> massNodeIds = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < definition.AdditionalNodalMasses.Count; i++)
        {
            StructuralNodalMass? mass = definition.AdditionalNodalMasses[i];
            if (mass is null)
            {
                Error("DYNAMIC_NODAL_MASS_NULL", $"dynamics.nodalMasses[{i}] cannot be null.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(mass.Id))
                Error("DYNAMIC_NODAL_MASS_ID_REQUIRED", $"dynamics.nodalMasses[{i}].id is required.");
            else if (!massIds.Add(mass.Id))
                Error("DYNAMIC_NODAL_MASS_DUPLICATE_ID", $"Duplicate nodal mass id '{mass.Id}'.", mass.Id);

            if (string.IsNullOrWhiteSpace(mass.NodeId) || !nodes.ContainsKey(mass.NodeId))
                Error("DYNAMIC_NODAL_MASS_NODE_NOT_FOUND", $"Nodal mass '{mass.Id}' references missing node '{mass.NodeId}'.", mass.Id);
            else
            {
                if (!massNodeIds.Add(mass.NodeId))
                    Error("DYNAMIC_NODAL_MASS_DUPLICATE_NODE", $"Node '{mass.NodeId}' has multiple additional masses; combine them into one entry to prevent double counting.", mass.Id);
                if (!connectedNodeIds.Contains(mass.NodeId))
                    Error("DYNAMIC_NODAL_MASS_DISCONNECTED", $"Mass '{mass.Id}' belongs to an isolated node '{mass.NodeId}'.", mass.Id);
                if (mass.RotationalInertia.HasValue && !frameNodeIds.Contains(mass.NodeId))
                    Error("DYNAMIC_NODAL_INERTIA_TRUSS_ONLY", $"Rotational inertia at node '{mass.NodeId}' has no Frame2D rotational degree of freedom.", mass.Id);
            }
            if (!double.IsFinite(mass.TranslationalMass) || mass.TranslationalMass <= 0)
                Error("DYNAMIC_NODAL_MASS_INVALID", $"Nodal mass '{mass.Id}' must have positive finite translationalMass [kN*s²/m].", mass.Id);
            if (mass.RotationalInertia is { } inertia && (!double.IsFinite(inertia) || inertia <= 0))
                Error("DYNAMIC_NODAL_INERTIA_INVALID", $"Nodal mass '{mass.Id}' must have positive finite rotationalInertia [kN*s²*m], when provided.", mass.Id);
        }

        return issues.Count == 0 ? StructuralModelValidationResult.Success : new StructuralModelValidationResult(issues);
    }
}
