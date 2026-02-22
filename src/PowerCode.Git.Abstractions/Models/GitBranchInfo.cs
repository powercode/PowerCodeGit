namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents information about a git branch.
/// </summary>
public sealed class GitBranchInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitBranchInfo"/> class.
    /// </summary>
    /// <param name="name">The branch name.</param>
    /// <param name="isHead">Whether this branch is the current HEAD.</param>
    /// <param name="isRemote">Whether this is a remote-tracking branch.</param>
    /// <param name="tipSha">The full SHA of the branch tip commit.</param>
    /// <param name="trackedBranchName">The name of the tracked remote branch, or <see langword="null"/>.</param>
    /// <param name="aheadBy">The number of commits ahead of the tracked branch, or <see langword="null"/>.</param>
    /// <param name="behindBy">The number of commits behind the tracked branch, or <see langword="null"/>.</param>
    public GitBranchInfo(
        string name,
        bool isHead,
        bool isRemote,
        string tipSha,
        string? trackedBranchName,
        int? aheadBy,
        int? behindBy)
    {
        Name = name;
        IsHead = isHead;
        IsRemote = isRemote;
        TipSha = tipSha;
        TipShortSha = tipSha.Length > GitConstants.ShortShaLength ? tipSha[..GitConstants.ShortShaLength] : tipSha;
        TrackedBranchName = trackedBranchName;
        AheadBy = aheadBy;
        BehindBy = behindBy;
    }

    /// <summary>
    /// Gets the branch name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether this branch is the current HEAD.
    /// </summary>
    public bool IsHead { get; }

    /// <summary>
    /// Gets a value indicating whether this is a remote-tracking branch.
    /// </summary>
    public bool IsRemote { get; }

    /// <summary>
    /// Gets the full SHA of the branch tip commit.
    /// </summary>
    public string TipSha { get; }

    /// <summary>
    /// Gets the short SHA of the branch tip commit.
    /// </summary>
    public string TipShortSha { get; }

    /// <summary>
    /// Gets the name of the tracked remote branch, or <see langword="null"/> if not tracking.
    /// </summary>
    public string? TrackedBranchName { get; }

    /// <summary>
    /// Gets the number of commits ahead of the tracked branch, or <see langword="null"/>.
    /// </summary>
    public int? AheadBy { get; }

    /// <summary>
    /// Gets the number of commits behind the tracked branch, or <see langword="null"/>.
    /// </summary>
    public int? BehindBy { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var head = IsHead ? "* " : "  ";
        var tracking = TrackedBranchName is not null
            ? $" [{TrackedBranchName}: ahead {AheadBy ?? 0}, behind {BehindBy ?? 0}]"
            : string.Empty;

        return $"{head}{Name} {TipShortSha}{tracking}";
    }
}
