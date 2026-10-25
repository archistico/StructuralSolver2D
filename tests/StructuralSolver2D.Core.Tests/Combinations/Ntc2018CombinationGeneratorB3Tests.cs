using System.Globalization;
using System.Text.Json;
using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Combinations.Generation;
using StructuralSolver2D.Core.Model;

namespace StructuralSolver2D.Core.Tests.Combinations;

public sealed class Ntc2018CombinationGeneratorB3Tests
{
    public static IEnumerable<object[]> GoldenIds()
    {
        using JsonDocument fixture = OpenGolden();
        foreach (JsonElement item in fixture.RootElement.GetProperty("cases").EnumerateArray())
            yield return new object[] { item.GetProperty("id").GetString()! };
    }

    [Theory]
    [MemberData(nameof(GoldenIds))]
    public void GeneratedTermsAndOmissions_MatchManuallyDerivedIndependentP0B1Golden(string caseId)
    {
        using JsonDocument fixture = OpenGolden();
        JsonElement root = fixture.RootElement;
        JsonElement reference = root.GetProperty("cases").EnumerateArray()
            .First(c => string.Equals(c.GetProperty("id").GetString(), caseId, StringComparison.Ordinal));
        (StructuralModel model, ActionInventory inventory) = BuildFixtureInventory(root.GetProperty("actions"));
        Assert.True(ActionInventoryValidator.Validate(inventory, model).IsValid);

        Ntc2018GeneratedCombination generated = Ntc2018BuildingCombinationGenerator.Generate(
            inventory, model, ScenarioFromGolden(reference));

        Dictionary<string, Ntc2018GeneratedTerm> actualTerms = generated.Manifest.Terms
            .ToDictionary(x => x.ActionId, StringComparer.OrdinalIgnoreCase);
        JsonElement expected = reference.GetProperty("factors");
        Assert.Equal(expected.EnumerateObject().Count(), actualTerms.Count);
        foreach (JsonProperty factor in expected.EnumerateObject())
        {
            Assert.True(actualTerms.TryGetValue(factor.Name, out Ntc2018GeneratedTerm? actual));
            Assert.Equal(Dec(factor.Value.GetString()!), actual!.EffectiveFactor);
            Assert.Equal(actual.Gamma * actual.Psi, actual.EffectiveFactor);
            Assert.NotEqual(0m, actual.EffectiveFactor);
        }

        HashSet<string> present = reference.GetProperty("present").EnumerateArray()
            .Select(x => x.GetString()!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Ntc2018OmittedAction[] actualIncludedOmissions = generated.Manifest.OmittedActions
            .Where(x => present.Contains(x.ActionId)).ToArray();
        JsonElement omissions = reference.GetProperty("omitted");
        Assert.Equal(omissions.EnumerateObject().Count(), actualIncludedOmissions.Length);
        foreach (JsonProperty omitted in omissions.EnumerateObject())
        {
            Ntc2018OmittedAction item = Assert.Single(actualIncludedOmissions,
                x => string.Equals(x.ActionId, omitted.Name, StringComparison.OrdinalIgnoreCase));
            Ntc2018OmissionReason expectedReason = omitted.Value.GetString() switch
            {
                "Favorable" => Ntc2018OmissionReason.FavorableEffect,
                "PsiZero" => Ntc2018OmissionReason.ZeroPsi,
                _ => throw new InvalidOperationException("Unexpected independent golden omission.")
            };
            Assert.Equal(expectedReason, item.Reason);
        }

        decimal effect = generated.Manifest.Terms.Sum(term =>
            Dec(root.GetProperty("actions").GetProperty(term.ActionId).GetProperty("effect").GetString()!) *
            term.EffectiveFactor);
        Assert.Equal(Dec(reference.GetProperty("expectedEffect").GetString()!), effect);
        Assert.Equal(caseId, generated.FemCombination.Id);
        Assert.Equal(generated.Manifest.Terms.Count, generated.FemCombination.Terms.Count);
        Assert.All(generated.FemCombination.Terms, term => Assert.NotEqual(0.0, term.Factor));
        Assert.False(generated.Manifest.FullCombinationSetAudited);
        Assert.Empty(model.LoadCombinations); // Pure candidate generation never modifies Core.
    }

    [Theory]
    [InlineData(VariableActionCategory.ResidentialA, "0.7", "0.5", "0.3")]
    [InlineData(VariableActionCategory.OfficeB, "0.7", "0.5", "0.3")]
    [InlineData(VariableActionCategory.AssemblyC, "0.7", "0.7", "0.6")]
    [InlineData(VariableActionCategory.CommercialD, "0.7", "0.7", "0.6")]
    [InlineData(VariableActionCategory.StorageE, "1.0", "0.9", "0.8")]
    [InlineData(VariableActionCategory.LightParkingF, "0.7", "0.7", "0.6")]
    [InlineData(VariableActionCategory.HeavyParkingG, "0.7", "0.5", "0.3")]
    [InlineData(VariableActionCategory.MaintenanceRoofH, "0", "0", "0")]
    [InlineData(VariableActionCategory.Wind, "0.6", "0.2", "0")]
    [InlineData(VariableActionCategory.SnowAtOrBelow1000Metres, "0.5", "0.2", "0")]
    [InlineData(VariableActionCategory.SnowAbove1000Metres, "0.7", "0.5", "0.2")]
    [InlineData(VariableActionCategory.Temperature, "0.6", "0.5", "0")]
    public void PsiTable_MatchesPrimaryNtc2018Table25I(
        VariableActionCategory category, string psi0, string psi1, string psi2)
    {
        Assert.Equal((Dec(psi0), Dec(psi1), Dec(psi2)),
            Ntc2018BuildingActionCoefficients.Psi(category));
    }

    [Fact]
    public void GammaStrA1_MatchesTable26I_AndDocumentedG2Exception()
    {
        Assert.Equal(1.0m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.G1, true));
        Assert.Equal(1.3m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.G1, false));
        Assert.Equal(0.8m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.G2, true));
        Assert.Equal(1.5m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.G2, false));
        Assert.Equal(0m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.Q, true));
        Assert.Equal(1.5m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.Q, false));
        Assert.Equal(1.0m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.G2, true, true));
        Assert.Equal(1.3m, Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.G2, false, true));
        Assert.Throws<ArgumentException>(() =>
            Ntc2018BuildingActionCoefficients.GammaStrA1(ActionNature.G1, false, true));
    }

    [Fact]
    public void Generator_UsesG1GammaForG2OnlyWithExplicitEvidence()
    {
        StructuralModel model = Model("LC_G2", "LC_Q");
        ActionInventory inventory = Inventory(
            new ActionDefinition("G2", "LC_G2", ActionNature.G2, null,
                ActionValueBasis.Characteristic, "SRC", "HOM", TreatG2AsG1ByEvidence: true,
                G2CoefficientExceptionEvidenceId: "ENGINEER-G2-KNOWN-MAGNITUDE"),
            Variable("QA", "LC_Q", VariableActionCategory.ResidentialA));
        Ntc2018CombinationScenario request = Request(
            Ntc2018CombinationFamily.UlsStrA1, new[] { "G2", "QA" }, "QA");

        Ntc2018GeneratedCombination generated = Ntc2018BuildingCombinationGenerator.Generate(inventory, model, request);

        Ntc2018GeneratedTerm term = Assert.Single(generated.Manifest.Terms, x => x.ActionId == "G2");
        Assert.Equal(1.3m, term.EffectiveFactor);
        Assert.Equal("ENGINEER-G2-KNOWN-MAGNITUDE", term.ExceptionEvidenceId);
    }

    [Fact]
    public void MutuallyExclusiveWinds_CannotBeCombinedInOneScenario()
    {
        StructuralModel model = Model("LC_WP", "LC_WM");
        ActionInventory inventory = Inventory(
            Variable("W+", "LC_WP", VariableActionCategory.Wind, "WIND"),
            Variable("W-", "LC_WM", VariableActionCategory.Wind, "WIND"));

        Assert.Throws<ArgumentException>(() => Ntc2018BuildingCombinationGenerator.Generate(
            inventory, model, Request(Ntc2018CombinationFamily.UlsStrA1,
                new[] { "W+", "W-" }, "W+")));
    }

    [Fact]
    public void MissingLeader_WhenVariablePresent_IsRejected()
    {
        (StructuralModel model, ActionInventory inventory) = OneVariableFixture();
        Assert.Throws<ArgumentException>(() => Ntc2018BuildingCombinationGenerator.Generate(
            inventory, model, Request(Ntc2018CombinationFamily.SlsRare, new[] { "QA" }, null)));
    }

    [Fact]
    public void QuasiPermanentMustNotDeclareLeader()
    {
        (StructuralModel model, ActionInventory inventory) = OneVariableFixture();
        Assert.Throws<ArgumentException>(() => Ntc2018BuildingCombinationGenerator.Generate(
            inventory, model, Request(Ntc2018CombinationFamily.SlsQuasiPermanent,
                new[] { "QA" }, "QA")));
    }

    [Fact]
    public void FavorabilityWithoutDeclaredEffectEvidence_IsRejected()
    {
        (StructuralModel model, ActionInventory inventory) = OneVariableFixture();
        Ntc2018CombinationScenario request = Request(
            Ntc2018CombinationFamily.UlsStrA1, new[] { "QA" }, "QA") with
        { FavorabilityEvidenceId = "" };
        Assert.Throws<ArgumentException>(() =>
            Ntc2018BuildingCombinationGenerator.Generate(inventory, model, request));
    }

    [Fact]
    public void GeneratedIdCannotCollideWithAnExistingFemLoadCase()
    {
        (StructuralModel model, ActionInventory inventory) = OneVariableFixture();
        Ntc2018CombinationScenario request = Request(
            Ntc2018CombinationFamily.UlsStrA1, new[] { "QA" }, "QA") with
        { CombinationId = "lc_q" };
        Assert.Throws<ArgumentException>(() =>
            Ntc2018BuildingCombinationGenerator.Generate(inventory, model, request));
    }

    [Fact]
    public void FavorableIdCannotReferenceActionOutsideScenario()
    {
        StructuralModel model = Model("LC_Q", "LC_G1");
        ActionInventory inventory = Inventory(Variable("QA", "LC_Q", VariableActionCategory.ResidentialA),
            new ActionDefinition("G1", "LC_G1", ActionNature.G1, null,
                ActionValueBasis.Characteristic, "SRC-G1", "HOM-G1"));
        Ntc2018CombinationScenario request = Request(
            Ntc2018CombinationFamily.UlsStrA1, new[] { "QA" }, "QA") with
        { FavorableActionIds = new[] { "G1" } };
        Assert.Throws<ArgumentException>(() =>
            Ntc2018BuildingCombinationGenerator.Generate(inventory, model, request));
    }

    [Fact]
    public void PhysicallyAbsentEvidence_CannotContradictIncludedAction()
    {
        (StructuralModel model, ActionInventory inventory) = OneVariableFixture();
        Ntc2018CombinationScenario request = Request(
            Ntc2018CombinationFamily.UlsStrA1, new[] { "QA" }, "QA") with
        { PhysicallyAbsentActionEvidence = new Dictionary<string, string> { ["QA"] = "ABSENT" } };
        Assert.Throws<ArgumentException>(() =>
            Ntc2018BuildingCombinationGenerator.Generate(inventory, model, request));
    }

    [Fact]
    public void PhysicallyAbsentAction_IsDistinctFromNotSelectedAndFromZeroPsi()
    {
        StructuralModel model = Model("LC_G1", "LC_G2", "LC_Q");
        ActionInventory inventory = Inventory(
            new ActionDefinition("G1", "LC_G1", ActionNature.G1, null,
                ActionValueBasis.Characteristic, "SRC-G1", "HOM-G1"),
            new ActionDefinition("G2", "LC_G2", ActionNature.G2, null,
                ActionValueBasis.Characteristic, "SRC-G2", "HOM-G2"),
            Variable("QA", "LC_Q", VariableActionCategory.ResidentialA));
        Ntc2018CombinationScenario scenario = Request(
            Ntc2018CombinationFamily.UlsStrA1, new[] { "G1", "QA" }, "QA") with
        { PhysicallyAbsentActionEvidence = new Dictionary<string, string> { ["g2"] = "NOT_YET_INSTALLED" } };

        Ntc2018GeneratedCombination generated = Ntc2018BuildingCombinationGenerator.Generate(inventory, model, scenario);

        Ntc2018OmittedAction omission = Assert.Single(generated.Manifest.OmittedActions);
        Assert.Equal("G2", omission.ActionId);
        Assert.Equal(Ntc2018OmissionReason.PhysicallyAbsentByEvidence, omission.Reason);
        Assert.Equal("NOT_YET_INSTALLED", omission.EvidenceId);
    }

    [Fact]
    public void UndeclaredScenarioAction_IsRejected()
    {
        (StructuralModel model, ActionInventory inventory) = OneVariableFixture();
        Assert.Throws<ArgumentException>(() => Ntc2018BuildingCombinationGenerator.Generate(
            inventory, model, Request(Ntc2018CombinationFamily.UlsStrA1,
                new[] { "QA", "UNKNOWN" }, "QA")));
    }

    [Fact]
    public void AZeroPsiAction_IsDocumentedButNotWrittenToCore()
    {
        StructuralModel model = Model("LC_G1", "LC_H");
        ActionInventory inventory = Inventory(
            new ActionDefinition("G1", "LC_G1", ActionNature.G1, null,
                ActionValueBasis.Characteristic, "SRC-G1", "HOM-G1"),
            Variable("QH", "LC_H", VariableActionCategory.MaintenanceRoofH));

        Ntc2018GeneratedCombination generated = Ntc2018BuildingCombinationGenerator.Generate(
            inventory, model, Request(Ntc2018CombinationFamily.SlsFrequent,
                new[] { "G1", "QH" }, "QH"));

        Assert.Single(generated.FemCombination.Terms);
        Assert.Equal("LC_G1", generated.FemCombination.Terms[0].LoadCaseId);
        Ntc2018OmittedAction omitted = Assert.Single(generated.Manifest.OmittedActions);
        Assert.Equal(Ntc2018OmissionReason.ZeroPsi, omitted.Reason);
        Assert.Equal("QH", omitted.ActionId);
    }

    [Fact]
    public void IAndKRemainUnsupportedAndCannotGeneratePsiAutomatically()
    {
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018BuildingActionCoefficients.Psi(VariableActionCategory.RoofI));
        Assert.Throws<NotSupportedException>(() =>
            Ntc2018BuildingActionCoefficients.Psi(VariableActionCategory.RoofK));
    }

    [Fact]
    public void NonselectedActionsAreExplicitlyNotAuditedNotSilentlyPassed()
    {
        StructuralModel model = Model("LC_G1", "LC_Q");
        ActionInventory inventory = Inventory(
            new ActionDefinition("G1", "LC_G1", ActionNature.G1, null,
                ActionValueBasis.Characteristic, "SRC-G1", "HOM-G1"),
            Variable("QA", "LC_Q", VariableActionCategory.ResidentialA));

        Ntc2018GeneratedCombination generated = Ntc2018BuildingCombinationGenerator.Generate(
            inventory, model, Request(Ntc2018CombinationFamily.UlsStrA1, new[] { "G1" }, null));

        Ntc2018OmittedAction omission = Assert.Single(generated.Manifest.OmittedActions);
        Assert.Equal(Ntc2018OmissionReason.NotSelectedInScenario, omission.Reason);
        Assert.False(generated.Manifest.FullCombinationSetAudited);
    }

    private static (StructuralModel Model, ActionInventory Inventory) OneVariableFixture()
    {
        StructuralModel model = Model("LC_Q");
        return (model, Inventory(Variable("QA", "LC_Q", VariableActionCategory.ResidentialA)));
    }

    private static JsonDocument OpenGolden() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "P0B1_NTC2018_ACTION_GOLDEN.json")));

    private static decimal Dec(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static (StructuralModel Model, ActionInventory Inventory) BuildFixtureInventory(JsonElement reference)
    {
        StructuralModel model = new();
        List<ActionDefinition> actions = new();
        foreach (JsonProperty entry in reference.EnumerateObject())
        {
            string id = entry.Name;
            string loadCaseId = "LC_" + id;
            model.AddLoadCase(new StructuralLoadCase(loadCaseId, id));
            ActionNature nature = entry.Value.GetProperty("kind").GetString() switch
            {
                "G1" => ActionNature.G1,
                "G2" => ActionNature.G2,
                "Q" => ActionNature.Q,
                _ => throw new InvalidOperationException("Unexpected golden action nature")
            };
            VariableActionCategory? category = nature != ActionNature.Q ? null :
                entry.Value.GetProperty("category").GetString() switch
                {
                    "A" => VariableActionCategory.ResidentialA,
                    "WIND" => VariableActionCategory.Wind,
                    "SNOW_LOW" => VariableActionCategory.SnowAtOrBelow1000Metres,
                    "H" => VariableActionCategory.MaintenanceRoofH,
                    _ => throw new InvalidOperationException("Unexpected golden action category")
                };
            string? group = entry.Value.TryGetProperty("alternativeGroup", out JsonElement groupValue)
                ? groupValue.GetString() : null;
            actions.Add(new ActionDefinition(id, loadCaseId, nature, category,
                ActionValueBasis.Characteristic, "REF-" + id, "HOM-" + id, group));
        }
        return (model, Inventory(actions.ToArray()));
    }

    private static Ntc2018CombinationScenario ScenarioFromGolden(JsonElement reference)
    {
        Ntc2018CombinationFamily family = reference.GetProperty("family").GetString() switch
        {
            "ULS_STR_A1" => Ntc2018CombinationFamily.UlsStrA1,
            "SLS_RARE" => Ntc2018CombinationFamily.SlsRare,
            "SLS_FREQUENT" => Ntc2018CombinationFamily.SlsFrequent,
            "SLS_QUASI_PERMANENT" => Ntc2018CombinationFamily.SlsQuasiPermanent,
            _ => throw new InvalidOperationException("Unexpected golden family")
        };
        JsonElement leading = reference.GetProperty("lead");
        string? leader = leading.ValueKind == JsonValueKind.Null ? null : leading.GetString();
        Dictionary<string, string>? absent = reference.TryGetProperty("absentByEvidence", out JsonElement value)
            ? value.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString()!, StringComparer.OrdinalIgnoreCase)
            : null;
        return new Ntc2018CombinationScenario(reference.GetProperty("id").GetString()!,
            "GOLDEN_SCALAR_EFFECT_POSITIVE_WORSENS", family,
            reference.GetProperty("present").EnumerateArray().Select(x => x.GetString()!).ToArray(),
            leader,
            reference.GetProperty("favorable").EnumerateArray().Select(x => x.GetString()!).ToArray(),
            "P0B1_MANUAL_EFFECT_FAVORABILITY", absent);
    }

    private static ActionInventory Inventory(params ActionDefinition[] actions) =>
        new(ActionInventoryProfiles.SchemaVersion, ActionInventoryProfiles.CodeEdition, Array.AsReadOnly(actions));

    private static ActionDefinition Variable(string id, string loadCaseId,
        VariableActionCategory category, string? alternativeGroup = null) =>
        new(id, loadCaseId, ActionNature.Q, category, ActionValueBasis.Characteristic,
            "SRC-" + id, "HOM-" + id, alternativeGroup);

    private static StructuralModel Model(params string[] loadCases)
    {
        StructuralModel model = new();
        foreach (string id in loadCases)
            model.AddLoadCase(new StructuralLoadCase(id, id));
        return model;
    }

    private static Ntc2018CombinationScenario Request(Ntc2018CombinationFamily family,
        IReadOnlyList<string> present, string? leading) =>
        new("B3_CASE", "SCALAR_EFFECT", family, present, leading, Array.Empty<string>(), "ENGINEER_EFFECT_EVIDENCE");
}
