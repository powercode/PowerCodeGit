using System.Collections.Generic;
using PowerGit.Abstractions.Models;

namespace PowerGit.Abstractions.Services;

/// <summary>
/// Defines operations for listing git tags.
/// </summary>
public interface IGitTagService
{
    /// <summary>
    /// Gets all tags in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of tag information objects.</returns>
    IReadOnlyList<GitTagInfo> GetTags(string repositoryPath);
}
