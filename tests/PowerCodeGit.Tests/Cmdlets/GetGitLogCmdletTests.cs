using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitLogCmdletTests
{
    [TestMethod]
    public void BuildOptions_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitLogCmdlet(new StubGitHistoryService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_AllParametersSpecified_MapsToGitLogOptions()
    {
        var cmdlet = new GetGitLogCmdlet(new StubGitHistoryService())
        {
            RepoPath = "D:\\git",
            Branch = "main",
            MaxCount = 15,
            Author = "alice",
            Since = new DateTime(2024, 01, 01),
            Until = new DateTime(2024, 12, 31),
            MessagePattern = "fix",
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreEqual("D:\\git", options.RepositoryPath);
        Assert.AreEqual("main", options.BranchName);
        Assert.AreEqual(15, options.MaxCount);
        Assert.AreEqual("alice", options.AuthorFilter);
        Assert.AreEqual(new DateTimeOffset(new DateTime(2024, 01, 01)), options.Since);
        Assert.AreEqual(new DateTimeOffset(new DateTime(2024, 12, 31)), options.Until);
        Assert.AreEqual("fix", options.MessagePattern);
    }

    private sealed class StubGitHistoryService : IGitHistoryService
    {
        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options)
        {
            return Array.Empty<GitCommitInfo>();
        }
    }
}