namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Controls how <c>Receive-GitBranch</c> handles remote-tracking branches received
/// from the pipeline (e.g. from <c>Get-GitBranch -Remote</c>).
/// </summary>
public enum ReceiveBranchAction
{
    /// <summary>
    /// Create a local tracking branch for each remote-tracking branch that does not
    /// already have a corresponding local branch. Branches that already exist locally
    /// are skipped without error.
    /// </summary>
    Create,

    /// <summary>
    /// Create a local tracking branch for each remote-tracking branch that does not
    /// already exist locally, and fast-forward any existing local branch whose
    /// corresponding remote-tracking ref is ahead of it.
    /// </summary>
    CreateOrUpdate,

    /// <summary>
    /// Fast-forward existing local branches whose corresponding remote-tracking ref is
    /// ahead of them. Remote-tracking branches that have no local counterpart are
    /// skipped without error.
    /// </summary>
    UpdateOnly,
}
