using PowerGit.Abstractions.Services;

namespace PowerGit;

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
}