namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for fetching remote changes without merging them into a local branch.
/// </summary>
public sealed class GitFetchOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the remote to fetch from. Defaults to <c>"origin"</c>.
    /// </summary>
    public string RemoteName { get; init; } = "origin";

    /// <summary>
    /// Gets a value indicating whether to prune remote-tracking branches that no
    /// longer exist on the remote.
    /// </summary>
    public bool Prune { get; init; }

    /// <summary>
    /// Gets a value indicating whether to fetch tags from the remote.
    /// <see langword="true"/> fetches all tags; <see langword="false"/> suppresses
    /// automatic tag fetching; <see langword="null"/> uses the remote's default.
    /// </summary>
    public bool? Tags { get; init; }

    /// <summary>
    /// Gets the optional username for HTTP authentication.
    /// </summary>
    public string? CredentialUsername { get; init; }

    /// <summary>
    /// Gets the optional password for HTTP authentication.
    /// </summary>
    public string? CredentialPassword { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return OptionsFormatter.Format(nameof(GitFetchOptions),
            (nameof(RepositoryPath), RepositoryPath),
            (nameof(RemoteName), RemoteName),
            (nameof(Prune), Prune));
    }
}
