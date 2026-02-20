using System.Management.Automation;
using PowerCode.Git.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Base class for all PowerCode.Git cmdlets that operate on a git repository.
/// Provides a common <see cref="RepoPath"/> parameter that defaults to
/// the current PowerShell working directory.
/// </summary>
public abstract class GitCmdlet : PSCmdlet, ICurrentLocationProvider
{
    /// <summary>
    /// Gets or sets the path to the git repository. When omitted the current
    /// PowerShell file-system location is used.
    /// </summary>
    [Parameter]
    [Alias("RepositoryPath")]
    public string? RepoPath { get; set; }

    /// <summary>
    /// Resolves the repository path from <see cref="RepoPath"/> or the
    /// current PowerShell location.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// Optional override for the current directory, used by unit tests.
    /// </param>
    /// <returns>The resolved repository path.</returns>
    internal string ResolveRepositoryPath(string? currentFileSystemPath = null)
    {
        if (!string.IsNullOrWhiteSpace(RepoPath))
        {
            return RepoPath!;
        }

        return currentFileSystemPath ?? GetCurrentFileSystemLocation();
    }

    /// <inheritdoc />
    public string GetCurrentFileSystemLocation() => SessionState.Path.CurrentFileSystemLocation.Path;
}
