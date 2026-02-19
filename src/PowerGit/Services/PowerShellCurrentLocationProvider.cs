using System.Management.Automation;

namespace PowerGit.Services;

/// <summary>
/// Resolves the current file-system location by invoking <c>Get-Location</c>
/// on the current PowerShell runspace. This implementation is intended for
/// argument completers, which do not have direct access to <c>SessionState</c>.
/// </summary>
internal sealed class PowerShellCurrentLocationProvider : ICurrentLocationProvider
{
    /// <inheritdoc />
    public string GetCurrentFileSystemLocation()
    {
        using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
        ps.AddCommand("Get-Location");
        var result = ps.Invoke<PathInfo>();

        return result.Count > 0 ? result[0].Path : ".";
    }
}
