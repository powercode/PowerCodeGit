using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git;

/// <summary>
/// Creates service instances used by cmdlets.
/// </summary>
/// <remarks>
/// Acts as the stable API surface between cmdlets and the <see cref="DependencyContext"/> ALC
/// plumbing. Cmdlets call <see cref="ServiceFactory"/> methods; the implementation delegates to
/// <see cref="DependencyContext.CreateService{T}"/>, which resolves types across the Assembly Load
/// Context boundary. This indirection ensures cmdlets remain isolated from internal loading details
/// and makes the construction points easy to locate when adding new services.
/// </remarks>
internal static class ServiceFactory
{
    /// <summary>
    /// Creates a git history service implementation loaded from the isolated
    /// dependency context. The returned instance is strongly typed via the
    /// shared <see cref="IGitHistoryService"/> interface.
    /// </summary>
    /// <returns>An initialized <see cref="IGitHistoryService"/> instance.</returns>
    public static IGitHistoryService CreateGitHistoryService() => DependencyContext.CreateService<IGitHistoryService>();

    /// <summary>
    /// Creates a git working tree service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitWorkingTreeService"/> instance.</returns>
    public static IGitWorkingTreeService CreateGitWorkingTreeService() => DependencyContext.CreateService<IGitWorkingTreeService>();

    /// <summary>
    /// Creates a git branch service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitBranchService"/> instance.</returns>
    public static IGitBranchService CreateGitBranchService() => DependencyContext.CreateService<IGitBranchService>();

    /// <summary>
    /// Creates a git tag service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitTagService"/> instance.</returns>
    public static IGitTagService CreateGitTagService() => DependencyContext.CreateService<IGitTagService>();

    /// <summary>
    /// Creates a git path service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitPathService"/> instance.</returns>
    public static IGitPathService CreateGitPathService() => DependencyContext.CreateService<IGitPathService>();

    /// <summary>
    /// Creates a git remote service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitRemoteService"/> instance.</returns>
    public static IGitRemoteService CreateGitRemoteService() => DependencyContext.CreateService<IGitRemoteService>();

    /// <summary>
    /// Creates a git worktree service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitWorktreeService"/> instance.</returns>
    public static IGitWorktreeService CreateGitWorktreeService() => DependencyContext.CreateService<IGitWorktreeService>();

    /// <summary>
    /// Creates a git rebase service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitRebaseService"/> instance.</returns>
    public static IGitRebaseService CreateGitRebaseService() => DependencyContext.CreateService<IGitRebaseService>();

    /// <summary>
    /// Creates a git commit file service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitCommitFileService"/> instance.</returns>
    public static IGitCommitFileService CreateGitCommitFileService() => DependencyContext.CreateService<IGitCommitFileService>();

    /// <summary>
    /// Creates a git configuration service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitConfigService"/> instance.</returns>
    public static IGitConfigService CreateGitConfigService() => DependencyContext.CreateService<IGitConfigService>();

    /// <summary>
    /// Creates a git commit search service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitCommitSearchService"/> instance.</returns>
    public static IGitCommitSearchService CreateGitCommitSearchService() => DependencyContext.CreateService<IGitCommitSearchService>();

    /// <summary>
    /// Creates a git tree comparison service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitTreeComparisonService"/> instance.</returns>
    public static IGitTreeComparisonService CreateGitTreeComparisonService() => DependencyContext.CreateService<IGitTreeComparisonService>();

    /// <summary>
    /// Creates a git history rewrite service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitHistoryRewriteService"/> instance.</returns>
    public static IGitHistoryRewriteService CreateGitHistoryRewriteService() => DependencyContext.CreateService<IGitHistoryRewriteService>();

    /// <summary>
    /// Creates a <c>LibGit2Sharp.Repository</c> instance from the isolated
    /// AssemblyLoadContext and opens the repository at <paramref name="repositoryPath"/>.
    /// </summary>
    /// <remarks>
    /// See <see cref="DependencyContext.CreateRepository"/> for details on the
    /// intentional ALC boundary break this entails.
    /// </remarks>
    /// <param name="repositoryPath">Path to the git repository.</param>
    /// <returns>
    /// A <c>LibGit2Sharp.Repository</c> instance as <see cref="object"/>.
    /// Cast to <see cref="System.IDisposable"/> to dispose.
    /// </returns>
    public static object CreateRepository(string repositoryPath) => DependencyContext.CreateRepository(repositoryPath);
}
