using PowerGit.Cmdlets;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Tests.Cmdlets;

[TestClass]
public sealed class SwitchGitBranchCmdletTests
{
    [TestMethod]
    public void ResolvePath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new SwitchGitBranchCmdlet(new StubGitBranchService())
        {
            Name = "feature",
        };

        var resolvedPath = cmdlet.ResolvePath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolvePath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new SwitchGitBranchCmdlet(new StubGitBranchService())
        {
            Path = "D:\\other-repo",
            Name = "feature",
        };

        var resolvedPath = cmdlet.ResolvePath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Name_IsSetCorrectly()
    {
        var cmdlet = new SwitchGitBranchCmdlet(new StubGitBranchService())
        {
            Name = "develop",
        };

        Assert.AreEqual("develop", cmdlet.Name);
    }

    private sealed class StubGitBranchService : IGitBranchService
    {
        public IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath)
        {
            return Array.Empty<GitBranchInfo>();
        }

        public GitBranchInfo SwitchBranch(string repositoryPath, string branchName)
        {
            return new GitBranchInfo(branchName, true, false, "abc1234", null, null, null);
        }
    }
}
