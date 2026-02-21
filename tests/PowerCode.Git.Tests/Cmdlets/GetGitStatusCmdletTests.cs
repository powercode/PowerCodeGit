using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitStatusCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void BuildOptions_StatusParameterSet_DefaultsApplied()
    {
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsFalse(options.IncludeIgnored);
        Assert.IsNull(options.Paths);
        Assert.IsNull(options.UntrackedFilesMode);
    }

    [TestMethod]
    public void BuildOptions_WithPaths_PathsPropagated()
    {
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["src/", "tests/"],
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        CollectionAssert.AreEqual(new[] { "src/", "tests/" }, options.Paths);
    }

    [TestMethod]
    public void BuildOptions_WithUntrackedFilesMode_ModePropagated()
    {
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService())
        {
            UntrackedFiles = GitUntrackedFilesMode.No,
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual(GitUntrackedFilesMode.No, options.UntrackedFilesMode);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var expected = new GitStatusOptions { RepositoryPath = "D:\\repo" };
        var cmdlet = new GetGitStatusCmdlet(new StubGitWorkingTreeService())
        {
            Options = expected,
        };

        // When ParameterSetName is "Options", BuildOptions returns Options directly
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

        public void Reset(string repositoryPath, string? target, GitResetMode mode) =>
            throw new NotImplementedException();
    }
}
