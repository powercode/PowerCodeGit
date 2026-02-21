namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for adding a new worktree to a git repository.
/// </summary>
public sealed class GitWorktreeAddOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name for the new worktree.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the filesystem path where the worktree will be created.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the committish or branch to check out in the new worktree.
    /// When <see langword="null"/>, the current HEAD is used.
    /// </summary>
    public string? Branch { get; init; }

    /// <summary>
    /// Gets a value indicating whether the worktree should be created in a locked state.
    /// </summary>
    public bool Locked { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Name={Name}",
            $"Path={Path}",
        };
        if (Branch is not null) parts.Add($"Branch={Branch}");
        if (Locked) parts.Add("locked");
        return $"GitWorktreeAddOptions({string.Join(", ", parts)})";
    }
}
