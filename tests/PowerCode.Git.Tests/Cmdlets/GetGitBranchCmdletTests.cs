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

        public GitBranchInfo CreateBranch(string repositoryPath, string branchName) =>
            throw new NotImplementedException();

        public void DeleteBranch(string repositoryPath, string branchName, bool force = false) =>
            throw new NotImplementedException();
    }
}
