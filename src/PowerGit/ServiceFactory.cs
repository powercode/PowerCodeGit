using PowerGit.Core.Services;

namespace PowerGit;

/// <summary>
/// Creates service instances used by cmdlets.
/// </summary>
internal static class ServiceFactory
{
    /// <summary>
    /// Creates a git history service implementation.
    /// </summary>
    /// <returns>An initialized <see cref="IGitHistoryService"/> instance.</returns>
    public static IGitHistoryService CreateGitHistoryService()
    {
        return new GitHistoryService();
    }
}