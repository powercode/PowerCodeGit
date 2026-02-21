using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for resetting HEAD in a git repository.
/// </summary>
public sealed class GitResetOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the revision to reset to. When <see langword="null"/>, resets to HEAD.
    /// </summary>
    public string? Revision { get; init; }

    /// <summary>
    /// Gets the reset mode (Mixed, Soft, or Hard).
    /// </summary>
    public GitResetMode Mode { get; init; } = GitResetMode.Mixed;

    /// <summary>
    /// Gets the specific file paths to reset. When specified, performs a path-based
    /// reset (unstage), ignoring <see cref="Mode"/>.
    /// </summary>
    public IReadOnlyList<string>? Paths { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (Paths is { Count: > 0 })
        {
            return $"GitResetOptions(paths={string.Join(", ", Paths)})";
        }

        return $"GitResetOptions(revision={Revision ?? "HEAD"}, mode={Mode})";
    }
}
