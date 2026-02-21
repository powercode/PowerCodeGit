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

    private sealed class StubGitBranchService : IGitBranchService
    {
        public IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options) =>
            Array.Empty<GitBranchInfo>();

        public GitBranchInfo SwitchBranch(string repositoryPath, string branchName) =>
            new(branchName, true, false, "abc1234", null, null, null);

        public GitBranchInfo CreateBranch(GitBranchCreateOptions options) =>
            new(options.Name, true, false, "abc1234", null, null, null);

        public void DeleteBranch(string repositoryPath, string name, bool force = false)
        {
        }
    }
}
