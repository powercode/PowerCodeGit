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
}
