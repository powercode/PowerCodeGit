using System.Collections.Generic;
using PowerCodeGit.Abstractions.Models;

namespace PowerCodeGit.Abstractions.Services;

/// <summary>
/// Defines operations for inspecting the working tree and index state of a git repository.
/// </summary>
public interface IGitWorkingTreeService
{
    /// <summary>
    /// Gets the current status of the repository working tree and index.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A status result containing file entries and summary counts.</returns>
    GitStatusResult GetStatus(string repositoryPath);

    /// <summary>
    /// Gets the diff entries for working tree or staged changes.
    /// </summary>
    /// <param name="options">The diff query options.</param>
    /// <returns>A list of diff entries describing file changes.</returns>
    IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options);
}
