namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for deleting a branch in a git repository.
/// </summary>
public sealed class GitBranchDeleteOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the branch to delete.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force-delete the branch even if it is not
    /// fully merged into its upstream or HEAD (equivalent to <c>git branch -D</c>).
    /// </summary>
    public bool Force { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        Force
            ? $"GitBranchDeleteOptions(Name={Name}, force)"
            : $"GitBranchDeleteOptions(Name={Name})";
}
