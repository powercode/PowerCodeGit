namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for removing a worktree from a git repository.
/// </summary>
public sealed class GitWorktreeRemoveOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the worktree to remove.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force removal of a locked worktree.
    /// </summary>
    public bool Force { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var force = Force ? ", force" : string.Empty;
        return $"GitWorktreeRemoveOptions(Name={Name}{force})";
    }
}
