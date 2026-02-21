using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class ReceiveGitBranchCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void MergeStrategy_DefaultsToMerge()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService());

        Assert.AreEqual(GitMergeStrategy.Merge, cmdlet.MergeStrategy);
    }

    [TestMethod]
    public void MergeStrategy_IsSetCorrectly()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService())
        {
            MergeStrategy = GitMergeStrategy.FastForward,
        };

        Assert.AreEqual(GitMergeStrategy.FastForward, cmdlet.MergeStrategy);
    }

    [TestMethod]
    public void Prune_DefaultsToFalse()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService());

        Assert.IsFalse(cmdlet.Prune.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_DefaultSet_MergeStrategyAndPathMapped()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual(GitMergeStrategy.Merge, options.MergeStrategy);
        Assert.IsFalse(options.AutoStash);
        Assert.IsNull(options.Tags);
    }

    [TestMethod]
    public void BuildOptions_AutoStashSet_AutoStashMapped()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService())
        {
            AutoStash = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.AutoStash);
    }

    [TestMethod]
    public void BuildOptions_TagsSet_TagsTrueInOptions()
    {
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService())
        {
            Tags = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Tags);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefined = new GitPullOptions { RepositoryPath = "D:\\repo", AutoStash = true };
        var cmdlet = new ReceiveGitBranchCmdlet(new StubGitRemoteService())
        {
            Options = predefined,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(predefined, options);
    }

    private sealed class StubGitRemoteService : IGitRemoteService
    {
        public IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath) =>
            Array.Empty<GitRemoteInfo>();

        public string Clone(GitCloneOptions options, Action<int, string>? onProgress = null) =>
            "cloned-repo";

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
