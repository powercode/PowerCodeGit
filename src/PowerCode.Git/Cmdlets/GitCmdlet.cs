using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Reflection;
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
    /// Identifies PowerShell control-flow exceptions that must propagate rather
    /// than be reported as ordinary cmdlet errors.
    /// </summary>
    /// <param name="exception">The exception being considered by a catch filter.</param>
    /// <returns>
    /// <c>true</c> for flow-control, pipeline-stopped, action-preference-stop, or
    /// halt-command exceptions, including runtime and reflection wrappers.
    /// </returns>
    /// <remarks>
    /// Compatibility helper for PowerShell/PowerShell#28137. Only PowerShell
    /// runtime and reflection wrappers are unwrapped; other exceptions retain
    /// their normal error handling.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="exception"/> is <c>null</c>.
    /// </exception>
    internal static bool IsPowerShellControlFlowException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        while (true)
        {
            if (exception is FlowControlException
                or PipelineStoppedException
                or ActionPreferenceStopException
                or HaltCommandException)
            {
                return true;
            }

            if (exception is TargetInvocationException or RuntimeException
                && exception.InnerException is not null)
            {
                exception = exception.InnerException;
                continue;
            }

            return false;
        }
    }

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
