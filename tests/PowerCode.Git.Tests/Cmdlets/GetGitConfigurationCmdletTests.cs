using System.Management.Automation;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitConfigurationCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitConfigurationCmdlet(new StubGitConfigService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitConfigurationCmdlet(new StubGitConfigService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void BuildOptions_NoName_NameIsNull()
    {
        var cmdlet = new GetGitConfigurationCmdlet(new StubGitConfigService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsNull(options.Name);
        Assert.IsNull(options.Scope);
    }

    [TestMethod]
    public void BuildOptions_NameSet_NameMapped()
    {
        var cmdlet = new GetGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = "user.name",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("user.name", options.Name);
    }

    [TestMethod]
    public void BuildOptions_ScopeSet_ScopeMapped()
    {
        var cmdlet = new GetGitConfigurationCmdlet(new StubGitConfigService())
        {
            Scope = GitConfigScope.Global,
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual(GitConfigScope.Global, options.Scope);
    }

    [TestMethod]
    public void BuildOptions_AllParameters_AllMapped()
    {
        var cmdlet = new GetGitConfigurationCmdlet(new StubGitConfigService())
        {
            RepoPath = "C:\\repo",
            Name = "core.autocrlf",
            Scope = GitConfigScope.Local,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("core.autocrlf", options.Name);
        Assert.AreEqual(GitConfigScope.Local, options.Scope);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptions()
    {
        var predefined = new GitConfigGetOptions
        {
            RepositoryPath = "D:\\repo",
            Name = "user.email",
            Scope = GitConfigScope.Global,
        };

        var cmdlet = new GetGitConfigurationCmdlet(new StubGitConfigService())
        {
            Options = predefined,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.IsNotNull(options);
    }
}
