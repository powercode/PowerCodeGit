using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class NewGitBranchCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            Name = "feature",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "D:\\other-repo",
            Name = "feature",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Name_IsSetCorrectly()
    {
        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            Name = "feature/new-branch",
        };

        Assert.AreEqual("feature/new-branch", cmdlet.Name);
    }

    [TestMethod]
    public void BuildOptions_NoOptionalParams_DefaultsApplied()
    {
        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Name = "feature",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("feature", options.Name);
        Assert.IsNull(options.StartPoint);
        Assert.IsFalse(options.Track);
        Assert.IsFalse(options.Force);
    }

    [TestMethod]
    public void BuildOptions_StartPointSet_Mapped()
    {
        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Name = "hotfix",
            StartPoint = "v1.0.0",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("v1.0.0", options.StartPoint);
    }

    [TestMethod]
    public void BuildOptions_TrackSet_TrackIsTrue()
    {
        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Name = "feature",
            Track = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Track);
    }

    [TestMethod]
    public void BuildOptions_ForceSet_ForceIsTrue()
    {
        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Name = "feature",
            Force = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Force);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefinedOptions = new GitBranchCreateOptions
        {
            RepositoryPath = "D:\\other",
            Name = "release/1.0",
            StartPoint = "main",
            Force = true,
        };

        var cmdlet = new NewGitBranchCmdlet(new StubGitBranchService())
        {
            Options = predefinedOptions,
        };

        Assert.AreSame(predefinedOptions, cmdlet.Options);
        Assert.AreEqual("release/1.0", cmdlet.Options.Name);
        Assert.AreEqual("main", cmdlet.Options.StartPoint);
        Assert.IsTrue(cmdlet.Options.Force);
    }

    private sealed class StubGitBranchService : IGitBranchService
    {
        public IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options) =>
            Array.Empty<GitBranchInfo>();

        public GitBranchInfo SwitchBranch(GitSwitchOptions options) =>
            new(options.BranchName ?? "HEAD", true, false, "abc1234", null, null, null);

        public GitBranchInfo CreateBranch(GitBranchCreateOptions options) =>
            new(options.Name, true, false, "abc1234", null, null, null);

        public void DeleteBranch(GitBranchDeleteOptions options)
        {
        }
    }
}
