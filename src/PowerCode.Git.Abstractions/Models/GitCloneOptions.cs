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

    /// <summary>
    /// Gets or sets the maximum number of commits to fetch (shallow clone).
    /// Null performs a full clone.
    /// </summary>
    public int? Depth { get; init; }

    /// <summary>
    /// Gets or sets the branch name to check out after cloning.
    /// Null checks out the remote's default branch.
    /// </summary>
    public string? BranchName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to create a bare repository (--bare).
    /// </summary>
    public bool Bare { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to recursively clone submodules (--recurse-submodules).
    /// </summary>
    public bool RecurseSubmodules { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return OptionsFormatter.Format(nameof(GitCloneOptions),
            (nameof(Url), Url),
            (nameof(LocalPath), LocalPath),
            (nameof(CredentialUsername), CredentialUsername),
            (nameof(CredentialPassword), CredentialPassword),
            (nameof(SingleBranch), SingleBranch),
            (nameof(Depth), Depth));
    }
}
