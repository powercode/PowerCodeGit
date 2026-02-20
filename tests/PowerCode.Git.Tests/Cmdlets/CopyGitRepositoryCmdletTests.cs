using System;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class CopyGitRepositoryCmdletTests
{
    [TestMethod]
    public void Url_IsSetCorrectly()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
        };

        Assert.AreEqual("https://github.com/user/repo.git", cmdlet.Url);
    }

    [TestMethod]
    public void LocalPath_DefaultsToNull()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
        };

        Assert.IsNull(cmdlet.LocalPath);
    }

    [TestMethod]
    public void LocalPath_IsSetCorrectly()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
            LocalPath = "C:\\repos\\my-repo",
        };

        Assert.AreEqual("C:\\repos\\my-repo", cmdlet.LocalPath);
    }

    [TestMethod]
    public void SingleBranch_DefaultsToFalse()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
        };

        Assert.IsFalse(cmdlet.SingleBranch.IsPresent);
    }

    private sealed class StubGitRemoteService : IGitRemoteService
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
}
