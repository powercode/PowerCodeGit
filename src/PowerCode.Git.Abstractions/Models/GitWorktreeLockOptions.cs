namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for locking a worktree in a git repository.
/// </summary>
public sealed class GitWorktreeLockOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the worktree to lock.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the reason for locking the worktree, or <see langword="null"/>.
    /// </summary>
    public string? Reason { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var reason = Reason is not null ? $", Reason={Reason}" : string.Empty;
        return $"GitWorktreeLockOptions(Name={Name}{reason})";
    }
}
