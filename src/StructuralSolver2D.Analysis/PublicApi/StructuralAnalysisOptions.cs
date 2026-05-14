using StructuralSolver2D.Analysis.Serviceability;

namespace StructuralSolver2D.Analysis.PublicApi;

/// <summary>
/// Controls the public high-level analysis workflow.
/// </summary>
public sealed class StructuralAnalysisOptions
{
    /// <summary>
    /// Gets or sets the number of samples used for each internal-force diagram.
    /// Must be at least 2.
    /// </summary>
    public int InternalForceSampleCount { get; set; } = 21;

    /// <summary>
    /// Gets or sets the number of samples used for each displacement diagram.
    /// Must be at least 2.
    /// </summary>
    public int DisplacementSampleCount { get; set; } = 21;

    /// <summary>
    /// Gets or sets a value indicating whether displacement diagrams should be sampled.
    /// </summary>
    public bool IncludeDisplacementDiagrams { get; set; } = true;

    /// <summary>
    /// Gets or sets a preliminary deflection limit. When set, displacement diagrams are sampled and checked against this limit.
    /// </summary>
    public DeflectionLimit? DeflectionLimit { get; set; }

    /// <summary>
    /// Validates option values and throws a clear exception if they are not usable.
    /// </summary>
    public void Validate()
    {
        if (InternalForceSampleCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(InternalForceSampleCount),
                InternalForceSampleCount,
                "At least two internal-force samples are required.");
        }

        if (DisplacementSampleCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DisplacementSampleCount),
                DisplacementSampleCount,
                "At least two displacement samples are required.");
        }
    }
}
