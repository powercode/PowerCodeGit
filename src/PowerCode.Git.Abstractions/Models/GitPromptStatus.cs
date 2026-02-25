namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents the git repository status information used to build a shell prompt segment.
/// </summary>
/// <remarks>
/// Retrieve an instance via <c>Get-GitPromptStatus</c>. The <see cref="FormattedString"/>
/// property (populated by the cmdlet) is a Powerline-styled, ANSI-colored string ready for
/// embedding in a shell prompt. When used in a PowerShell string interpolation context
/// (e.g. <c>"$(Get-GitPromptStatus) &gt;"</c>), <see cref="ToString()"/> returns that string.
/// </remarks>
public sealed class GitPromptStatus
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitPromptStatus"/> class.
    /// </summary>
    /// <param name="repositoryPath">The absolute path to the repository root.</param>
    /// <param name="branchName">The current branch name, or a short SHA when in detached HEAD state.</param>
    /// <param name="upstreamProvider">The detected hosting provider of the upstream remote.</param>
    /// <param name="trackedBranchName">The name of the tracking branch, or <see langword="null"/> when not set.</param>
    /// <param name="aheadBy">The number of commits ahead of the upstream, or <see langword="null"/> when unavailable.</param>
    /// <param name="behindBy">The number of commits behind the upstream, or <see langword="null"/> when unavailable.</param>
    /// <param name="stagedCount">The number of staged changes.</param>
    /// <param name="modifiedCount">The number of unstaged modifications.</param>
    /// <param name="untrackedCount">The number of untracked files.</param>
    /// <param name="stashCount">The number of stash entries.</param>
    /// <param name="isDetachedHead">
    /// <see langword="true"/> when the repository is in detached HEAD state;
    /// <see langword="false"/> when on a named branch.
    /// </param>
    public GitPromptStatus(
        string repositoryPath,
        string branchName,
        GitUpstreamProvider upstreamProvider,
        string? trackedBranchName,
        int? aheadBy,
        int? behindBy,
        int stagedCount,
        int modifiedCount,
        int untrackedCount,
        int stashCount,
        bool isDetachedHead)
    {
        RepositoryPath = repositoryPath;
        BranchName = branchName;
        UpstreamProvider = upstreamProvider;
        TrackedBranchName = trackedBranchName;
        AheadBy = aheadBy;
        BehindBy = behindBy;
        StagedCount = stagedCount;
        ModifiedCount = modifiedCount;
        UntrackedCount = untrackedCount;
        StashCount = stashCount;
        IsDetachedHead = isDetachedHead;
    }

    /// <summary>
    /// Gets the absolute path to the repository root.
    /// </summary>
    public string RepositoryPath { get; }

    /// <summary>
    /// Gets the current branch name, or a short SHA when in detached HEAD state.
    /// </summary>
    public string BranchName { get; }

    /// <summary>
    /// Gets the detected hosting provider of the upstream remote.
    /// </summary>
    public GitUpstreamProvider UpstreamProvider { get; }

    /// <summary>
    /// Gets the name of the upstream tracking branch (e.g. <c>origin/main</c>),
    /// or <see langword="null"/> when no tracking branch is configured.
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

    /// <summary>
    /// Gets the number of staged (index) changes.
    /// </summary>
    public int StagedCount { get; }

    /// <summary>
    /// Gets the number of unstaged working-tree modifications.
    /// </summary>
    public int ModifiedCount { get; }

    /// <summary>
    /// Gets the number of untracked files in the working tree.
    /// </summary>
    public int UntrackedCount { get; }

    /// <summary>
    /// Gets the number of stash entries in the repository.
    /// </summary>
    public int StashCount { get; }

    /// <summary>
    /// Gets a value indicating whether the repository is in detached HEAD state.
    /// </summary>
    public bool IsDetachedHead { get; }

    /// <summary>
    /// Gets or sets the pre-formatted, ANSI-colored prompt string produced by
    /// <c>Get-GitPromptStatus</c>. When set, <see cref="ToString()"/> returns this value.
    /// </summary>
    public string? FormattedString { get; set; }

    /// <summary>
    /// Returns the <see cref="FormattedString"/> when available,
    /// or a plain-text fallback of the form <c>[branch]</c>.
    /// </summary>
    /// <returns>A string representation suitable for embedding in a shell prompt.</returns>
    public override string ToString() =>
        FormattedString ?? $"[{BranchName}]";
}
