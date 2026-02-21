using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitRemoteService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitRemoteService : IGitRemoteService
{
    public IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath) =>
        Array.Empty<GitRemoteInfo>();

    public string Clone(GitCloneOptions options, Action<int, string>? onProgress = null) =>
        options.LocalPath ?? "cloned-repo";

    public GitBranchInfo Push(GitPushOptions options, Action<int, string>? onProgress = null) =>
        new("main", true, false, "abc1234", null, null, null);

    public GitCommitInfo Pull(GitPullOptions options, Action<int, string>? onProgress = null) =>
        new(
            "abc1234",
            "Test User",
            "test@example.com",
            DateTimeOffset.Now,
            "Test User",
            "test@example.com",
            DateTimeOffset.Now,
            "test",
            "test",
            []);
}
