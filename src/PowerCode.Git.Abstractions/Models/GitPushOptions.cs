namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for pushing a branch to a remote.
/// </summary>
public sealed class GitPushOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the name of the remote to push to. Defaults to <c>"origin"</c>.
    /// </summary>
    public string RemoteName { get; init; } = "origin";

    /// <summary>
    /// Gets or sets the branch name to push. When null, the current HEAD branch is used.
    /// </summary>
    public string? BranchName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to set the upstream tracking reference.
    /// </summary>
    public bool SetUpstream { get; init; }

    /// <summary>
    /// Gets or sets the optional username for HTTP authentication.
    /// </summary>
    public string? CredentialUsername { get; init; }

    /// <summary>
    /// Gets or sets the optional password for HTTP authentication.
    /// </summary>
    public string? CredentialPassword { get; init; }
}
