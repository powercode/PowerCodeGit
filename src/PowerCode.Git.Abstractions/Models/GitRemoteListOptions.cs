namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for listing configured remotes in a git repository.
/// </summary>
public sealed class GitRemoteListOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of a specific remote to retrieve.
    /// When <see langword="null"/>, all remotes are returned.
    /// </summary>
    public string? Name { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        Name is not null
            ? $"GitRemoteListOptions(Name={Name})"
            : "GitRemoteListOptions()";
}
