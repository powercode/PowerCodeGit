using System;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for creating a git commit.
/// </summary>
public sealed class GitCommitOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the commit message. When null and <see cref="Amend"/>
    /// is <see langword="true"/>, the existing commit message is reused.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to amend the previous commit.
    /// </summary>
    public bool Amend { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow an empty commit
    /// (no staged changes).
    /// </summary>
    public bool AllowEmpty { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically stage all tracked
    /// modified files before committing (equivalent to <c>git commit -a</c>).
    /// </summary>
    public bool All { get; init; }

    /// <summary>
    /// Gets or sets a custom author in "Name &lt;email&gt;" format. When null,
    /// the configured git user identity is used.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Gets or sets a custom author/committer date. When null,
    /// the current time is used.
    /// </summary>
    public DateTimeOffset? Date { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return OptionsFormatter.Format(nameof(GitCommitOptions),
            (nameof(RepositoryPath), RepositoryPath),
            (nameof(Message), Message),
            (nameof(Amend), Amend),
            (nameof(AllowEmpty), AllowEmpty),
            (nameof(All), All));
    }
}
