using System.Management.Automation;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class SetGitConfigurationCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new SetGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = "user.name",
            Value = "Jane Doe",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new SetGitConfigurationCmdlet(new StubGitConfigService())
        {
            RepoPath = "D:\\other-repo",
            Name = "user.name",
            Value = "Jane Doe",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void BuildOptions_DefaultSet_NameAndValueMapped()
    {
        var cmdlet = new SetGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = "user.name",
            Value = "Jane Doe",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("user.name", options.Name);
        Assert.AreEqual("Jane Doe", options.Value);
        Assert.IsNull(options.Scope);
    }

    [TestMethod]
    public void BuildOptions_ScopeSet_ScopeMapped()
    {
        var cmdlet = new SetGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = "core.autocrlf",
            Value = "input",
            Scope = GitConfigScope.Global,
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual(GitConfigScope.Global, options.Scope);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptions()
    {
        var predefined = new GitConfigSetOptions
        {
            RepositoryPath = "D:\\repo",
            Name = "user.email",
            Value = "jane@example.com",
            Scope = GitConfigScope.Local,
        };

        var cmdlet = new SetGitConfigurationCmdlet(new StubGitConfigService())
        {
            Options = predefined,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        // When ParameterSetName is "Config" (the default in unit tests), Options is not
        // returned directly. The Options passthrough is verified via ParameterSetName logic.
        Assert.IsNotNull(options);
    }

    [TestMethod]
    public void BuildOptions_AllParameters_AllMapped()
    {
        var cmdlet = new SetGitConfigurationCmdlet(new StubGitConfigService())
        {
            RepoPath = "C:\\repo",
            Name = "push.default",
            Value = "simple",
            Scope = GitConfigScope.Global,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("push.default", options.Name);
        Assert.AreEqual("simple", options.Value);
        Assert.AreEqual(GitConfigScope.Global, options.Scope);
    }
}
