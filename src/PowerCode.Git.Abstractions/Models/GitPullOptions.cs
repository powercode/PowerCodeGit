namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for pulling remote changes into the current branch.
/// </summary>
public sealed class GitPullOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the name of the remote to pull from. Defaults to <c>"origin"</c>.
    /// </summary>
    public string RemoteName { get; init; } = "origin";

    /// <summary>
    /// Gets or sets the merge strategy to use. Defaults to <see cref="GitMergeStrategy.Merge"/>.
    /// </summary>
    public GitMergeStrategy MergeStrategy { get; init; } = GitMergeStrategy.Merge;

    /// <summary>
    /// Gets or sets a value indicating whether to prune remote-tracking
    /// branches that no longer exist on the remote.
    /// </summary>
    public bool Prune { get; init; }

    /// <summary>
    /// Gets or sets the optional username for HTTP authentication.
    /// </summary>
    public string? CredentialUsername { get; init; }

    /// <summary>
    /// Gets or sets the optional password for HTTP authentication.
    /// </summary>
    public string? CredentialPassword { get; init; }
}
