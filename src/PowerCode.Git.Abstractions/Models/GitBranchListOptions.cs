namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for listing branches in a git repository.
/// </summary>
public sealed class GitBranchListOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to list only remote-tracking branches
    /// (equivalent to <c>git branch -r</c>).
    /// </summary>
    public bool ListRemote { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to list both local and remote-tracking
    /// branches (equivalent to <c>git branch -a</c>).
    /// </summary>
    public bool ListAll { get; set; }

    /// <summary>
    /// Gets or sets a glob pattern to filter branch names (equivalent to
    /// <c>git branch -l &lt;pattern&gt;</c>).
    /// </summary>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter branches that contain the specified commit
    /// (equivalent to <c>git branch --contains &lt;commit&gt;</c>).
    /// </summary>
    public string? ContainsCommit { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter only branches whose tips are reachable from
    /// the specified commit (equivalent to <c>git branch --merged [&lt;commit&gt;]</c>).
    /// When set to an empty string or the value <c>"HEAD"</c>, HEAD is used.
    /// </summary>
    public string? MergedInto { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter only branches whose tips are NOT reachable from
    /// the specified commit (equivalent to <c>git branch --no-merged [&lt;commit&gt;]</c>).
    /// </summary>
    public string? NotMergedInto { get; set; }

    /// <summary>
    /// Gets or sets wildcard patterns used to include branches by name.
    /// Only branches matching at least one pattern are returned.
    /// When <see langword="null" /> or empty, all branches are included.
    /// </summary>
    public string[]? Include { get; set; }

    /// <summary>
    /// Gets or sets wildcard patterns used to exclude branches by name.
    /// Branches matching any pattern are removed from the result.
    /// Exclude is applied after <see cref="Include"/>.
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (ListRemote) parts.Add("remote");
        if (ListAll) parts.Add("all");
        if (Pattern is not null) parts.Add($"pattern={Pattern}");
        if (ContainsCommit is not null) parts.Add($"contains={ContainsCommit}");
        if (MergedInto is not null) parts.Add($"merged={MergedInto}");
        if (NotMergedInto is not null) parts.Add($"no-merged={NotMergedInto}");
        if (Include is { Length: > 0 }) parts.Add($"include={string.Join(",", Include)}");
        if (Exclude is { Length: > 0 }) parts.Add($"exclude={string.Join(",", Exclude)}");
        return parts.Count > 0
            ? $"GitBranchListOptions({string.Join(", ", parts)})"
            : "GitBranchListOptions()";
    }
}
