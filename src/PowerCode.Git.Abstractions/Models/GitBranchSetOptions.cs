namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for configuring an existing local branch, equivalent to
/// <c>git branch --set-upstream-to</c>, <c>git branch --edit-description</c>,
/// and related configuration commands.
/// </summary>
public sealed class GitBranchSetOptions
{
    /// <summary>
    /// Path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// The local branch name to configure.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The remote name to set as <c>branch.&lt;name&gt;.remote</c>.
    /// When specified together with <see cref="Upstream"/>, configures the
    /// full upstream tracking reference for the branch.
    /// </summary>
    public string? Remote { get; init; }

    /// <summary>
    /// The upstream merge ref to set as <c>branch.&lt;name&gt;.merge</c>.
    /// Typically a short branch name such as <c>main</c>, which is stored
    /// as <c>refs/heads/main</c>.
    /// </summary>
    public string? Upstream { get; init; }

    /// <summary>
    /// The branch description to set as <c>branch.&lt;name&gt;.description</c>.
    /// </summary>
    public string? Description { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string> { $"Name={Name}" };
        if (Remote is not null) parts.Add($"Remote={Remote}");
        if (Upstream is not null) parts.Add($"Upstream={Upstream}");
        if (Description is not null) parts.Add($"Description={Description}");
        return $"GitBranchSetOptions({string.Join(", ", parts)})";
    }
}
