namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for deleting a tag in a git repository.
/// </summary>
public sealed class GitTagDeleteOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the tag to delete.
    /// </summary>
    public required string Name { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"GitTagDeleteOptions(Name={Name})";
}
