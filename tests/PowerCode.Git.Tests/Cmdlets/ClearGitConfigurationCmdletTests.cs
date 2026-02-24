using System;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class ClearGitConfigurationCmdletTests
{
    // ── ResolveRepositoryPath ────────────────────────────────────────────────

    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = ["user.name"],
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            RepoPath = "D:\\other-repo",
            Name = ["user.name"],
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    // ── Name parameter ───────────────────────────────────────────────────────

    [TestMethod]
    public void Name_SingleEntry_RoundTrips()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = ["user.name"],
        };

        Assert.HasCount(1, cmdlet.Name);
        Assert.AreEqual("user.name", cmdlet.Name[0]);
    }

    [TestMethod]
    public void Name_MultipleEntries_AllStoredInOrder()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = ["user.name", "user.email", "core.autocrlf"],
        };

        Assert.HasCount(3, cmdlet.Name);
        Assert.AreEqual("user.name", cmdlet.Name[0]);
        Assert.AreEqual("user.email", cmdlet.Name[1]);
        Assert.AreEqual("core.autocrlf", cmdlet.Name[2]);
    }

    // ── Scope parameter ──────────────────────────────────────────────────────

    [TestMethod]
    public void Scope_NotSet_IsNull()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = ["user.name"],
        };

        Assert.IsNull(cmdlet.Scope);
    }

    [TestMethod]
    public void Scope_SetToGlobal_RoundTrips()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = ["user.name"],
            Scope = GitConfigScope.Global,
        };

        Assert.AreEqual(GitConfigScope.Global, cmdlet.Scope);
    }

    [TestMethod]
    public void Scope_SetToLocal_RoundTrips()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = ["user.name"],
            Scope = GitConfigScope.Local,
        };

        Assert.AreEqual(GitConfigScope.Local, cmdlet.Scope);
    }

    [TestMethod]
    public void Scope_SetToSystem_RoundTrips()
    {
        var cmdlet = new ClearGitConfigurationCmdlet(new StubGitConfigService())
        {
            Name = ["user.name"],
            Scope = GitConfigScope.System,
        };

        Assert.AreEqual(GitConfigScope.System, cmdlet.Scope);
    }

    // ── Constructor ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Constructor_NullConfigService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ClearGitConfigurationCmdlet(null!));
    }
}
