using System;

namespace PowerGit.Core.Models;

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
}