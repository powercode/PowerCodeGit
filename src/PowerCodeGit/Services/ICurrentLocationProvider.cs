namespace PowerCodeGit.Services;

/// <summary>
/// Provides the current file-system location for repository path resolution.
/// Cmdlets implement this via <c>SessionState</c>, while argument completers
/// use a PowerShell runspace–based implementation.
/// </summary>
public interface ICurrentLocationProvider
{
    /// <summary>
    /// Returns the absolute path of the current file-system working directory.
    /// </summary>
    string GetCurrentFileSystemLocation();
}
