using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class SaveGitCommitCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Message_IsSetCorrectly()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            Message = "Initial commit",
        };

        Assert.AreEqual("Initial commit", cmdlet.Message);
    }

    [TestMethod]
    public void Amend_DefaultsToFalse()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService());

        Assert.IsFalse(cmdlet.Amend.IsPresent);
    }

    [TestMethod]
    public void AllowEmpty_DefaultsToFalse()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService());

        Assert.IsFalse(cmdlet.AllowEmpty.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_DefaultParameterSet_MapsAllStandardProperties()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            Message = "feat: new feature",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("feat: new feature", options.Message);
        Assert.IsFalse(options.Amend);
        Assert.IsFalse(options.AllowEmpty);
        Assert.IsFalse(options.All);
        Assert.IsNull(options.Author);
        Assert.IsNull(options.Date);
    }

    [TestMethod]
    public void BuildOptions_AllSet_AllTrue()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            Message = "chore: stage all",
        };
        cmdlet.All = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.All);
    }

    [TestMethod]
    public void BuildOptions_AuthorSet_AuthorMapped()
    {
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            Message = "fix: with author",
            Author = "Jane Doe <jane@example.com>",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("Jane Doe <jane@example.com>", options.Author);
    }

    [TestMethod]
    public void BuildOptions_DateSet_DateMapped()
    {
        var expectedDate = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            Message = "fix: backdated",
            Date = expectedDate,
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual(expectedDate, options.Date);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var prebuilt = new GitCommitOptions
        {
            RepositoryPath = "D:\\explicit-repo",
            Message = "explicit commit",
            Author = "Custom Author <custom@example.com>",
        };
        var cmdlet = new SaveGitCommitCmdlet(new StubGitHistoryService())
        {
            Options = prebuilt,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(prebuilt, options);
    }

    private sealed class StubGitHistoryService : IGitHistoryService
    {
        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options) =>
            Array.Empty<GitCommitInfo>();

        public GitCommitInfo Commit(GitCommitOptions options) =>
            new(
                "abc1234",
                "Test User",
                "test@example.com",
                DateTimeOffset.Now,
                "Test User",
                "test@example.com",
                DateTimeOffset.Now,
                options.Message ?? "test",
                options.Message ?? "test",
                []);
    }
}

