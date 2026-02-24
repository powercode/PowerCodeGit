namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents the ahead/behind divergence of a branch relative to a specified reference branch.
/// </summary>
public sealed class GitBranchComparisonInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GitBranchComparisonInfo"/> class.
    /// </summary>
    /// <param name="referenceBranchName">The name of the reference branch (e.g. <c>origin/master</c>).</param>
    /// <param name="aheadBy">The number of commits the branch is ahead of the reference branch.</param>
    /// <param name="behindBy">The number of commits the branch is behind the reference branch.</param>
    public GitBranchComparisonInfo(string referenceBranchName, int aheadBy, int behindBy)
    {
        ReferenceBranchName = referenceBranchName;
        AheadBy = aheadBy;
        BehindBy = behindBy;
    }

    /// <summary>
    /// Gets the name of the reference branch used for comparison.
    /// </summary>
    public string ReferenceBranchName { get; }

    /// <summary>
    /// Gets the number of commits the branch is ahead of the reference branch.
    /// </summary>
    public int AheadBy { get; }

    /// <summary>
    /// Gets the number of commits the branch is behind the reference branch.
    /// </summary>
    public int BehindBy { get; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"(ahead {AheadBy}) | (behind {BehindBy}) {ReferenceBranchName}";
}
