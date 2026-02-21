namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for creating a git tag.
/// </summary>
public sealed class GitTagCreateOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the tag to create.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the target committish (branch name, tag name, or SHA) to tag.
    /// When <see langword="null"/>, HEAD is used.
    /// </summary>
    public string? Target { get; init; }

    /// <summary>
    /// Gets the annotation message. When provided, an annotated tag is created using
    /// <c>git tag -a -m &lt;message&gt;</c>. When <see langword="null"/> or empty, a lightweight
    /// tag is created.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets a value indicating whether to overwrite an existing tag with the same name.
    /// Equivalent to <c>git tag -f</c>.
    /// </summary>
    public bool Force { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string> { $"Name={Name}" };
        if (Target is not null) parts.Add($"Target={Target}");
        if (Message is not null) parts.Add($"Message={Message}");
        if (Force) parts.Add("force");
        return $"GitTagCreateOptions({string.Join(", ", parts)})";
    }
}
