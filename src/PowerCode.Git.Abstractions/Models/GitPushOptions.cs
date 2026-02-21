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

    /// <summary>
    /// Gets or sets a value indicating whether to force-push the branch (git push --force).
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to force-push only if the remote tip matches
    /// the local expectation (git push --force-with-lease).
    /// </summary>
    public bool ForceWithLease { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete the branch on the remote (git push --delete).
    /// </summary>
    public bool Delete { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to push all tags (git push --tags).
    /// </summary>
    public bool Tags { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to push all branches (git push --all).
    /// </summary>
    public bool All { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to perform a dry run without actually pushing.
    /// </summary>
    public bool DryRun { get; init; }
}
