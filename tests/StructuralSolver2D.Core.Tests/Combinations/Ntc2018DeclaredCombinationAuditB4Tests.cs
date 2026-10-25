using StructuralSolver2D.Combinations.Actions;
using StructuralSolver2D.Combinations.Audit;
using StructuralSolver2D.Combinations.Generation;
using StructuralSolver2D.Core.Model;
using StructuralSolver2D.Core.Model.Enums;
using StructuralSolver2D.Core.Validation;

namespace StructuralSolver2D.Core.Tests.Combinations;

public sealed class Ntc2018DeclaredCombinationAuditB4Tests
{
    [Fact]
    public void CompleteDeclaredSevenScenarioPlan_MatchesActualFemInputs_ButNotGlobalNormativeClosure()
    {
        (StructuralModel model, ActionInventory inventory, Ntc2018DeclaredCombinationPlan plan) = Fixture();
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
        Assert.Equal(Ntc2018DeclaredAuditStatus.MatchedDeclaredPlan,
            Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan).Status);

        Ntc2018CombinationAuditBaseline baseline = Ntc2018DeclaredCombinationAuditor.Capture(inventory, model, plan);
        Assert.Equal(7, baseline.Candidates.Count);
        InstallCoreCombinations(model, baseline);

        Ntc2018DeclaredAuditResult result = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);

        Assert.Equal(Ntc2018DeclaredAuditStatus.MatchedDeclaredPlan, result.Status);
        Assert.Empty(result.Issues);
        Assert.False(result.GlobalNormativeCompletenessEstablished);
        Assert.False(result.MemberAdequacyEstablished);
        Assert.True(new StructuralModelValidator().Validate(model).IsValid);
    }

    [Fact]
    public void MissingApplicableWindLeader_IsIncomplete()
    {
        var (model, inventory, plan) = Fixture();
        plan = plan with { Scenarios = plan.Scenarios.Where(x => x.CombinationId != "AUD_ULS_W").ToArray() };
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Incomplete, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_LEADING_VARIANT_MISSING" && x.CombinationId == "W");
        Assert.Throws<ArgumentException>(() => Ntc2018DeclaredCombinationAuditor.Capture(inventory, model, plan));
    }

    [Fact]
    public void MissingDeclaredFamily_IsIncomplete()
    {
        var (model, inventory, plan) = Fixture();
        plan = plan with { Scenarios = plan.Scenarios.Where(x => x.Family != Ntc2018CombinationFamily.SlsFrequent).ToArray() };
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Incomplete, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_FAMILY_NOT_COVERED");
    }

    [Fact]
    public void InScopeActionCannotDisappearFromScenarioWithoutExplanation()
    {
        var (model, inventory, plan) = Fixture();
        plan = plan with
        {
            Scenarios = plan.Scenarios.Select(x => x.CombinationId == "AUD_ULS_QA"
                ? x with { PresentActionIds = new[] { "G1", "QA" } } : x).ToArray()
        };
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Incomplete, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_SCENARIO_ACTION_UNRESOLVED" && x.CombinationId == "AUD_ULS_QA");
    }

    [Fact]
    public void AllInventoryActionsMustBeExplicitlyInScopeOrExcludedWithEvidence()
    {
        var (model, inventory, plan) = Fixture();
        plan = plan with { InScopeActionIds = new[] { "G1", "QA" } };
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan);
        Assert.Contains(check.Issues, x => x.Code == "B4_ACTION_UNCLASSIFIED" && x.CombinationId == "W");
    }

    [Fact]
    public void PhysicalCompatibilityEvidenceIsMandatory()
    {
        var (model, inventory, plan) = Fixture();
        plan = plan with { PhysicalCompatibilityEvidenceId = "" };
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Incomplete, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_COMPATIBILITY_EVIDENCE_MISSING");
    }

    [Fact]
    public void PhysicallyAbsentActionWithEvidence_IsNotConfusedWithSilentlyUnselectedAction()
    {
        var (model, inventory, plan) = Fixture();
        plan = plan with { Scenarios = plan.Scenarios.Select(x => x.CombinationId == "AUD_ULS_QA"
            ? x with
            {
                PresentActionIds = new[] { "G1", "QA" },
                PhysicallyAbsentActionEvidence = new Dictionary<string, string> { ["W"] = "ENGINEER_NO_WIND_FOR_SCENARIO" }
            }
            : x).ToArray() };
        Assert.Equal(Ntc2018DeclaredAuditStatus.MatchedDeclaredPlan,
            Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan).Status);
        Assert.Equal(7, Ntc2018DeclaredCombinationAuditor.Capture(inventory, model, plan).Candidates.Count);
    }

    [Fact]
    public void ExplicitExclusiveWindAlternatives_RequireBothLeaderVariants()
    {
        var (model, inventory, plan) = Fixture();
        model.AddLoadCase(new StructuralLoadCase("LC_WM", "Opposite wind"))
            .AddLoad(StructuralLoad.NodalForce("L_WM", "LC_WM", "B", StructuralLoadDirection.GlobalX, -5));
        inventory = inventory with { Actions = inventory.Actions.Select(x => x.ActionId == "W"
            ? x with { AlternativeGroupId = "WIND_DIRECTIONS" } : x)
            .Concat(new[] { new ActionDefinition("WM", "LC_WM", ActionNature.Q, VariableActionCategory.Wind,
                ActionValueBasis.Characteristic, "SRC-WM", "HOM-WM", "WIND_DIRECTIONS") }).ToArray() };
        plan = plan with { InScopeActionIds = new[] { "G1", "QA", "W", "WM" } };

        Ntc2018DeclaredAuditResult missing = Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Incomplete, missing.Status);
        Assert.Contains(missing.Issues, x => x.Code == "B4_LEADING_VARIANT_MISSING" && x.CombinationId == "WM");

        Ntc2018CombinationScenario[] wmScenarios =
        {
            new("AUD_ULS_WM", plan.EffectId, Ntc2018CombinationFamily.UlsStrA1,
                new[] { "G1", "QA", "WM" }, "WM", Array.Empty<string>(), "EVIDENCE"),
            new("AUD_RARE_WM", plan.EffectId, Ntc2018CombinationFamily.SlsRare,
                new[] { "G1", "QA", "WM" }, "WM", Array.Empty<string>(), "EVIDENCE"),
            new("AUD_FREQ_WM", plan.EffectId, Ntc2018CombinationFamily.SlsFrequent,
                new[] { "G1", "QA", "WM" }, "WM", Array.Empty<string>(), "EVIDENCE"),
            new("AUD_QUASI_WM", plan.EffectId, Ntc2018CombinationFamily.SlsQuasiPermanent,
                new[] { "G1", "QA", "WM" }, null, Array.Empty<string>(), "EVIDENCE")
        };
        plan = plan with { Scenarios = plan.Scenarios.Concat(wmScenarios).ToArray() };
        Assert.Equal(Ntc2018DeclaredAuditStatus.MatchedDeclaredPlan,
            Ntc2018DeclaredPlanValidator.Validate(inventory, model, plan).Status);
        Assert.Equal(11, Ntc2018DeclaredCombinationAuditor.Capture(inventory, model, plan).Candidates.Count);
    }

    [Fact]
    public void ARequiredScenarioMissingFromActualFemModel_IsIncomplete()
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        model.LoadCombinations.RemoveAt(0);
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Incomplete, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_REQUIRED_COMBINATION_MISSING");
    }

    [Fact]
    public void ChangedActualFemFactor_InvalidatesCapturedCoreInput()
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        StructuralLoadCombination actual = model.LoadCombinations[0];
        StructuralLoadCombinationTerm first = actual.Terms[0];
        model.LoadCombinations[0] = ReplaceTerms(actual,
            actual.Terms.Select((x, index) => index == 0
                ? new StructuralLoadCombinationTerm(first.LoadCaseId, first.Factor + 0.01)
                : x).ToArray());

        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Invalid, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_TERM_FACTOR_MISMATCH");
        Assert.Contains(check.Issues, x => x.Code == "B4_CORE_INPUT_FINGERPRINT_MISMATCH");
    }

    [Fact]
    public void MissingActualFemTerm_IsNotSilentlyAccepted()
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        StructuralLoadCombination actual = model.LoadCombinations[0];
        model.LoadCombinations[0] = ReplaceTerms(actual, actual.Terms.Skip(1).ToArray());
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Contains(check.Issues, x => x.Code == "B4_REQUIRED_TERM_MISSING");
        Assert.NotEqual(Ntc2018DeclaredAuditStatus.MatchedDeclaredPlan, check.Status);
    }

    [Fact]
    public void UnexpectedActualFemTerm_IsInvalid()
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        StructuralLoadCombination actual = model.LoadCombinations[0];
        model.LoadCombinations[0] = ReplaceTerms(actual, actual.Terms
            .Concat(new[] { new StructuralLoadCombinationTerm("LC_UNDECLARED", 1.0) }).ToArray());
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Invalid, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_UNEXPECTED_TERM");
    }

    [Fact]
    public void DuplicateActualFemCombination_IsInvalid()
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        StructuralLoadCombination actual = model.LoadCombinations[0];
        model.AddLoadCombination(new StructuralLoadCombination(actual.Id.ToLowerInvariant(), "Duplicate", actual.Terms));
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Invalid, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_DUPLICATE_ACTUAL_COMBINATION");
    }

    [Fact]
    public void AddedUndeclaredFemCombination_IsInvalid()
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        model.AddLoadCombination(new StructuralLoadCombination("EXTRA", "Not planned",
            new[] { new StructuralLoadCombinationTerm("LC_G1", 1.0) }));
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Invalid, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_UNDECLARED_COMBINATION");
    }

    [Theory]
    [InlineData("Load")]
    [InlineData("Geometry")]
    [InlineData("Material")]
    [InlineData("Section")]
    [InlineData("Release")]
    [InlineData("Support")]
    [InlineData("SelfWeight")]
    public void AnyPhysicalFemInputMutation_IsDetectedByFingerprint(string mutation)
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        switch (mutation)
        {
            case "Load": model.Loads[0] = model.Loads[0] with { Value = -11.0 }; break;
            case "Geometry": model.Nodes[1] = model.Nodes[1] with { X = 6.0 }; break;
            case "Material": model.Materials[0] = model.Materials[0] with { ElasticModulus = 200_000_000 }; break;
            case "Section": model.Sections[0] = model.Sections[0] with { Area = 0.004 }; break;
            case "Release": model.Members[0] = model.Members[0] with { ReleaseStartMoment = true }; break;
            case "Support": model.Supports[0] = model.Supports[0] with { RestrainedUx = false }; break;
            case "SelfWeight": model.Materials[0] = model.Materials[0] with { UnitWeight = 80.0 }; break;
        }
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Invalid, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_CORE_INPUT_FINGERPRINT_MISMATCH");
    }

    [Fact]
    public void ChangingEvidenceAfterCaptureAlsoInvalidatesBaseline()
    {
        var (model, inventory, plan, baseline) = CapturedAndInstalled();
        plan = plan with { PhysicalCompatibilityEvidenceId = "ANOTHER_ENGINEERING_EVIDENCE" };
        Ntc2018DeclaredAuditResult check = Ntc2018DeclaredCombinationAuditor.Audit(inventory, model, plan, baseline);
        Assert.Equal(Ntc2018DeclaredAuditStatus.Invalid, check.Status);
        Assert.Contains(check.Issues, x => x.Code == "B4_CORE_INPUT_FINGERPRINT_MISMATCH");
    }

    [Fact]
    public void BaselineCaptureIsPureAndDoesNotInsertAnyFemCombination()
    {
        var (model, inventory, plan) = Fixture();
        Ntc2018CombinationAuditBaseline baseline = Ntc2018DeclaredCombinationAuditor.Capture(inventory, model, plan);
        Assert.Equal(7, baseline.Candidates.Count);
        Assert.Empty(model.LoadCombinations);
        Assert.All(baseline.Candidates, x => Assert.Equal(64, x.CoreInputSha256.Length));
    }

    private static (StructuralModel Model, ActionInventory Inventory, Ntc2018DeclaredCombinationPlan Plan,
        Ntc2018CombinationAuditBaseline Baseline) CapturedAndInstalled()
    {
        var (model, inventory, plan) = Fixture();
        Ntc2018CombinationAuditBaseline baseline = Ntc2018DeclaredCombinationAuditor.Capture(inventory, model, plan);
        InstallCoreCombinations(model, baseline);
        return (model, inventory, plan, baseline);
    }

    private static void InstallCoreCombinations(StructuralModel model, Ntc2018CombinationAuditBaseline baseline)
    {
        foreach (Ntc2018AuditedCandidate item in baseline.Candidates)
            model.AddLoadCombination(new StructuralLoadCombination(item.CombinationId, item.CombinationId,
                item.ExpectedCoreTerms.ToArray()));
    }

    private static StructuralLoadCombination ReplaceTerms(StructuralLoadCombination current,
        IReadOnlyList<StructuralLoadCombinationTerm> terms) =>
        new(current.Id, current.Name, terms);

    private static (StructuralModel Model, ActionInventory Inventory, Ntc2018DeclaredCombinationPlan Plan) Fixture()
    {
        StructuralModel model = new StructuralModel()
            .AddNode(new StructuralNode("A", 0, 0))
            .AddNode(new StructuralNode("B", 5, 0))
            .AddMaterial(new StructuralMaterial("STEEL", "S235", 210_000_000, 78.5))
            .AddSection(new StructuralSection("IPE", "Sample", 0.003, 0.00002))
            .AddMember(new StructuralMember("M", "A", "B", "STEEL", "IPE"))
            .AddSupport(StructuralSupport.Hinge("SA", "A"))
            .AddSupport(StructuralSupport.SimpleSupport("SB", "B"))
            .AddLoadCase(new StructuralLoadCase("LC_G1", "Permanent"))
            .AddLoadCase(new StructuralLoadCase("LC_QA", "Residential"))
            .AddLoadCase(new StructuralLoadCase("LC_W", "Wind"))
            .AddLoad(StructuralLoad.NodalForce("L_G1", "LC_G1", "B", StructuralLoadDirection.GlobalY, -10))
            .AddLoad(StructuralLoad.NodalForce("L_QA", "LC_QA", "B", StructuralLoadDirection.GlobalY, -6))
            .AddLoad(StructuralLoad.NodalForce("L_W", "LC_W", "B", StructuralLoadDirection.GlobalX, 5));

        ActionInventory inventory = new(ActionInventoryProfiles.SchemaVersion, ActionInventoryProfiles.CodeEdition,
            new ActionDefinition[]
            {
                new("G1", "LC_G1", ActionNature.G1, null, ActionValueBasis.Characteristic, "SRC-G1", "HOM-G1"),
                new("QA", "LC_QA", ActionNature.Q, VariableActionCategory.ResidentialA,
                    ActionValueBasis.Characteristic, "SRC-QA", "HOM-QA"),
                new("W", "LC_W", ActionNature.Q, VariableActionCategory.Wind,
                    ActionValueBasis.Characteristic, "SRC-W", "HOM-W")
            });

        string[] all = { "G1", "QA", "W" };
        string effect = "M_AB_POS";
        Ntc2018CombinationScenario[] scenarios =
        {
            Scenario("AUD_ULS_QA", Ntc2018CombinationFamily.UlsStrA1, "QA"),
            Scenario("AUD_ULS_W", Ntc2018CombinationFamily.UlsStrA1, "W"),
            Scenario("AUD_RARE_QA", Ntc2018CombinationFamily.SlsRare, "QA"),
            Scenario("AUD_RARE_W", Ntc2018CombinationFamily.SlsRare, "W"),
            Scenario("AUD_FREQ_QA", Ntc2018CombinationFamily.SlsFrequent, "QA"),
            Scenario("AUD_FREQ_W", Ntc2018CombinationFamily.SlsFrequent, "W"),
            Scenario("AUD_QUASI", Ntc2018CombinationFamily.SlsQuasiPermanent, null)
        };

        Ntc2018CombinationScenario Scenario(string id, Ntc2018CombinationFamily family, string? lead) =>
            new(id, effect, family, all, lead, Array.Empty<string>(), "ENGINEER_FAVORABILITY");

        Ntc2018DeclaredCombinationPlan plan = new("BUILDING_EFFECT_PLAN_V1", effect,
            new[] { Ntc2018CombinationFamily.UlsStrA1, Ntc2018CombinationFamily.SlsRare,
                Ntc2018CombinationFamily.SlsFrequent, Ntc2018CombinationFamily.SlsQuasiPermanent },
            all, new Dictionary<string, string>(), new Dictionary<string, string>(),
            "ENGINEER_SCENARIO_COMPATIBILITY", scenarios);
        return (model, inventory, plan);
    }
}
