using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Provides access to files changed by a specific commit, comparing
/// the commit's tree against its parent.
/// </summary>
public interface IGitCommitFileService
{
    /// <summary>
    /// Returns the files changed by the commit specified in <paramref name="options"/>.
    /// Each entry represents a file that was added, modified, deleted, or renamed
    /// relative to the commit's first parent.
    /// </summary>
    /// <param name="options">Options controlling the query.</param>
    /// <returns>A read-only list of diff entries for the commit.</returns>
    IReadOnlyList<GitDiffEntry> GetCommitFiles(GitCommitFileOptions options);
}
