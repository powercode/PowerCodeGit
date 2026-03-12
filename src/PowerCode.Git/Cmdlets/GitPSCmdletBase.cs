using System.Management.Automation;
using PowerCode.Git.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Minimal base class for all PowerCode.Git cmdlets.
/// Provides <see cref="PathResolver"/> initialisation so that PS provider
/// paths can be resolved to absolute file-system paths in a testable way.
/// </summary>
public abstract class GitPSCmdletBase : PSCmdlet
{
    /// <summary>
    /// Gets or sets the path resolver used to translate PowerShell provider
    /// paths to absolute file-system paths.  At runtime this is initialised
    /// in <see cref="BeginProcessing"/> to a <see cref="SessionStatePathResolver"/>.
    /// Unit tests that run outside the PowerShell engine can set a stub
    /// implementation instead.  When <c>null</c>, callers should fall back to
    /// returning the input path unchanged.
    /// </summary>
    internal IPathResolver? PathResolver { get; set; }

    /// <summary>
    /// Binds <see cref="PathResolver"/> to the current
    /// <see cref="PSCmdlet.SessionState"/> so that subsequent path resolution
    /// uses the live PowerShell provider.
    /// </summary>
    protected override void BeginProcessing()
    {
        base.BeginProcessing();
        PathResolver ??= new SessionStatePathResolver(SessionState);
    }
}
