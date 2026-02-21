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

    [TestMethod]
    public void BuildOptions_AllBranchesSet_AllBranchesTrue()
    {
        var cmdlet = new GetGitLogCmdlet(new StubGitHistoryService());
        cmdlet.AllBranches = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.AllBranches);
    }

    [TestMethod]
    public void BuildOptions_FirstParentSet_FirstParentTrue()
    {
        var cmdlet = new GetGitLogCmdlet(new StubGitHistoryService());
        cmdlet.FirstParent = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.FirstParent);
    }

    [TestMethod]
    public void BuildOptions_NoMergesSet_NoMergesTrue()
    {
        var cmdlet = new GetGitLogCmdlet(new StubGitHistoryService());
        cmdlet.NoMerges = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.NoMerges);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var prebuilt = new GitLogOptions
        {
            RepositoryPath = "D:\\prebuilt",
            AllBranches = true,
            FirstParent = true,
        };
        var cmdlet = new GetGitLogCmdlet(new StubGitHistoryService())
        {
            Options = prebuilt,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(prebuilt, options);
    }

    private sealed class StubGitHistoryService : IGitHistoryService
    {
        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options)
        {
            return Array.Empty<GitCommitInfo>();
        }

        public GitCommitInfo Commit(GitCommitOptions options) =>
            throw new NotImplementedException();
    }
}
