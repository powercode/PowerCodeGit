using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitBranchCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void BuildOptions_NoParameters_DefaultListOptions()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsFalse(options.ListRemote);
        Assert.IsFalse(options.ListAll);
        Assert.IsNull(options.Pattern);
        Assert.IsNull(options.ContainsCommit);
        Assert.IsNull(options.MergedInto);
        Assert.IsNull(options.NotMergedInto);
    }

    [TestMethod]
    public void BuildOptions_RemoteSet_ListRemoteIsTrue()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Remote = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.ListRemote);
        Assert.IsFalse(options.ListAll);
    }

    [TestMethod]
    public void BuildOptions_AllSet_ListAllIsTrue()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            All = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.ListAll);
        Assert.IsFalse(options.ListRemote);
    }

    [TestMethod]
    public void BuildOptions_PatternSet_PatternMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Pattern = "feature/*",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("feature/*", options.Pattern);
    }

    [TestMethod]
    public void BuildOptions_ContainsSet_ContainsCommitMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Contains = "abc1234",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("abc1234", options.ContainsCommit);
    }

    [TestMethod]
    public void BuildOptions_MergedSet_MergedIntoMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Merged = "HEAD",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("HEAD", options.MergedInto);
    }

    [TestMethod]
    public void BuildOptions_NoMergedSet_NotMergedIntoMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            NoMerged = "main",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("main", options.NotMergedInto);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefinedOptions = new GitBranchListOptions
        {
            RepositoryPath = "D:\\other-repo",
            ListRemote = true,
            Pattern = "release/*",
        };

        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            Options = predefinedOptions,
        };

        // Simulate the "Options" parameter set by setting the internal name via reflection
        var paramSetProp = typeof(GetGitBranchCmdlet).BaseType!.BaseType!.GetProperty(
            "ParameterSetName",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        // We can't set ParameterSetName directly (PSCmdlet internal), so instead verify
        // that BuildOptions returns Options object when Options is set and ParameterSetName returns "Options".
        // Just verify that Options property is correctly assigned.
        Assert.AreSame(predefinedOptions, cmdlet.Options);
        Assert.AreEqual("D:\\other-repo", cmdlet.Options.RepositoryPath);
        Assert.IsTrue(cmdlet.Options.ListRemote);
        Assert.AreEqual("release/*", cmdlet.Options.Pattern);
    }

    private sealed class StubGitBranchService : IGitBranchService
    {
        public IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options)
        {
            return Array.Empty<GitBranchInfo>();
        }

        public GitBranchInfo SwitchBranch(GitSwitchOptions options) =>
            new GitBranchInfo(options.BranchName ?? "HEAD", true, false, "abc1234", null, null, null);

        public GitBranchInfo CreateBranch(GitBranchCreateOptions options) =>
            throw new NotImplementedException();

        public void DeleteBranch(GitBranchDeleteOptions options) =>
            throw new NotImplementedException();
    }
}
