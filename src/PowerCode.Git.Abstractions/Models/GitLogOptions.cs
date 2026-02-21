using System;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents supported filters for a git log query.
/// </summary>
public sealed class GitLogOptions
{
    /// <summary>
    /// Gets or sets the repository path to query.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the branch name to query. Null uses repository HEAD.
    /// </summary>
    public string? BranchName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to include commits reachable
    /// from all local branches, not just HEAD or the specified branch.
    /// </summary>
    public bool AllBranches { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of commits to return.
    /// </summary>
    public int? MaxCount { get; set; }

    /// <summary>
    /// Gets or sets a substring filter for author name or email.
    /// </summary>
    public string? AuthorFilter { get; set; }

    /// <summary>
    /// Gets or sets the minimum commit date.
    /// </summary>
    public DateTimeOffset? Since { get; set; }

    /// <summary>
    /// Gets or sets the maximum commit date.
    /// </summary>
    public DateTimeOffset? Until { get; set; }

    /// <summary>
    /// Gets or sets a substring filter for commit messages.
    /// </summary>
    public string? MessagePattern { get; set; }

    /// <summary>
    /// Gets or sets one or more repository-relative file paths. When set,
    /// only commits that touch at least one of these paths are returned.
    /// </summary>
    public string[]? Paths { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to follow only the first parent
    /// commit when traversing the history (equivalent to <c>git log --first-parent</c>).
    /// </summary>
    public bool FirstParent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to exclude merge commits from the results
    /// (equivalent to <c>git log --no-merges</c>).
    /// </summary>
    public bool NoMerges { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (BranchName is not null) parts.Add($"branch={BranchName}");
        if (AllBranches) parts.Add("all");
        if (MaxCount.HasValue) parts.Add($"max={MaxCount}");
        if (AuthorFilter is not null) parts.Add($"author={AuthorFilter}");
        if (Since.HasValue) parts.Add($"since={Since:yyyy-MM-dd}");
        if (Until.HasValue) parts.Add($"until={Until:yyyy-MM-dd}");
        if (MessagePattern is not null) parts.Add($"grep={MessagePattern}");
        if (Paths is { Length: > 0 }) parts.Add($"paths=[{string.Join(", ", Paths)}]");
        if (FirstParent) parts.Add("first-parent");
        if (NoMerges) parts.Add("no-merges");

        return parts.Count > 0
            ? $"GitLogOptions({string.Join(", ", parts)})"
            : "GitLogOptions()";
    }
}
