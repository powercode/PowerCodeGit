using System.Collections;
using System.Management.Automation;

namespace PowerGit.Completers;

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
    public static string ResolveRepositoryPath(IDictionary fakeBoundParameters)
    {
        if (fakeBoundParameters is not null &&
            fakeBoundParameters.Contains("RepoPath") &&
            fakeBoundParameters["RepoPath"] is string path &&
            !string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        // Fall back to the current PowerShell location.
        using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);
        ps.AddCommand("Get-Location");
        var result = ps.Invoke<PathInfo>();

        return result.Count > 0 ? result[0].Path : ".";
    }
}
