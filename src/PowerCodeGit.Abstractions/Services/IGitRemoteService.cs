using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for listing git remotes.
/// </summary>
public interface IGitRemoteService
{
    /// <summary>
    /// Gets all configured remotes in the repository.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of remote information objects.</returns>
    IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath);
}
