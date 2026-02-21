using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class RemoveGitBranchCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new RemoveGitBranchCmdlet(new StubGitBranchService())
        {
            Name = "feature",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new RemoveGitBranchCmdlet(new StubGitBranchService())
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
        var cmdlet = new RemoveGitBranchCmdlet(new StubGitBranchService())
        {
            Name = "feature/old",
        };

        Assert.AreEqual("feature/old", cmdlet.Name);
    }

    [TestMethod]
    public void Force_DefaultsToFalse()
    {
        var cmdlet = new RemoveGitBranchCmdlet(new StubGitBranchService())
        {
            Name = "feature",
        };

        Assert.IsFalse(cmdlet.Force.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_NoForce_DefaultsApplied()
    {
        var cmdlet = new RemoveGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Name = "feature",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("feature", options.Name);
        Assert.IsFalse(options.Force);
    }

    [TestMethod]
    public void BuildOptions_ForceSet_ForceIsTrue()
    {
        var cmdlet = new RemoveGitBranchCmdlet(new StubGitBranchService())
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
        var predefinedOptions = new GitBranchDeleteOptions
        {
            RepositoryPath = "D:\\other",
            Name = "old-feature",
            Force = true,
        };

        var cmdlet = new RemoveGitBranchCmdlet(new StubGitBranchService())
        {
            Options = predefinedOptions,
        };

        Assert.AreSame(predefinedOptions, cmdlet.Options);
        Assert.AreEqual("old-feature", cmdlet.Options.Name);
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
