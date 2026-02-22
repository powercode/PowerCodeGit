using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git;

/// <summary>
/// Creates service instances used by cmdlets.
/// </summary>
internal static class ServiceFactory
{
    /// <summary>
    /// Creates a git history service implementation loaded from the isolated
    /// dependency context. The returned instance is strongly typed via the
    /// shared <see cref="IGitHistoryService"/> interface.
    /// </summary>
    /// <returns>An initialized <see cref="IGitHistoryService"/> instance.</returns>
    public static IGitHistoryService CreateGitHistoryService()
    {
        return DependencyContext.CreateGitHistoryService();
    }

    /// <summary>
    /// Creates a git working tree service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitWorkingTreeService"/> instance.</returns>
    public static IGitWorkingTreeService CreateGitWorkingTreeService()
    {
        return DependencyContext.CreateGitWorkingTreeService();
    }

    /// <summary>
    /// Creates a git branch service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitBranchService"/> instance.</returns>
    public static IGitBranchService CreateGitBranchService()
    {
        return DependencyContext.CreateGitBranchService();
    }

    /// <summary>
    /// Creates a git tag service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitTagService"/> instance.</returns>
    public static IGitTagService CreateGitTagService()
    {
        return DependencyContext.CreateGitTagService();
    }

    /// <summary>
    /// Creates a git path service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitPathService"/> instance.</returns>
    public static IGitPathService CreateGitPathService()
    {
        return DependencyContext.CreateGitPathService();
    }

    /// <summary>
    /// Creates a git remote service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitRemoteService"/> instance.</returns>
    public static IGitRemoteService CreateGitRemoteService()
    {
        return DependencyContext.CreateGitRemoteService();
    }

    /// <summary>
    /// Creates a git worktree service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitWorktreeService"/> instance.</returns>
    public static IGitWorktreeService CreateGitWorktreeService()
    {
        return DependencyContext.CreateGitWorktreeService();
    }

    /// <summary>
    /// Creates a git rebase service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitRebaseService"/> instance.</returns>
    public static IGitRebaseService CreateGitRebaseService()
    {
        return DependencyContext.CreateGitRebaseService();
    }

    /// <summary>
    /// Creates a git commit file service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitCommitFileService"/> instance.</returns>
    public static IGitCommitFileService CreateGitCommitFileService()
    {
        return DependencyContext.CreateGitCommitFileService();
    }
}
