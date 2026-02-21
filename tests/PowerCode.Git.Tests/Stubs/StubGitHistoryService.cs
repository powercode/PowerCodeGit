using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitHistoryService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitHistoryService : IGitHistoryService
{
    public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options) =>
        Array.Empty<GitCommitInfo>();

    public GitCommitInfo Commit(GitCommitOptions options) =>
        new(
            "abc1234",
            "Test User",
            "test@example.com",
            DateTimeOffset.Now,
            "Test User",
            "test@example.com",
            DateTimeOffset.Now,
            options.Message ?? "test",
            options.Message ?? "test",
            []);
}
