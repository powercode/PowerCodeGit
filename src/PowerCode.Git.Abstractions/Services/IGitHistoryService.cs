using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for reading and creating git history — including
/// log queries and commit creation.
/// </summary>
public interface IGitHistoryService
{
    /// <summary>
    /// Gets commit history using the supplied options.
    /// </summary>
    /// <param name="options">The history query options.</param>
    /// <returns>A filtered sequence of commits.</returns>
    IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options);

    /// <summary>
    /// Creates a commit from the current index.
    /// </summary>
    /// <param name="options">The commit options including message and flags.</param>
    /// <returns>Information about the created commit.</returns>
    GitCommitInfo Commit(GitCommitOptions options);
}
