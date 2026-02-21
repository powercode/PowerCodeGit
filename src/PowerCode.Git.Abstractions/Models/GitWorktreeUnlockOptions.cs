namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for unlocking a worktree in a git repository.
/// </summary>
public sealed class GitWorktreeUnlockOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the worktree to unlock.
    /// </summary>
    public required string Name { get; init; }

    /// <inheritdoc/>
    public override string ToString() => $"GitWorktreeUnlockOptions(Name={Name})";
}
