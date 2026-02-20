using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for listing tracked file paths in a git repository.
/// </summary>
public interface IGitPathService
{
    /// <summary>
    /// Gets all tracked file paths in the repository's current HEAD tree.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of repository-relative file paths.</returns>
    IReadOnlyList<string> GetTrackedPaths(string repositoryPath);
}
