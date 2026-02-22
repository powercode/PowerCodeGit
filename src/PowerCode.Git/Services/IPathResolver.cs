namespace PowerCode.Git.Services;

/// <summary>
/// Resolves PowerShell provider paths to absolute file-system paths.
/// <para>
/// The default runtime implementation wraps
/// <c>SessionState.Path.GetUnresolvedProviderPathFromPSPath</c>.
/// Unit tests that run outside the PowerShell engine can supply a
/// stub implementation to avoid the dependency on a live runspace.
/// </para>
/// </summary>
public interface IPathResolver
{
    /// <summary>
    /// Resolves a PowerShell provider path to an absolute file-system path.
    /// </summary>
    /// <param name="psPath">The path to resolve.</param>
    /// <returns>The resolved absolute path.</returns>
    string ResolvePath(string psPath);
}
