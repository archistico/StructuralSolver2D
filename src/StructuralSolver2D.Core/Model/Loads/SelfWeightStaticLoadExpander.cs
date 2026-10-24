using StructuralSolver2D.Core.Model.Enums;

namespace StructuralSolver2D.Core.Model.Loads;

/// <summary>
/// Expands an explicit, whole-model self-weight action into statically equivalent loads.
/// Frame2D elements receive a true global-Y uniform distributed load; Truss2D elements
/// receive two equivalent nodal forces. It never mutates the caller's model or adds
/// self-weight implicitly. This conversion is ONLY for linear static analysis.
/// </summary>
public static class SelfWeightStaticLoadExpander
{
    public static StructuralModel Expand(StructuralModel source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.Loads.Any(load => load.Type == StructuralLoadType.SelfWeight))
            return source;

        var nodes = source.Nodes.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var materials = source.Materials.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var sections = source.Sections.ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);
        var takenIds = new HashSet<string>(source.Loads.Select(item => item.Id), StringComparer.OrdinalIgnoreCase);
        var expanded = new StructuralModel();
        foreach (var node in source.Nodes) expanded.AddNode(node);
        foreach (var member in source.Members) expanded.AddMember(member);
        foreach (var material in source.Materials) expanded.AddMaterial(material);
        foreach (var section in source.Sections) expanded.AddSection(section);
        foreach (var support in source.Supports) expanded.AddSupport(support);
        foreach (var loadCase in source.LoadCases) expanded.AddLoadCase(loadCase);
        foreach (var combination in source.LoadCombinations) expanded.AddLoadCombination(combination);
        foreach (var load in source.Loads.Where(item => item.Type != StructuralLoadType.SelfWeight))
            expanded.AddLoad(load);

        foreach (var action in source.Loads.Where(item => item.Type == StructuralLoadType.SelfWeight))
        {
            if (action.TargetType != StructuralLoadTargetType.Model ||
                !string.IsNullOrWhiteSpace(action.TargetId) ||
                action.Direction != StructuralLoadDirection.GlobalY ||
                !double.IsFinite(action.Value) || action.Value <= 0)
                throw new ArgumentException($"Invalid self-weight action '{action.Id}'. Use model target, GlobalY and a positive dimensionless scale.");

            foreach (var member in source.Members)
            {
                var material = materials[member.MaterialId];
                var section = sections[member.SectionId];
                if (!material.UnitWeight.HasValue || !double.IsFinite(material.UnitWeight.Value) || material.UnitWeight <= 0)
                    throw new ArgumentException($"Member '{member.Id}' lacks valid UnitWeight [kN/m³] for self-weight '{action.Id}'.");
                var start = nodes[member.StartNodeId];
                var end = nodes[member.EndNodeId];
                double length = StructuralMember.GetLength(start, end);
                double downwardLineLoad = -material.UnitWeight.Value * section.Area * action.Value;
                if (!double.IsFinite(downwardLineLoad) || !double.IsFinite(length) || length <= 0 ||
                    !double.IsFinite(downwardLineLoad * length))
                    throw new ArgumentException($"Self-weight '{action.Id}' on member '{member.Id}' cannot be represented by finite loads.");

                if (member.Type == MemberType.Frame2D)
                {
                    expanded.AddLoad(StructuralLoad.UniformDistributedLoad(
                        NewId(action.Id, member.Id, "q", takenIds), action.LoadCaseId, member.Id,
                        StructuralLoadDirection.GlobalY, downwardLineLoad,
                        $"Self-weight {action.Id} / {member.Id}"));
                }
                else if (member.Type == MemberType.Truss2D)
                {
                    // Linear truss shape functions: integral(N_i*q dx) = q*L/2.
                    // The transverse component is represented as nodal loading, NOT bending.
                    double endForce = downwardLineLoad * length / 2.0;
                    expanded.AddLoad(StructuralLoad.NodalForce(
                        NewId(action.Id, member.Id, "i", takenIds), action.LoadCaseId, member.StartNodeId,
                        StructuralLoadDirection.GlobalY, endForce,
                        $"Self-weight {action.Id} / {member.Id} start"));
                    expanded.AddLoad(StructuralLoad.NodalForce(
                        NewId(action.Id, member.Id, "j", takenIds), action.LoadCaseId, member.EndNodeId,
                        StructuralLoadDirection.GlobalY, endForce,
                        $"Self-weight {action.Id} / {member.Id} end"));
                }
                else
                    throw new ArgumentException($"Unsupported member type '{member.Type}' for self-weight.");
            }
        }
        return expanded;
    }

    private static string NewId(string actionId, string memberId, string suffix, HashSet<string> taken)
    {
        string prefix = $"__sw_{actionId}_{memberId}_{suffix}";
        string candidate = prefix;
        int counter = 0;
        while (!taken.Add(candidate)) candidate = $"{prefix}_{++counter}";
        return candidate;
    }
}
