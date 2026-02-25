using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents the full working-tree and index status of a git repository.
/// </summary>
public sealed class GitStatusResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitStatusResult"/> class.
    /// </summary>
    /// <param name="repositoryPath">The repository root path.</param>
    /// <param name="currentBranch">The name of the current branch.</param>
    /// <param name="entries">The individual file status entries.</param>
    /// <param name="stagedCount">The number of staged changes.</param>
    /// <param name="modifiedCount">The number of unstaged modifications.</param>
    /// <param name="untrackedCount">The number of untracked files.</param>
    /// <param name="trackedBranchName">The name of the upstream tracking branch, or <see langword="null"/>.</param>
    /// <param name="aheadBy">The number of commits ahead of the tracked branch, or <see langword="null"/>.</param>
    /// <param name="behindBy">The number of commits behind the tracked branch, or <see langword="null"/>.</param>
    public GitStatusResult(
        string repositoryPath,
        string currentBranch,
        IReadOnlyList<GitStatusEntry> entries,
        int stagedCount,
        int modifiedCount,
        int untrackedCount,
        string? trackedBranchName = null,
        int? aheadBy = null,
        int? behindBy = null)
    {
        RepositoryPath = repositoryPath;
        CurrentBranch = currentBranch;
        Entries = entries;
        StagedCount = stagedCount;
        ModifiedCount = modifiedCount;
        UntrackedCount = untrackedCount;
        TrackedBranchName = trackedBranchName;
        AheadBy = aheadBy;
        BehindBy = behindBy;
    }

    /// <summary>
    /// Gets the repository root path.
    /// </summary>
    public string RepositoryPath { get; }

    /// <summary>
    /// Gets the name of the current branch.
    /// </summary>
    public string CurrentBranch { get; }

    /// <summary>
    /// Gets the individual file status entries.
    /// </summary>
    public IReadOnlyList<GitStatusEntry> Entries { get; }

    /// <summary>
    /// Gets the number of staged changes.
    /// </summary>
    public int StagedCount { get; }

    /// <summary>
    /// Gets the number of unstaged modifications (excludes untracked files).
    /// </summary>
    public int ModifiedCount { get; }

    /// <summary>
    /// Gets the number of untracked files.
    /// </summary>
    public int UntrackedCount { get; }

    /// <summary>
    /// Gets the name of the upstream tracking branch (e.g. <c>origin/main</c>),
    /// or <see langword="null"/> when the current branch does not track a remote branch.
    /// </summary>
    public string? TrackedBranchName { get; }

    /// <summary>
    /// Gets the number of commits the current branch is ahead of the tracked branch,
    /// or <see langword="null"/> when no tracking branch is configured.
    /// </summary>
    public int? AheadBy { get; }

    /// <summary>
    /// Gets the number of commits the current branch is behind the tracked branch,
    /// or <see langword="null"/> when no tracking branch is configured.
    /// </summary>
    public int? BehindBy { get; }

    /// <inheritdoc/>
    public override string ToString() =>
        TrackedBranchName is not null
            ? $"[{CurrentBranch}...{TrackedBranchName}] ahead: {AheadBy ?? 0}, behind: {BehindBy ?? 0}, staged: {StagedCount}, modified: {ModifiedCount}, untracked: {UntrackedCount}"
            : $"[{CurrentBranch}] staged: {StagedCount}, modified: {ModifiedCount}, untracked: {UntrackedCount}";
}
