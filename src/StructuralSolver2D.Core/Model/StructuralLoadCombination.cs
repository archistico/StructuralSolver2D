namespace StructuralSolver2D.Core.Model;

/// <summary>
/// Represents a user-defined manual load combination.
/// The combination does not imply any automatic normative generation.
/// </summary>
public sealed class StructuralLoadCombination
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralLoadCombination"/> class.
    /// </summary>
    /// <param name="id">Unique combination identifier.</param>
    /// <param name="name">User-facing combination name.</param>
    /// <param name="terms">Factored load cases that compose the combination.</param>
    /// <param name="description">Optional description.</param>
    public StructuralLoadCombination(
        string id,
        string name,
        IEnumerable<StructuralLoadCombinationTerm> terms,
        string? description = null)
    {
        Id = id;
        Name = name;
        Terms = terms?.ToList() ?? throw new ArgumentNullException(nameof(terms));
        Description = description;
    }

    /// <summary>
    /// Gets the unique combination identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the user-facing combination name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the factored load cases that compose this combination.
    /// </summary>
    public IList<StructuralLoadCombinationTerm> Terms { get; }

    /// <summary>
    /// Gets an optional description.
    /// </summary>
    public string? Description { get; }
}
