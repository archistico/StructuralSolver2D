namespace StructuralSolver2D.Core.Model.Sections;

using StructuralSolver2D.Core.Model;

/// <summary>
/// Represents a reusable in-memory catalog of structural sections.
/// The catalog is independent from a specific structural model and can be persisted to JSON.
/// </summary>
public sealed class StructuralSectionCatalog
{
    private readonly List<StructuralSection> sections = new();

    /// <summary>
    /// Initializes a new empty section catalog.
    /// </summary>
    public StructuralSectionCatalog()
    {
    }

    /// <summary>
    /// Initializes a new section catalog from an existing sequence of sections.
    /// </summary>
    /// <param name="sections">Sections to add to the catalog.</param>
    public StructuralSectionCatalog(IEnumerable<StructuralSection> sections)
    {
        ArgumentNullException.ThrowIfNull(sections);

        foreach (StructuralSection section in sections)
        {
            Add(section);
        }
    }

    /// <summary>
    /// Gets the catalog sections in insertion order.
    /// </summary>
    public IReadOnlyList<StructuralSection> Sections => sections;

    /// <summary>
    /// Gets the number of sections contained in the catalog.
    /// </summary>
    public int Count => sections.Count;

    /// <summary>
    /// Creates a catalog from a sequence of sections.
    /// </summary>
    public static StructuralSectionCatalog FromSections(IEnumerable<StructuralSection> sections) =>
        new(sections);

    /// <summary>
    /// Adds a section to the catalog.
    /// Section identifiers are matched case-insensitively.
    /// </summary>
    /// <param name="section">The section to add.</param>
    /// <returns>The current catalog for fluent construction.</returns>
    public StructuralSectionCatalog Add(StructuralSection section)
    {
        ValidateSection(section);

        if (Contains(section.Id))
        {
            throw new InvalidOperationException($"A section with id '{section.Id}' already exists in the catalog.");
        }

        sections.Add(section);
        return this;
    }

    /// <summary>
    /// Adds a section to the catalog or replaces an existing section with the same identifier.
    /// Section identifiers are matched case-insensitively.
    /// </summary>
    /// <param name="section">The section to add or replace.</param>
    /// <returns>The current catalog for fluent construction.</returns>
    public StructuralSectionCatalog AddOrReplace(StructuralSection section)
    {
        ValidateSection(section);

        int index = sections.FindIndex(item => string.Equals(item.Id, section.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            sections[index] = section;
            return this;
        }

        sections.Add(section);
        return this;
    }

    /// <summary>
    /// Returns true when the catalog contains a section with the supplied identifier.
    /// </summary>
    public bool Contains(string sectionId) =>
        sections.Any(item => string.Equals(item.Id, sectionId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Attempts to find a section by identifier.
    /// Section identifiers are matched case-insensitively.
    /// </summary>
    public bool TryFind(string sectionId, out StructuralSection? section)
    {
        section = sections.FirstOrDefault(item => string.Equals(item.Id, sectionId, StringComparison.OrdinalIgnoreCase));
        return section is not null;
    }

    /// <summary>
    /// Finds a section by identifier or throws a clear exception when it is missing.
    /// </summary>
    public StructuralSection Find(string sectionId)
    {
        if (TryFind(sectionId, out StructuralSection? section) && section is not null)
        {
            return section;
        }

        throw new KeyNotFoundException($"Section '{sectionId}' was not found in the catalog.");
    }

    /// <summary>
    /// Adds catalog sections to a structural model.
    /// </summary>
    /// <param name="model">Target model.</param>
    /// <param name="replaceExisting">When true, model sections with matching identifiers are replaced. Otherwise duplicates are skipped.</param>
    /// <returns>The number of sections added or replaced in the model.</returns>
    public int ApplyToModel(StructuralModel model, bool replaceExisting = false)
    {
        ArgumentNullException.ThrowIfNull(model);

        int changed = 0;
        foreach (StructuralSection section in sections)
        {
            int existingIndex = model.Sections.ToList().FindIndex(item => string.Equals(item.Id, section.Id, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                if (!replaceExisting)
                {
                    continue;
                }

                model.Sections[existingIndex] = section;
                changed++;
                continue;
            }

            model.AddSection(section);
            changed++;
        }

        return changed;
    }

    private static void ValidateSection(StructuralSection section)
    {
        ArgumentNullException.ThrowIfNull(section);

        if (string.IsNullOrWhiteSpace(section.Id))
        {
            throw new ArgumentException("The section identifier must not be empty.", nameof(section));
        }

        if (string.IsNullOrWhiteSpace(section.Name))
        {
            throw new ArgumentException("The section name must not be empty.", nameof(section));
        }

        ValidatePositiveFinite(section.Area, nameof(section.Area));
        ValidatePositiveFinite(section.MomentOfInertia, nameof(section.MomentOfInertia));

        if (section.Height is not null)
        {
            ValidatePositiveFinite(section.Height.Value, nameof(section.Height));
        }

        if (section.Width is not null)
        {
            ValidatePositiveFinite(section.Width.Value, nameof(section.Width));
        }
    }

    private static void ValidatePositiveFinite(double value, string parameterName)
    {
        if (value <= 0.0 || !double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "The section property must be positive and finite.");
        }
    }
}
