namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for listing tags in a git repository.
/// </summary>
public sealed class GitTagListOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a glob pattern to filter tag names
    /// (equivalent to <c>git tag -l &lt;pattern&gt;</c>).
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the sort field. Supported values: <c>"name"</c>, <c>"version"</c> (or <c>"v:refname"</c>).
    /// Null sorts by creation order.
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter only tags that contain the specified commit
    /// (equivalent to <c>git tag --contains &lt;commit&gt;</c>).
    /// </summary>
    public string? ContainsCommit { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (Pattern is not null) parts.Add($"pattern={Pattern}");
        if (SortBy is not null) parts.Add($"sortBy={SortBy}");
        if (ContainsCommit is not null) parts.Add($"contains={ContainsCommit}");
        return parts.Count > 0
            ? $"GitTagListOptions({string.Join(", ", parts)})"
            : "GitTagListOptions()";
    }
}
