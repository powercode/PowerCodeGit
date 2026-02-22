using System;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitCommitFileCmdletTests
{
    [TestMethod]
    public void BuildOptions_DefaultCommit_UsesCurrentPathAndNullCommit()
    {
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsNull(options.Commit);
    }

    [TestMethod]
    public void BuildOptions_WithCommitParam_SetsCommitSha()
    {
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            Commit = "abc1234",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("abc1234", options.Commit);
    }

    [TestMethod]
    public void BuildOptions_WithInputObject_UsesShaFromCommitInfo()
    {
        var commitInfo = new GitCommitInfo(
            "def5678abcdef5678abcdef5678abcdef5678abcd",
            "Author",
            "author@test.com",
            DateTimeOffset.Now,
            "Committer",
            "committer@test.com",
            DateTimeOffset.Now,
            "Test commit",
            "Test commit\n\nDetails",
            Array.Empty<string>());

        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            InputObject = commitInfo,
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("def5678abcdef5678abcdef5678abcdef5678abcd", options.Commit);
    }

    [TestMethod]
    public void BuildOptions_CommitParamTakesPrecedenceOverInputObject()
    {
        var commitInfo = new GitCommitInfo(
            "def5678abcdef5678abcdef5678abcdef5678abcd",
            "Author",
            "author@test.com",
            DateTimeOffset.Now,
            "Committer",
            "committer@test.com",
            DateTimeOffset.Now,
            "Test commit",
            "Test commit",
            Array.Empty<string>());

        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            Commit = "explicit123",
            InputObject = commitInfo,
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("explicit123", options.Commit);
    }

    [TestMethod]
    public void BuildOptions_WithPaths_SetsPathFilter()
    {
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            Path = ["src/file.cs", "README.md"],
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsNotNull(options.Paths);
        Assert.HasCount(2, options.Paths);
        Assert.AreEqual("src/file.cs", options.Paths[0]);
    }

    [TestMethod]
    public void BuildOptions_WithRepoPath_UsesProvidedPath()
    {
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            RepoPath = "D:\\other-repo",
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_IgnoreWhitespaceSet_Propagated()
    {
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            IgnoreWhitespace = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.IgnoreWhitespace);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var expected = new GitCommitFileOptions { RepositoryPath = "D:\\repo" };
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            Options = expected,
        };

        Assert.AreSame(expected, cmdlet.Options);
    }

    [TestMethod]
    public void Hunk_DefaultsToFalse()
    {
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService());

        Assert.IsFalse(cmdlet.Hunk.IsPresent);
    }

    [TestMethod]
    public void Hunk_WhenSet_IsTrue()
    {
        var cmdlet = new GetGitCommitFileCmdlet(new StubGitCommitFileService())
        {
            Hunk = new System.Management.Automation.SwitchParameter(true),
        };

        Assert.IsTrue(cmdlet.Hunk.IsPresent);
    }
}
