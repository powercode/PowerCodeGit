using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for listing git tags.
/// </summary>
public interface IGitTagService
{
    /// <summary>
    /// Gets tags in the repository according to the specified options.
    /// </summary>
    /// <param name="options">The options controlling which tags are returned.</param>
    /// <returns>A list of tag information objects.</returns>
    IReadOnlyList<GitTagInfo> GetTags(GitTagListOptions options);

    /// <summary>
    /// Gets all tags in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of tag information objects.</returns>
    IReadOnlyList<GitTagInfo> GetTags(string repositoryPath)
        => GetTags(new GitTagListOptions { RepositoryPath = repositoryPath });
}
