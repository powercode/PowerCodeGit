using System;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class CopyGitRepositoryCmdletTests
{
    [TestMethod]
    public void Url_IsSetCorrectly()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
        };

        Assert.AreEqual("https://github.com/user/repo.git", cmdlet.Url);
    }

    [TestMethod]
    public void LocalPath_DefaultsToNull()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
        };

        Assert.IsNull(cmdlet.LocalPath);
    }

    [TestMethod]
    public void LocalPath_IsSetCorrectly()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
            LocalPath = "C:\\repos\\my-repo",
        };

        Assert.AreEqual("C:\\repos\\my-repo", cmdlet.LocalPath);
    }

    [TestMethod]
    public void SingleBranch_DefaultsToFalse()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
        };

        Assert.IsFalse(cmdlet.SingleBranch.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_DefaultSet_UrlMapped()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://github.com/user/repo.git",
        };

        var options = cmdlet.BuildOptions();

        Assert.AreEqual("https://github.com/user/repo.git", options.Url);
        Assert.IsNull(options.LocalPath);
        Assert.IsFalse(options.Bare);
        Assert.IsFalse(options.RecurseSubmodules);
    }

    [TestMethod]
    public void BuildOptions_BranchNameSet_BranchNameMapped()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://example.com/repo.git",
            BranchName = "develop",
        };

        var options = cmdlet.BuildOptions();

        Assert.AreEqual("develop", options.BranchName);
    }

    [TestMethod]
    public void BuildOptions_BareSet_BareMapped()
    {
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Url = "https://example.com/repo.git",
        };
        cmdlet.Bare = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions();

        Assert.IsTrue(options.Bare);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefined = new GitCloneOptions { Url = "https://example.com/repo.git", BranchName = "main" };
        var cmdlet = new CopyGitRepositoryCmdlet(new StubGitRemoteService())
        {
            Options = predefined,
        };

        var options = cmdlet.BuildOptions();

        Assert.AreSame(predefined, options);
    }

}
