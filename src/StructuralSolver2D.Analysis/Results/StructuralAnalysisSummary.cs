namespace StructuralSolver2D.Analysis.Results;

/// <summary>
/// Provides compact maximum/minimum result values for one analyzed load case.
/// This type is intended for CLI output, reports and future UI result panels.
/// </summary>
public sealed class StructuralAnalysisSummary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralAnalysisSummary"/> class.
    /// </summary>
    public StructuralAnalysisSummary(
        string loadCaseId,
        AnalysisResultExtreme maxAbsUx,
        AnalysisResultExtreme maxAbsUy,
        AnalysisResultExtreme maxAbsRz,
        AnalysisResultExtreme maxAbsReactionFx,
        AnalysisResultExtreme maxAbsReactionFy,
        AnalysisResultExtreme maxAbsReactionMz,
        IReadOnlyList<MemberInternalForceExtrema> memberExtrema)
    {
        LoadCaseId = loadCaseId;
        MaxAbsUx = maxAbsUx;
        MaxAbsUy = maxAbsUy;
        MaxAbsRz = maxAbsRz;
        MaxAbsReactionFx = maxAbsReactionFx;
        MaxAbsReactionFy = maxAbsReactionFy;
        MaxAbsReactionMz = maxAbsReactionMz;
        MemberExtrema = memberExtrema;
        MaxAbsNormalForce = FindGlobalMax(memberExtrema, extrema => extrema.MaxAbsNormalForce);
        MaxAbsShearForce = FindGlobalMax(memberExtrema, extrema => extrema.MaxAbsShearForce);
        MaxAbsBendingMoment = FindGlobalMax(memberExtrema, extrema => extrema.MaxAbsBendingMoment);
    }

    /// <summary>
    /// Gets the analyzed load case identifier.
    /// </summary>
    public string LoadCaseId { get; }

    /// <summary>
    /// Gets the maximum absolute horizontal displacement Ux [m].
    /// </summary>
    public AnalysisResultExtreme MaxAbsUx { get; }

    /// <summary>
    /// Gets the maximum absolute vertical displacement Uy [m].
    /// </summary>
    public AnalysisResultExtreme MaxAbsUy { get; }

    /// <summary>
    /// Gets the maximum absolute rotation Rz [rad].
    /// </summary>
    public AnalysisResultExtreme MaxAbsRz { get; }

    /// <summary>
    /// Gets the maximum absolute horizontal reaction Fx [kN].
    /// </summary>
    public AnalysisResultExtreme MaxAbsReactionFx { get; }

    /// <summary>
    /// Gets the maximum absolute vertical reaction Fy [kN].
    /// </summary>
    public AnalysisResultExtreme MaxAbsReactionFy { get; }

    /// <summary>
    /// Gets the maximum absolute moment reaction Mz [kNm].
    /// </summary>
    public AnalysisResultExtreme MaxAbsReactionMz { get; }

    /// <summary>
    /// Gets internal-force extrema for each sampled member.
    /// </summary>
    public IReadOnlyList<MemberInternalForceExtrema> MemberExtrema { get; }

    /// <summary>
    /// Gets the global maximum absolute normal force N [kN].
    /// </summary>
    public InternalForceExtreme MaxAbsNormalForce { get; }

    /// <summary>
    /// Gets the global maximum absolute shear force V [kN].
    /// </summary>
    public InternalForceExtreme MaxAbsShearForce { get; }

    /// <summary>
    /// Gets the global maximum absolute bending moment M [kNm].
    /// </summary>
    public InternalForceExtreme MaxAbsBendingMoment { get; }

    private static InternalForceExtreme FindGlobalMax(
        IReadOnlyList<MemberInternalForceExtrema> memberExtrema,
        Func<MemberInternalForceExtrema, InternalForceExtreme> selector)
    {
        if (memberExtrema.Count == 0)
        {
            return new InternalForceExtreme(string.Empty, 0.0, 0.0, 0.0);
        }

        return memberExtrema
            .Select(selector)
            .MaxBy(extreme => Math.Abs(extreme.Value))!;
    }
}
