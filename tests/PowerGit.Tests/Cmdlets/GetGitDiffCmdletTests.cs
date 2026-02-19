using PowerGit.Cmdlets;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Tests.Cmdlets;

[TestClass]
public sealed class GetGitDiffCmdletTests
{
    [TestMethod]
    public void BuildOptions_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService())
        {
            Path = "D:\\other-repo",
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_StagedNotSet_DefaultsToFalse()
    {
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsFalse(options.Staged);
    }

    [TestMethod]
    public void BuildOptions_AllParametersSpecified_MapsToGitDiffOptions()
    {
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService())
        {
            Path = "D:\\git",
            Staged = true,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreEqual("D:\\git", options.RepositoryPath);
        Assert.IsTrue(options.Staged);
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
