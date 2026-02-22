using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for inspecting and modifying the working tree, index,
/// and HEAD of a git repository — including status, diff, staging, and reset.
/// </summary>
public interface IGitWorkingTreeService
{
    /// <summary>
    /// Gets the current status of the repository working tree and index.
    /// </summary>
    /// <param name="options">The status query options.</param>
    /// <returns>A status result containing file entries and summary counts.</returns>
    GitStatusResult GetStatus(GitStatusOptions options);

    /// <summary>
    /// Gets the diff entries for working tree or staged changes.
    /// </summary>
    /// <param name="options">The diff query options.</param>
    /// <returns>A list of diff entries describing file changes.</returns>
    IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options);

    /// <summary>
    /// Stages files for the next commit.
    /// </summary>
    /// <param name="options">The staging options specifying paths or all-files mode.</param>
    void Stage(GitStageOptions options);

    /// <summary>
    /// Unstages files, moving them back from the index to the working tree.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="paths">
    /// The paths to unstage. When null, all staged changes are unstaged.
    /// </param>
    void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null);

    /// <summary>
    /// Resets HEAD to the given revision with the specified mode, or resets specific paths.
    /// </summary>
    /// <param name="options">The reset options.</param>
    void Reset(GitResetOptions options);

    /// <summary>
    /// Resets HEAD to the given revision with the specified mode.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <param name="revision">
    /// The revision to reset to. When null, resets to HEAD (useful for
    /// unstaging with <see cref="GitResetMode.Mixed"/>).
    /// </param>
    /// <param name="mode">The reset mode (Mixed, Soft, or Hard).</param>
    void Reset(string repositoryPath, string? revision, GitResetMode mode)
        => Reset(new GitResetOptions { RepositoryPath = repositoryPath, Revision = revision, Mode = mode });

    /// <summary>
    /// Stages individual diff hunks into the index by applying their patch
    /// content via <c>git apply --cached</c>.
    /// </summary>
    /// <param name="options">The hunk staging options.</param>
    void StageHunks(GitStageHunkOptions options);

    /// <summary>
    /// Restores working-tree files or staged changes to their committed state
    /// (<c>git restore</c>).
    /// </summary>
    /// <param name="options">The restore options specifying paths, scope, and source.</param>
    void Restore(GitRestoreOptions options);

    /// <summary>
    /// Reverts individual diff hunks by applying an inverted patch
    /// (<c>git apply -R</c> or <c>git apply -R --cached</c>).
    /// </summary>
    /// <param name="options">The hunk restore options.</param>
    void RestoreHunks(GitRestoreHunkOptions options);
}
