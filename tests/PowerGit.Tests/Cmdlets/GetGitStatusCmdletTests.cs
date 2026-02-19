using PowerGit.Cmdlets;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Tests.Cmdlets;

[TestClass]
public sealed class GetGitStatusCmdletTests
{
    [TestMethod]
    public void ResolvePath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService());

        var resolvedPath = cmdlet.ResolvePath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolvePath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService())
        {
            Path = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolvePath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    private sealed class StubGitWorkingTreeService : IGitWorkingTreeService
    {
        public GitStatusResult GetStatus(string repositoryPath)
        {
            return new GitStatusResult(repositoryPath, "main", Array.Empty<GitStatusEntry>(), 0, 0, 0);
        }

        public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options)
        {
            return Array.Empty<GitDiffEntry>();
        }
    }
}
