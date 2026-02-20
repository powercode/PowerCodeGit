namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for cloning a git repository.
/// </summary>
public sealed class GitCloneOptions
{
    /// <summary>
    /// Gets or sets the remote URL to clone from.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Gets or sets the local directory to clone into.
    /// When null, the directory is derived from the URL.
    /// </summary>
    public string? LocalPath { get; init; }

    /// <summary>
    /// Gets or sets the optional username for HTTP authentication.
    /// </summary>
    public string? CredentialUsername { get; init; }

    /// <summary>
    /// Gets or sets the optional password for HTTP authentication.
    /// </summary>
    public string? CredentialPassword { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to clone only the default
    /// branch (equivalent to <c>--single-branch</c>).
    /// </summary>
    public bool SingleBranch { get; init; }
}
