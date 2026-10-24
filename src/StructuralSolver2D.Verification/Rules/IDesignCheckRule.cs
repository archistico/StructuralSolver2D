using StructuralSolver2D.Verification.Contracts;

namespace StructuralSolver2D.Verification.Rules;

/// <summary>Normative formulas live behind this boundary in later milestones, NOT in M87-A.</summary>
public interface IDesignCheckRule
{
    string RuleId { get; }
    DesignCodeEdition Code { get; }
    DesignMaterial Material { get; }
    DesignLimitState LimitState { get; }
    string SourceClause { get; }
    DesignRuleEvaluation Evaluate(DesignCheckRequest request);
}

public enum DesignRuleEvaluationKind { Calculated, MissingData, NotApplicable }

/// <summary>
/// A rule must provide a nonnegative demand, strictly positive resistance, unit and audit trace
/// to yield Passed/Failed. All other paths must explicitly explain why they were not computed.
/// </summary>
public sealed class DesignRuleEvaluation
{
    private DesignRuleEvaluation(
        DesignRuleEvaluationKind kind, string message, double? demand, double? resistance,
        string? unit, string? calculationTrace)
    {
        Kind = kind;
        Message = message;
        Demand = demand;
        Resistance = resistance;
        Unit = unit;
        CalculationTrace = calculationTrace;
    }

    public DesignRuleEvaluationKind Kind { get; }
    public string Message { get; }
    public double? Demand { get; }
    public double? Resistance { get; }
    public string? Unit { get; }
    public string? CalculationTrace { get; }

    public static DesignRuleEvaluation Calculated(double demand, double resistance,
        string unit, string calculationTrace) =>
        new(DesignRuleEvaluationKind.Calculated, "", demand, resistance, unit, calculationTrace);

    public static DesignRuleEvaluation MissingData(string explanation) =>
        new(DesignRuleEvaluationKind.MissingData, explanation, null, null, null, null);

    public static DesignRuleEvaluation NotApplicable(string explanation) =>
        new(DesignRuleEvaluationKind.NotApplicable, explanation, null, null, null, null);
}
