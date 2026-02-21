using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

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
            RepoPath = "D:\\other-repo",
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
            RepoPath = "D:\\git",
            Staged = true,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreEqual("D:\\git", options.RepositoryPath);
        Assert.IsTrue(options.Staged);
    }

    [TestMethod]
    public void BuildOptions_CommitSet_CommitPropagated()
    {
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService())
        {
            Commit = "abc1234",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("abc1234", options.Commit);
    }

    [TestMethod]
    public void BuildOptions_RangeSet_FromAndToPropagated()
    {
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService())
        {
            FromCommit = "abc1",
            ToCommit = "def2",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("abc1", options.FromCommit);
        Assert.AreEqual("def2", options.ToCommit);
    }

    [TestMethod]
    public void BuildOptions_IgnoreWhitespaceSet_Propagated()
    {
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService())
        {
            IgnoreWhitespace = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.IgnoreWhitespace);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var expected = new GitDiffOptions { RepositoryPath = "D:\\repo" };
        var cmdlet = new GetGitDiffCmdlet(new StubGitWorkingTreeService())
        {
            Options = expected,
        };

        Assert.AreSame(expected, cmdlet.Options);
    }

    private sealed class StubGitWorkingTreeService : IGitWorkingTreeService
    {
        public GitStatusResult GetStatus(GitStatusOptions options)
        {
            return new GitStatusResult(options.RepositoryPath, "main", Array.Empty<GitStatusEntry>(), 0, 0, 0);
        }

        public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options)
        {
            return Array.Empty<GitDiffEntry>();
        }

        public void Stage(GitStageOptions options) => throw new NotImplementedException();

        public void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null) =>
            throw new NotImplementedException();

        public void Reset(GitResetOptions options) => throw new NotImplementedException();
    }
}
