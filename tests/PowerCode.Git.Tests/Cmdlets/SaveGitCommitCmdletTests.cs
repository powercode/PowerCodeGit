using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class SaveGitCommitCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Message_IsSetCorrectly()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            Message = "Initial commit",
        };

        Assert.AreEqual("Initial commit", cmdlet.Message);
    }

    [TestMethod]
    public void Amend_DefaultsToFalse()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService());

        Assert.IsFalse(cmdlet.Amend.IsPresent);
    }

    [TestMethod]
    public void AllowEmpty_DefaultsToFalse()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService());

        Assert.IsFalse(cmdlet.AllowEmpty.IsPresent);
    }

    private sealed class StubGitHistoryService : IGitHistoryService
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
}
