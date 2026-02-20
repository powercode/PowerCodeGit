using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for staging files in a git repository.
/// </summary>
public sealed class GitStageOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the paths to stage. When null or empty, <see cref="All"/>
    /// must be set to <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<string>? Paths { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to stage all changes.
    /// </summary>
    public bool All { get; init; }
}
