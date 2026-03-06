using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for reverting individual diff hunks by applying an inverted patch
/// (<c>git apply -R</c>).
/// </summary>
public sealed class GitRestoreHunkOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the hunks to revert.
    /// </summary>
    public required IReadOnlyList<GitDiffHunk> Hunks { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to revert staged (index) changes
    /// rather than working-tree changes. When <see langword="true"/>, the inverted
    /// patch is applied to the index only (<c>git apply -R --cached</c>).
    /// </summary>
    public bool Staged { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return OptionsFormatter.Format(nameof(GitRestoreHunkOptions),
            (nameof(RepositoryPath), RepositoryPath),
            (nameof(Hunks), Hunks),
            (nameof(Staged), Staged));
    }
}
