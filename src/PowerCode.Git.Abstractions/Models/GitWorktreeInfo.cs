namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents information about a git worktree.
/// </summary>
public sealed class GitWorktreeInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitWorktreeInfo"/> class.
    /// </summary>
    /// <param name="name">The worktree name.</param>
    /// <param name="path">The filesystem path of the worktree.</param>
    /// <param name="isLocked">Whether the worktree is locked.</param>
    /// <param name="lockReason">The reason the worktree is locked, or <see langword="null"/>.</param>
    public GitWorktreeInfo(string name, string path, bool isLocked, string? lockReason)
    {
        Name = name;
        Path = path;
        IsLocked = isLocked;
        LockReason = lockReason;
    }

    /// <summary>
    /// Gets the worktree name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the filesystem path of the worktree.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets a value indicating whether the worktree is locked.
    /// </summary>
    public bool IsLocked { get; }

    /// <summary>
    /// Gets the reason the worktree is locked, or <see langword="null"/> if unlocked.
    /// </summary>
    public string? LockReason { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var locked = IsLocked ? " [locked]" : string.Empty;
        return $"{Name} {Path}{locked}";
    }
}
