using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class RemoveGitTagCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new RemoveGitTagCmdlet(new StubGitTagService())
        {
            Name = "v1.0.0",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new RemoveGitTagCmdlet(new StubGitTagService())
        {
            RepoPath = "D:\\other-repo",
            Name = "v1.0.0",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Name_IsSetCorrectly()
    {
        var cmdlet = new RemoveGitTagCmdlet(new StubGitTagService())
        {
            Name = "v2.0.0",
        };

        Assert.AreEqual("v2.0.0", cmdlet.Name);
    }

    [TestMethod]
    public void BuildOptions_DefaultParameterSet_BuildsFromParameters()
    {
        var cmdlet = new RemoveGitTagCmdlet(new StubGitTagService())
        {
            RepoPath = "C:\\repo",
            Name = "v1.0.0",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("v1.0.0", options.Name);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefinedOptions = new GitTagDeleteOptions
        {
            RepositoryPath = "D:\\other",
            Name = "v3.0.0",
        };

        var cmdlet = new RemoveGitTagCmdlet(new StubGitTagService())
        {
            Options = predefinedOptions,
        };

        Assert.AreSame(predefinedOptions, cmdlet.Options);
        Assert.AreEqual("v3.0.0", cmdlet.Options.Name);
    }
}
