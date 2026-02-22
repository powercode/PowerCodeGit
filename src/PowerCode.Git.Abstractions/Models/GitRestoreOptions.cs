using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for restoring working-tree files or unstaging index changes (git restore).
/// </summary>
public sealed class GitRestoreOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the paths to restore. When <see langword="null"/> or empty,
    /// <see cref="All"/> must be set to <see langword="true"/>.
    /// </summary>
    public IReadOnlyList<string>? Paths { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to restore all files in the repository.
    /// </summary>
    public bool All { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to restore the index (staged changes)
    /// rather than the working tree. Equivalent to <c>git restore --staged</c>.
    /// When <see langword="false"/> (the default), the working-tree file is restored.
    /// </summary>
    public bool Staged { get; init; }

    /// <summary>
    /// Gets or sets the tree to restore content from. When <see langword="null"/>,
    /// defaults to HEAD (for working-tree restore) or the index (for <c>--staged</c> restore).
    /// Equivalent to <c>git restore --source=&lt;tree&gt;</c>.
    /// </summary>
    public string? Source { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return OptionsFormatter.Format(nameof(GitRestoreOptions),
            (nameof(RepositoryPath), RepositoryPath),
            (nameof(Paths), Paths),
            (nameof(All), All),
            (nameof(Staged), Staged),
            (nameof(Source), Source));
    }
}
