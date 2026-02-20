using PowerCodeGit.Cmdlets;
using PowerCodeGit.Abstractions.Models;
using PowerCodeGit.Abstractions.Services;

namespace PowerCodeGit.Tests.Cmdlets;

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
