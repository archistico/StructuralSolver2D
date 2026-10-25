using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Combinations.Audit;

/// <summary>
/// Domain-separated SHA-256 over actual Core input state and the actual
/// combination terms. Includes geometry, restraints, materials/sections and
/// all characteristic loads: crucial for SelfWeight and changed load cases.
/// Other existing model combinations are deliberately excluded, so adding
/// the audited candidate does not alter the model-input fingerprint.
/// </summary>
public static class CoreCombinationInputFingerprint
{
    public static string Compute(StructuralModel model, ActionInventory inventory,
        Ntc2018DeclaredCombinationPlan plan, StructuralLoadCombination combination)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(combination);

        StringBuilder buffer = new();

        // Length-prefixed fields avoid delimiter injection/ambiguous concatenation.
        void Add(string? value)
        {
            if (value is null)
            {
                buffer.Append("N;");
                return;
            }
            buffer.Append('S').Append(value.Length).Append(':').Append(value).Append(';');
        }
        void Num(double value) => Add(value.ToString("R", CultureInfo.InvariantCulture));
        void NumNullable(double? value) => Add(value.HasValue ? value.Value.ToString("R", CultureInfo.InvariantCulture) : null);
        void Flag(bool value) => Add(value ? "1" : "0");
        IEnumerable<T> Ordered<T>(IEnumerable<T> source, Func<T, string> key) =>
            source.OrderBy(key, StringComparer.OrdinalIgnoreCase).ThenBy(key, StringComparer.Ordinal);

        Add("StructuralSolver2D:P0-B4:CoreInput:v1");
        Add(JsonSerializer.Serialize(plan));
        Add(ActionInventoryJsonSerializer.Serialize(inventory));
        Add(combination.Id);

        Add("NODES");
        foreach (StructuralNode node in Ordered(model.Nodes, x => x.Id))
        {
            Add(node.Id); Num(node.X); Num(node.Y); Add(node.Label);
        }
        Add("MATERIALS");
        foreach (StructuralMaterial material in Ordered(model.Materials, x => x.Id))
        {
            Add(material.Id); Add(material.Name); Num(material.ElasticModulus); NumNullable(material.UnitWeight);
        }
        Add("SECTIONS");
        foreach (StructuralSection section in Ordered(model.Sections, x => x.Id))
        {
            Add(section.Id); Add(section.Name); Num(section.Area); Num(section.MomentOfInertia);
            NumNullable(section.Height); NumNullable(section.Width);
        }
        Add("MEMBERS");
        foreach (StructuralMember member in Ordered(model.Members, x => x.Id))
        {
            Add(member.Id); Add(member.StartNodeId); Add(member.EndNodeId);
            Add(member.MaterialId); Add(member.SectionId); Add(member.Type.ToString());
            Add(member.Label); Flag(member.ReleaseStartMoment); Flag(member.ReleaseEndMoment);
        }
        Add("SUPPORTS");
        foreach (StructuralSupport support in Ordered(model.Supports, x => x.Id))
        {
            Add(support.Id); Add(support.NodeId); Flag(support.RestrainedUx);
            Flag(support.RestrainedUy); Flag(support.RestrainedRz);
            Add(support.Type.ToString()); Add(support.Label); Num(support.OrientationDegrees);
            Num(support.StiffnessUx); Num(support.StiffnessUy); Num(support.StiffnessRz);
        }
        Add("LOAD_CASES");
        foreach (StructuralLoadCase loadCase in Ordered(model.LoadCases, x => x.Id))
        {
            Add(loadCase.Id); Add(loadCase.Name); Add(loadCase.Description);
        }
        Add("CHARACTERISTIC_LOADS");
        foreach (StructuralLoad load in Ordered(model.Loads, x => x.Id))
        {
            Add(load.Id); Add(load.LoadCaseId); Add(load.Type.ToString());
            Add(load.TargetType.ToString()); Add(load.TargetId); Add(load.Direction.ToString());
            Num(load.Value); NumNullable(load.Position); NumNullable(load.EndValue); Add(load.Label);
        }
        Add("COMBINATION_TERMS");
        foreach (StructuralLoadCombinationTerm term in Ordered(combination.Terms, x => x.LoadCaseId))
        {
            Add(term.LoadCaseId); Num(term.Factor);
        }

        byte[] digest = SHA256.HashData(Encoding.UTF8.GetBytes(buffer.ToString()));
        return Convert.ToHexString(digest);
    }
}
