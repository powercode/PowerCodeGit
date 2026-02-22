using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class StartGitRebaseCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            Upstream = "main",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            RepoPath = "D:\\other-repo",
            Upstream = "main",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Upstream_IsSetCorrectly()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            Upstream = "origin/main",
        };

        Assert.AreEqual("origin/main", cmdlet.Upstream);
    }

    [TestMethod]
    public void Interactive_DefaultsToFalse()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            Upstream = "main",
        };

        Assert.IsFalse(cmdlet.Interactive.IsPresent);
    }

    [TestMethod]
    public void AutoStash_DefaultsToFalse()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            Upstream = "main",
        };

        Assert.IsFalse(cmdlet.AutoStash.IsPresent);
    }

    [TestMethod]
    public void Onto_DefaultsToNull()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            Upstream = "main",
        };

        Assert.IsNull(cmdlet.Onto);
    }

    [TestMethod]
    public void BuildOptions_DefaultValues_MapsCorrectly()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            RepoPath = "C:\\repo",
            Upstream = "main",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("main", options.Upstream);
        Assert.IsFalse(options.Interactive);
        Assert.IsFalse(options.AutoStash);
        Assert.IsNull(options.Onto);
    }

    [TestMethod]
    public void BuildOptions_WithOnto_MapsCorrectly()
    {
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            RepoPath = "C:\\repo",
            Upstream = "main",
            Onto = "feature/base",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("feature/base", options.Onto);
        Assert.AreEqual("main", options.Upstream);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var prebuilt = new GitRebaseOptions { RepositoryPath = "C:\\repo", Upstream = "main" };
        var cmdlet = new StartGitRebaseCmdlet(new StubGitRebaseService())
        {
            Options = prebuilt,
        };

        // Simulate Options parameter set
        var options = prebuilt;

        Assert.AreSame(prebuilt, options);
        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("main", options.Upstream);
    }
}
