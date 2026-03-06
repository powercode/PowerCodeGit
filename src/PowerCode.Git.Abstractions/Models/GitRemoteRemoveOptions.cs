namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for removing a remote from a git repository,
/// equivalent to <c>git remote remove &lt;name&gt;</c>.
/// </summary>
public sealed class GitRemoteRemoveOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the remote to remove.
    /// </summary>
    public required string Name { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"GitRemoteRemoveOptions(Name={Name})";
}
