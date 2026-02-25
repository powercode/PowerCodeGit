namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Lightweight snapshot of git repository state used to build a shell prompt segment.
/// Returned by <see cref="Services.IGitWorkingTreeService.GetPromptInfo"/>.
/// </summary>
/// <remarks>
/// All data is gathered in a single repository open to minimise I/O overhead for
/// prompt generation, which runs on every interactive command.
/// </remarks>
public sealed class GitWorkingTreePromptInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitWorkingTreePromptInfo"/> class.
    /// </summary>
    /// <param name="branchName">The current branch name, or a short SHA when detached.</param>
    /// <param name="isDetachedHead"><see langword="true"/> when HEAD is detached.</param>
    /// <param name="trackedBranchName">Upstream tracking branch name, or <see langword="null"/>.</param>
    /// <param name="aheadBy">Commits ahead of upstream, or <see langword="null"/>.</param>
    /// <param name="behindBy">Commits behind upstream, or <see langword="null"/>.</param>
    /// <param name="stagedCount">Number of staged changes.</param>
    /// <param name="modifiedCount">Number of unstaged modifications.</param>
    /// <param name="untrackedCount">Number of untracked files.</param>
    /// <param name="stashCount">Number of stash entries.</param>
    public GitWorkingTreePromptInfo(
        string branchName,
        bool isDetachedHead,
        string? trackedBranchName,
        int? aheadBy,
        int? behindBy,
        int stagedCount,
        int modifiedCount,
        int untrackedCount,
        int stashCount)
    {
        BranchName = branchName;
        IsDetachedHead = isDetachedHead;
        TrackedBranchName = trackedBranchName;
        AheadBy = aheadBy;
        BehindBy = behindBy;
        StagedCount = stagedCount;
        ModifiedCount = modifiedCount;
        UntrackedCount = untrackedCount;
        StashCount = stashCount;
    }

    /// <summary>
    /// Gets the current branch name, or a 7-character abbreviated SHA when in detached HEAD state.
    /// </summary>
    public string BranchName { get; }

    /// <summary>
    /// Gets a value indicating whether the repository is in detached HEAD state.
    /// </summary>
    public bool IsDetachedHead { get; }

    /// <summary>
    /// Gets the upstream tracking branch name (e.g. <c>origin/main</c>),
    /// or <see langword="null"/> when no upstream is configured.
    /// </summary>
    public string? TrackedBranchName { get; }

    /// <summary>
    /// Gets the number of commits the local branch is ahead of its upstream,
    /// or <see langword="null"/> when no upstream is configured.
    /// </summary>
    public int? AheadBy { get; }

    /// <summary>
    /// Gets the number of commits the local branch is behind its upstream,
    /// or <see langword="null"/> when no upstream is configured.
    /// </summary>
    public int? BehindBy { get; }

    /// <summary>Gets the number of staged (index) changes.</summary>
    public int StagedCount { get; }

    /// <summary>Gets the number of unstaged working-tree modifications.</summary>
    public int ModifiedCount { get; }

    /// <summary>Gets the number of untracked files in the working tree.</summary>
    public int UntrackedCount { get; }

    /// <summary>Gets the number of stash entries.</summary>
    public int StashCount { get; }
}
