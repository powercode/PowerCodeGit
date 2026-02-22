using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class ResumeGitRebaseCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new ResumeGitRebaseCmdlet(new StubGitRebaseService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new ResumeGitRebaseCmdlet(new StubGitRebaseService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Skip_DefaultsToFalse()
    {
        var cmdlet = new ResumeGitRebaseCmdlet(new StubGitRebaseService());

        Assert.IsFalse(cmdlet.Skip.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_DefaultValues_ContinueMode()
    {
        var cmdlet = new ResumeGitRebaseCmdlet(new StubGitRebaseService())
        {
            RepoPath = "C:\\repo",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsFalse(options.Skip);
    }

    [TestMethod]
    public void BuildOptions_SkipSet_SkipModeEnabled()
    {
        var cmdlet = new ResumeGitRebaseCmdlet(new StubGitRebaseService())
        {
            RepoPath = "C:\\repo",
        };
        cmdlet.Skip = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Skip);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var prebuilt = new GitRebaseContinueOptions { RepositoryPath = "C:\\repo", Skip = true };

        Assert.AreEqual("C:\\repo", prebuilt.RepositoryPath);
        Assert.IsTrue(prebuilt.Skip);
    }
}
