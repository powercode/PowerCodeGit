namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for listing worktrees in a git repository.
/// </summary>
public class GitWorktreeListOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;
}
