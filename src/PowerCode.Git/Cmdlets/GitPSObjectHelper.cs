using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;

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

    /// <summary>
    /// Attempts to resolve an upstream ref from a <see cref="PSObject"/> by inspecting
    /// its <see cref="PSObject.BaseObject"/> type (for strongly-typed objects like
    /// <see cref="GitBranchInfo"/> or <see cref="GitCommitInfo"/>) or duck-typing
    /// well-known property names (for <see cref="PSCustomObject"/>).
    /// </summary>
    /// <param name="obj">The object to inspect.</param>
    /// <returns>
    /// The resolved upstream ref string, or <see langword="null"/> when the object type
    /// is unsupported or no compatible property is found.
    /// </returns>
    public static string? ResolveInputObjectUpstream(PSObject obj)
    {
        var baseObject = obj.BaseObject;

        // Check for strongly-typed .NET objects unwrapped from the PSObject.
        if (baseObject is not PSCustomObject)
        {
            return baseObject switch
            {
                GitBranchInfo branch => branch.Name,
                GitCommitInfo commit => commit.Sha,
                string str => str,
                _ => null
            };
        }

        // Duck-type for PSCustomObject: try well-known property names in priority order.
        foreach (var propertyName in (string[])["Upstream", "BranchName", "Name", "Sha"])
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
