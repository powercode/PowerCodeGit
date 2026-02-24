using System.Collections.Generic;
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
    /// Gets or sets the path resolver used to translate PowerShell provider
    /// paths to absolute file-system paths.  At runtime this is initialised
    /// in <see cref="BeginProcessing"/> to a
    /// <see cref="SessionStatePathResolver"/>.  Unit tests that run outside
    /// the PowerShell engine can set a stub implementation instead.
    /// When <c>null</c>, <see cref="ResolvePSPath"/> returns the input path
    /// unchanged — a safe fallback for unit-test scenarios.
    /// </summary>
    internal IPathResolver? PathResolver { get; set; }

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
    /// Binds the <see cref="PathResolver"/> to the current
    /// <see cref="PSCmdlet.SessionState"/> so that subsequent calls to
    /// <see cref="ResolveRepositoryPath"/> can resolve PS provider paths.
    /// </summary>
    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        PathResolver ??= new SessionStatePathResolver(SessionState);
    }

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
            return ResolvePSPath(RepoPath);
        }

        if (!string.IsNullOrWhiteSpace(currentFileSystemPath))
        {
            return ResolvePSPath(currentFileSystemPath);
        }

        return GetCurrentFileSystemLocation();
    }

    /// <summary>
    /// Resolves a single PowerShell path using the configured
    /// <see cref="PathResolver"/>.  When no resolver is available (typical
    /// in unit tests that do not call <see cref="BeginProcessing"/>), the
    /// raw <paramref name="path"/> is returned unchanged.
    /// </summary>
    private string ResolvePSPath(string path) =>
        PathResolver?.ResolvePath(path) ?? path;

    /// <inheritdoc />
    public string GetCurrentFileSystemLocation() => SessionState.Path.CurrentFileSystemLocation.Path;
}
