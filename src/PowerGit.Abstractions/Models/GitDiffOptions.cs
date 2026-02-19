namespace PowerGit.Abstractions.Models;

/// <summary>
/// Represents options for a git diff query.
/// </summary>
public sealed class GitDiffOptions
{
    /// <summary>
    /// Gets or sets the repository path to query.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to show staged (index) changes
    /// instead of unstaged (working directory) changes.
    /// </summary>
    public bool Staged { get; set; }
}
