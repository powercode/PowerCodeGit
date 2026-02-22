using System.Management.Automation;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Shared helpers for extracting information from <see cref="PSObject"/> instances
/// received via pipeline input.
/// </summary>
internal static class GitPSObjectHelper
{
    /// <summary>
    /// Attempts to extract a file path from a <see cref="PSObject"/> by inspecting
    /// its properties in priority order: <c>FilePath</c>, <c>NewPath</c>, <c>Path</c>.
    /// </summary>
    /// <param name="obj">The object to inspect.</param>
    /// <returns>
    /// The resolved path string, or <see langword="null"/> when no compatible
    /// property is found.
    /// </returns>
    public static string? ResolveInputObjectPath(PSObject obj)
    {
        foreach (var propertyName in (string[])["FilePath", "NewPath", "Path"])
        {
            var property = obj.Properties[propertyName];

            if (property?.Value is string value && !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
