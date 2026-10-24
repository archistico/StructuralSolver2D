using StructuralSolver2D.Verification.Contracts;
using StructuralSolver2D.Verification.Engine;
using StructuralSolver2D.Verification.Rules;

namespace StructuralSolver2D.Verification.Tests.M87;

/// <summary>TEST-ONLY fictitious rule. No value here is a steel/timber normative resistance.</summary>
public sealed class DesignCheckEngineM87Tests
{
    private static readonly DesignCodeEdition TestCode = new("TEST-CODE", "EDITION-A", "ANNEX-IT-TEST");

    private static DesignCheckRequest Request(params string[] ids) => new(
        new DesignMemberContext("AB", DesignMaterial.Steel, "SECTION-01", 4),
        DesignLimitState.Ultimate,
        new NormativeProfile("PROFILE-TEST", TestCode, Array.Empty<NormativeCoefficient>()),
        new DesignEffectSnapshot("LINEAR-LC", "ULS-01", DesignEffectOrigin.FemAnalysis, 2,
            AxialForceKn: -12, BendingMomentZKnm: 3),
        ids);

    private sealed class FakeRule : IDesignCheckRule
    {
        private readonly Func<DesignCheckRequest, DesignRuleEvaluation> evaluate;
        public FakeRule(Func<DesignCheckRequest, DesignRuleEvaluation> eval) => evaluate = eval;
        public string RuleId => "TEST-NOT-NORMATIVE";
        public DesignCodeEdition Code { get; init; } = TestCode;
        public DesignMaterial Material { get; init; } = DesignMaterial.Steel;
        public DesignLimitState LimitState { get; init; } = DesignLimitState.Ultimate;
        public string SourceClause => "TEST CLAUSE, NOT A REAL STANDARD";
        public DesignRuleEvaluation Evaluate(DesignCheckRequest request) => evaluate(request);
    }

    private static DesignCheckEngine Engine(Func<DesignCheckRequest, DesignRuleEvaluation> calc) =>
        new(new[] { new FakeRule(calc) });

    private static DesignRuleEvaluation Calculated(double demand = 4, double capacity = 8) =>
        DesignRuleEvaluation.Calculated(demand, capacity, "TEST-UNIT", "Synthetic unit test only");

    [Fact]
    public void NoRulesRegistered_CannotReturnPass()
    {
        var result = new DesignCheckEngine(Array.Empty<IDesignCheckRule>()).Evaluate(Request("STEEL-BEND"));
        Assert.Equal(DesignOverallStatus.Incomplete, result.OverallStatus);
        Assert.False(result.IsComplete);
        Assert.Equal(DesignCheckStatus.Unsupported, Assert.Single(result.Results).Status);
    }

    [Fact]
    public void NoRequestedRules_CannotReturnPass()
    {
        var result = Engine(_ => Calculated()).Evaluate(Request());
        Assert.Empty(result.Results);
        Assert.Equal(DesignOverallStatus.Incomplete, result.OverallStatus);
    }

    [Fact]
    public void FullyDeclaredTestOnlyRule_CalculatesUtilizationAndTrace()
    {
        var result = Engine(_ => Calculated()).Evaluate(Request("TEST-NOT-NORMATIVE"));
        var check = Assert.Single(result.Results);
        Assert.Equal(DesignCheckStatus.Passed, check.Status);
        Assert.Equal(0.5d, check.Utilization);
        Assert.Equal("TEST CLAUSE, NOT A REAL STANDARD", check.SourceClause);
        Assert.Equal("Synthetic unit test only", check.CalculationTrace);
        Assert.True(result.IsComplete);
        Assert.Equal(DesignOverallStatus.Passed, result.OverallStatus);
    }

    [Fact]
    public void TestOnlyRule_UtilizationAboveOneFails()
    {
        var result = Engine(_ => Calculated(9, 8)).Evaluate(Request("TEST-NOT-NORMATIVE"));
        Assert.Equal(DesignCheckStatus.Failed, Assert.Single(result.Results).Status);
        Assert.Equal(DesignOverallStatus.Failed, result.OverallStatus);
    }

    [Fact]
    public void TestOnlyRule_UtilizationExactlyOnePasses()
    {
        var result = Engine(_ => Calculated(8, 8)).Evaluate(Request("TEST-NOT-NORMATIVE"));
        Assert.Equal(DesignCheckStatus.Passed, Assert.Single(result.Results).Status);
    }

    [Fact]
    public void DifferentEditionOrAnnex_IsNeverEvaluated()
    {
        var r = Request("TEST-NOT-NORMATIVE") with
        {
            Profile = new NormativeProfile("P", new DesignCodeEdition("TEST-CODE", "EDITION-B", "ANNEX-IT-TEST"), Array.Empty<NormativeCoefficient>())
        };
        var result = Engine(_ => Calculated()).Evaluate(r);
        Assert.Equal(DesignCheckStatus.Unsupported, Assert.Single(result.Results).Status);
    }

    [Fact]
    public void DifferentMaterialOrLimitState_IsNotApplicable()
    {
        var r = Request("TEST-NOT-NORMATIVE") with
        {
            Member = new DesignMemberContext("AB", DesignMaterial.Timber, "WOOD", 4)
        };
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Assert.Single(Engine(_ => Calculated()).Evaluate(r).Results).Status);
        r = Request("TEST-NOT-NORMATIVE") with { LimitState = DesignLimitState.Serviceability };
        Assert.Equal(DesignCheckStatus.NotApplicable,
            Assert.Single(Engine(_ => Calculated()).Evaluate(r).Results).Status);
    }

    [Fact]
    public void MissingOutOfPlaneInformation_RemainsUnknownAndMayBeRejectedByRule()
    {
        var r = Request("TEST-NOT-NORMATIVE");
        Assert.Null(r.Member.UnbracedLengthOutOfPlaneMetres);
        Assert.Null(r.Member.IsLaterallyRestrained);
        var result = Engine(x => x.Member.UnbracedLengthOutOfPlaneMetres is null
            ? DesignRuleEvaluation.MissingData("Out-of-plane effective length required")
            : Calculated()).Evaluate(r);
        Assert.Equal(DesignCheckStatus.MissingData, Assert.Single(result.Results).Status);
        Assert.False(result.IsComplete);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 8)]
    [InlineData(double.NaN, 8)]
    [InlineData(1, double.PositiveInfinity)]
    public void InvalidRuleNumbers_NeverReturnPassed(double demand, double resistance)
    {
        // First test row: zero resistance.
        var result = Engine(_ => Calculated(demand, resistance)).Evaluate(Request("TEST-NOT-NORMATIVE"));
        Assert.Equal(DesignCheckStatus.CalculationError, Assert.Single(result.Results).Status);
    }

    [Fact]
    public void InvalidTrace_AndRuleException_AreNotPasses()
    {
        var a = Engine(_ => DesignRuleEvaluation.Calculated(2, 3, "kN", ""))
            .Evaluate(Request("TEST-NOT-NORMATIVE"));
        Assert.Equal(DesignCheckStatus.CalculationError, Assert.Single(a.Results).Status);
        var b = Engine(_ => throw new InvalidOperationException("synthetic"))
            .Evaluate(Request("TEST-NOT-NORMATIVE"));
        Assert.Equal(DesignCheckStatus.CalculationError, Assert.Single(b.Results).Status);
    }

    [Fact]
    public void InvalidOrNonFiniteSnapshot_IsRejectedBeforeRuleExecution()
    {
        var r = Request("TEST-NOT-NORMATIVE") with
        {
            Effects = new DesignEffectSnapshot("FEM", "ULS", DesignEffectOrigin.FemAnalysis, 2,
                AxialForceKn: double.NaN)
        };
        var result = Engine(_ => throw new Exception("Should not be called")).Evaluate(r);
        Assert.Equal(DesignCheckStatus.InvalidInput, Assert.Single(result.Results).Status);
    }

    [Fact]
    public void InvalidStation_AndMissingProvenance_AreRejected()
    {
        var a = Request("TEST-NOT-NORMATIVE") with
        {
            Effects = new DesignEffectSnapshot("FEM", "ULS", DesignEffectOrigin.FemAnalysis, 7)
        };
        var b = Request("TEST-NOT-NORMATIVE") with
        {
            Effects = new DesignEffectSnapshot("", "ULS", DesignEffectOrigin.FemAnalysis, 2)
        };
        var engine = Engine(_ => Calculated());
        Assert.Equal(DesignCheckStatus.InvalidInput, Assert.Single(engine.Evaluate(a).Results).Status);
        Assert.Equal(DesignCheckStatus.InvalidInput, Assert.Single(engine.Evaluate(b).Results).Status);
    }

    [Fact]
    public void CoefficientMustBeFiniteAndTraceable()
    {
        var r = Request("TEST-NOT-NORMATIVE") with
        {
            Profile = new NormativeProfile("P", TestCode,
                new[] { new NormativeCoefficient("gamma", double.NaN, "TEST", "bad input") })
        };
        Assert.Equal(DesignCheckStatus.InvalidInput,
            Assert.Single(Engine(_ => Calculated()).Evaluate(r).Results).Status);
    }

    [Fact]
    public void DuplicateIdentifiers_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => new DesignCheckEngine(new[]
        {
            new FakeRule(_ => Calculated()), new FakeRule(_ => Calculated())
        }));
        var result = Engine(_ => Calculated()).Evaluate(Request("TEST-NOT-NORMATIVE", "test-not-normative"));
        Assert.Equal(DesignOverallStatus.Incomplete, result.OverallStatus);
        Assert.False(result.IsComplete);
        Assert.Contains(result.Results, x => x.Status == DesignCheckStatus.InvalidInput);
    }

    [Fact]
    public void PassedWithUnsupported_OverallIsIncomplete()
    {
        var result = Engine(_ => Calculated()).Evaluate(Request("TEST-NOT-NORMATIVE", "NOT-IMPLEMENTED"));
        Assert.Equal(DesignOverallStatus.Incomplete, result.OverallStatus);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public void PassingRuleDoesNotCreateTimberOrSteelNormativeCheck()
    {
        var engine = new DesignCheckEngine(Array.Empty<IDesignCheckRule>());
        foreach (var material in new[] { DesignMaterial.Steel, DesignMaterial.Timber })
        {
            var r = Request("EC3-FLEXURE") with
            {
                Member = new DesignMemberContext("AB", material, "S1", 4)
            };
            Assert.Equal(DesignCheckStatus.Unsupported, Assert.Single(engine.Evaluate(r).Results).Status);
        }
    }
}
