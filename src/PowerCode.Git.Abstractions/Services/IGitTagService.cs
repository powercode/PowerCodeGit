using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for listing and creating git tags.
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

    /// <summary>
    /// Creates a new tag according to the specified options. When <see cref="GitTagCreateOptions.Message"/>
    /// is provided it creates an annotated tag; otherwise a lightweight tag is created.
    /// </summary>
    /// <param name="options">The options controlling tag creation.</param>
    /// <returns>Information about the newly created tag.</returns>
    GitTagInfo CreateTag(GitTagCreateOptions options);

    /// <summary>
    /// Creates a lightweight tag at HEAD.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="name">The tag name.</param>
    /// <returns>Information about the newly created tag.</returns>
    GitTagInfo CreateTag(string repositoryPath, string name)
        => CreateTag(new GitTagCreateOptions { RepositoryPath = repositoryPath, Name = name });
}
