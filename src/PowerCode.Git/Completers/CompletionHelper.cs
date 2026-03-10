using System.Collections;
using PowerCode.Git.Services;

namespace PowerCode.Git.Completers;

/// <summary>
/// Shared utilities for argument completers that need to resolve the
/// repository path from the command invocation context.
/// </summary>
internal static class CompletionHelper
{
    /// <summary>
    /// Resolves the repository path from the bound parameters or falls back
    /// to the current PowerShell working directory.
    /// </summary>
    /// <param name="fakeBoundParameters">
    /// The dictionary of parameters already bound on the command line.
    /// </param>
    /// <returns>The repository path to use for completion queries.</returns>
    public static string ResolveRepositoryPath(IDictionary fakeBoundParameters) =>
        ResolveRepositoryPath(fakeBoundParameters, new PowerShellCurrentLocationProvider());

    /// <summary>
    /// Resolves the repository path from the bound parameters or falls back
    /// to the supplied <paramref name="locationProvider"/>.
    /// </summary>
    /// <param name="fakeBoundParameters">
    /// The dictionary of parameters already bound on the command line.
    /// </param>
    /// <param name="locationProvider">
    /// Provider used to obtain the current file-system location when
    /// <c>RepoPath</c> is not present in the bound parameters.
    /// </param>
    /// <returns>The repository path to use for completion queries.</returns>
    internal static string ResolveRepositoryPath(IDictionary fakeBoundParameters, ICurrentLocationProvider locationProvider)
    {
        if (fakeBoundParameters is not null &&
            fakeBoundParameters.Contains("RepoPath") &&
            fakeBoundParameters["RepoPath"] is string path &&
            !string.IsNullOrWhiteSpace(path))
        {
            return RepositoryDiscovery.ResolveRoot(path);
        }

        return RepositoryDiscovery.ResolveRoot(locationProvider.GetCurrentFileSystemLocation());
    }
}
