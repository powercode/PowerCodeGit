using System.Collections.Generic;
using System.Management.Automation;
using PowerCode.Git.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Base class for all PowerCode.Git cmdlets that operate on a git repository.
/// Provides a common <see cref="RepoPath"/> parameter that defaults to
/// the current PowerShell working directory.
/// </summary>
public abstract class GitCmdlet : GitPSCmdletBase, ICurrentLocationProvider
{
    /// <summary>
    /// Gets or sets the path to the git repository. When omitted the current
    /// PowerShell file-system location is used.
    /// </summary>
    [Parameter]
    [Alias("RepositoryPath")]
    public string? RepoPath { get; set; }

    /// <summary>
    /// Gets or sets an optional set of parameter names that are treated as
    /// explicitly bound.  Unit tests set this to simulate
    /// <see cref="InvocationInfo.BoundParameters"/> without running inside
    /// the PowerShell engine.
    /// </summary>
    internal ISet<string>? BoundParameterOverrides { get; set; }

    /// <summary>
    /// Returns <c>true</c> when the user explicitly specified the named
    /// parameter on the command line.  At runtime this delegates to
    /// <see cref="InvocationInfo.BoundParameters"/>; in unit tests it
    /// checks <see cref="BoundParameterOverrides"/>.
    /// </summary>
    /// <param name="parameterName">
    /// The name of the parameter to check, typically passed via
    /// <c>nameof(...)</c>.
    /// </param>
    /// <returns><c>true</c> if the parameter was explicitly bound.</returns>
    internal bool IsParameterBound(string parameterName) =>
        BoundParameterOverrides?.Contains(parameterName)
        ?? MyInvocation?.BoundParameters?.ContainsKey(parameterName)
        ?? false;

    /// <summary>
    /// Resolves the repository path from <see cref="RepoPath"/> or the
    /// current PowerShell location. If the resolved path is a subdirectory
    /// of a git working tree, it is resolved up to the repository root.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// Optional override for the current directory, used by unit tests.
    /// </param>
    /// <returns>The resolved repository root path.</returns>
    internal string ResolveRepositoryPath(string? currentFileSystemPath = null)
    {
        string raw;

        if (!string.IsNullOrWhiteSpace(RepoPath))
        {
            raw = ResolvePSPath(RepoPath);
        }
        else if (!string.IsNullOrWhiteSpace(currentFileSystemPath))
        {
            raw = ResolvePSPath(currentFileSystemPath);
        }
        else
        {
            raw = GetCurrentFileSystemLocation();
        }

        return RepositoryDiscovery.ResolveRoot(raw);
    }

    /// <summary>
    /// Resolves a single PowerShell path using the configured
    /// <see cref="GitPSCmdletBase.PathResolver"/>.  When no resolver is available (typical
    /// in unit tests that do not call <c>BeginProcessing</c>), the
    /// raw <paramref name="path"/> is returned unchanged.
    /// </summary>
    private string ResolvePSPath(string path) =>
        PathResolver?.ResolvePath(path) ?? path;

    /// <inheritdoc />
    public string GetCurrentFileSystemLocation() => SessionState.Path.CurrentFileSystemLocation.Path;
}
