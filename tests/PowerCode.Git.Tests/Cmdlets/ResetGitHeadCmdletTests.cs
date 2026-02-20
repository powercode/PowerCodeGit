using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class ResetGitHeadCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Revision_DefaultsToNull()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        Assert.IsNull(cmdlet.Revision);
    }

    [TestMethod]
    public void Revision_IsSetCorrectly()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService())
        {
            Revision = "HEAD~1",
        };

        Assert.AreEqual("HEAD~1", cmdlet.Revision);
    }

    [TestMethod]
    public void Hard_DefaultsToFalse()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        Assert.IsFalse(cmdlet.Hard.IsPresent);
    }

    [TestMethod]
    public void Soft_DefaultsToFalse()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        Assert.IsFalse(cmdlet.Soft.IsPresent);
    }

    private sealed class StubGitWorkingTreeService : IGitWorkingTreeService
    {
        public GitStatusResult GetStatus(GitStatusOptions options) =>
            new(options.RepositoryPath, "main", Array.Empty<GitStatusEntry>(), 0, 0, 0);

        public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options) =>
            Array.Empty<GitDiffEntry>();

        public void Stage(GitStageOptions options)
        {
        }

        public void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null)
        {
        }

        public void Reset(string repositoryPath, string? target, GitResetMode mode)
        {
        }
    }
}
