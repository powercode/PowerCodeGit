using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Compares two tree-ish references and returns the file-level differences
/// between them.
/// </summary>
public interface IGitTreeComparisonService
{
    /// <summary>
    /// Compares the trees of two commits (or other tree-ish references) and
    /// returns a diff entry for each changed file.
    /// </summary>
    /// <param name="options">Options controlling the comparison.</param>
    /// <returns>A read-only list of diff entries representing the changes.</returns>
    IReadOnlyList<GitDiffEntry> Compare(GitTreeCompareOptions options);
}
