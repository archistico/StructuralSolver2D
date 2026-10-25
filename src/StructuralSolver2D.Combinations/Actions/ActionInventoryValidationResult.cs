namespace StructuralSolver2D.Combinations.Actions;

/// <summary>Invalid configuration, unsupported normative category or missing evidence.</summary>
public enum ActionInventoryIssueLevel { Invalid, Unsupported, Incomplete }

public enum ActionInventoryStatus { Valid, Incomplete, Unsupported, Invalid }

public sealed record ActionInventoryIssue(
    string Code,
    ActionInventoryIssueLevel Level,
    string? ActionId,
    string Message);

/// <summary>
/// Valid means only that the supplied inventory passes the P0-B2 input checks.
/// It does NOT mean SLU/SLE combinations are complete or a member is verified.
/// </summary>
public sealed class ActionInventoryValidationResult
{
    public ActionInventoryValidationResult(IEnumerable<ActionInventoryIssue> issues)
    {
        Issues = Array.AsReadOnly(issues.ToArray());
        Status = Issues.Any(i => i.Level == ActionInventoryIssueLevel.Invalid)
            ? ActionInventoryStatus.Invalid
            : Issues.Any(i => i.Level == ActionInventoryIssueLevel.Unsupported)
                ? ActionInventoryStatus.Unsupported
                : Issues.Any(i => i.Level == ActionInventoryIssueLevel.Incomplete)
                    ? ActionInventoryStatus.Incomplete
                    : ActionInventoryStatus.Valid;
    }

    public IReadOnlyList<ActionInventoryIssue> Issues { get; }
    public ActionInventoryStatus Status { get; }
    public bool IsValid => Status == ActionInventoryStatus.Valid;
}
