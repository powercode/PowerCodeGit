using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for staging individual diff hunks into the index.
/// </summary>
public sealed class GitStageHunkOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the hunks to stage.
    /// </summary>
    public required IReadOnlyList<GitDiffHunk> Hunks { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return OptionsFormatter.Format(nameof(GitStageHunkOptions),
            (nameof(RepositoryPath), RepositoryPath),
            (nameof(Hunks), Hunks));
    }
}
