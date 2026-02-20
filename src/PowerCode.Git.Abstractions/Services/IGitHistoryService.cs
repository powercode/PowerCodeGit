using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for reading git history data.
/// </summary>
public interface IGitHistoryService
{
    /// <summary>
    /// Gets commit history using the supplied options.
    /// </summary>
    /// <param name="options">The history query options.</param>
    /// <returns>A filtered sequence of commits.</returns>
    IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options);
}
