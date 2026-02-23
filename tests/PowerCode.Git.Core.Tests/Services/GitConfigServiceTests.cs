using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitConfigServiceTests
{
    // Use a valid repo path so the service can be constructed, but the stub
    // prevents any real git processes from running.
    private const string RepoPath = "C:\\repo";

    // ── GetConfigEntries ─────────────────────────────────────────────────────

    [TestMethod]
    public void GetConfigEntries_NoScope_PassesListFlag()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, "user.name=Jane\ncore.autocrlf=true\n", string.Empty),
        };
        var service = new GitConfigService(stub);

        var entries = service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath });

        Assert.HasCount(2, entries);
        Assert.AreEqual("user.name", entries[0].Name);
        Assert.AreEqual("Jane", entries[0].Value);
        Assert.AreEqual("core.autocrlf", entries[1].Name);
        Assert.AreEqual("true", entries[1].Value);

        var args = stub.Invocations[0].Args;
        Assert.AreEqual("config", args[0]);
        Assert.AreEqual("--list", args[1]);
    }

    [TestMethod]
    public void GetConfigEntries_WithScope_IncludesScopeFlag()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, "user.name=Jane\n", string.Empty),
        };
        var service = new GitConfigService(stub);

        service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath, Scope = GitConfigScope.Global });

        var args = stub.Invocations[0].Args;
        Assert.IsTrue(args.Contains("--global"));
    }

    [TestMethod]
    public void GetConfigEntries_LocalScope_PassesLocalFlag()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, string.Empty, string.Empty),
        };
        var service = new GitConfigService(stub);

        service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath, Scope = GitConfigScope.Local });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--local"));
    }

    [TestMethod]
    public void GetConfigEntries_SystemScope_PassesSystemFlag()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, string.Empty, string.Empty),
        };
        var service = new GitConfigService(stub);

        service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath, Scope = GitConfigScope.System });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--system"));
    }

    [TestMethod]
    public void GetConfigEntries_WorktreeScope_PassesWorktreeFlag()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, string.Empty, string.Empty),
        };
        var service = new GitConfigService(stub);

        service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath, Scope = GitConfigScope.Worktree });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--worktree"));
    }

    [TestMethod]
    public void GetConfigEntries_ShowScope_ParsesScopeFromOutput()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, "local\tuser.name=Jane\nglobal\tcore.editor=vim\n", string.Empty),
        };
        var service = new GitConfigService(stub);

        var entries = service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath, ShowScope = true });

        Assert.HasCount(2, entries);
        Assert.AreEqual(GitConfigScope.Local, entries[0].Scope);
        Assert.AreEqual("user.name", entries[0].Name);
        Assert.AreEqual("Jane", entries[0].Value);
        Assert.AreEqual(GitConfigScope.Global, entries[1].Scope);
        Assert.AreEqual("core.editor", entries[1].Name);
        Assert.AreEqual("vim", entries[1].Value);
    }

    [TestMethod]
    public void GetConfigEntries_NonZeroExit_ReturnsEmpty()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(1, string.Empty, "error"),
        };
        var service = new GitConfigService(stub);

        var entries = service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath });

        Assert.IsEmpty(entries);
    }

    [TestMethod]
    public void GetConfigEntries_ValueContainsEquals_ParsesCorrectly()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, "url.ssh://git@github.com/.insteadOf=https://github.com/\n", string.Empty),
        };
        var service = new GitConfigService(stub);

        var entries = service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = RepoPath });

        Assert.HasCount(1, entries);
        Assert.AreEqual("url.ssh://git@github.com/.insteadOf", entries[0].Name);
        Assert.AreEqual("https://github.com/", entries[0].Value);
    }

    // ── GetConfigValue ───────────────────────────────────────────────────────

    [TestMethod]
    public void GetConfigValue_Found_ReturnsEntry()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, "Jane Doe\n", string.Empty),
        };
        var service = new GitConfigService(stub);

        var entry = service.GetConfigValue(new GitConfigGetOptions { RepositoryPath = RepoPath, Name = "user.name" });

        Assert.IsNotNull(entry);
        Assert.AreEqual("user.name", entry.Name);
        Assert.AreEqual("Jane Doe", entry.Value);
    }

    [TestMethod]
    public void GetConfigValue_NotFound_ReturnsNull()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(1, string.Empty, string.Empty),
        };
        var service = new GitConfigService(stub);

        var entry = service.GetConfigValue(new GitConfigGetOptions { RepositoryPath = RepoPath, Name = "nonexistent.key" });

        Assert.IsNull(entry);
    }

    [TestMethod]
    public void GetConfigValue_PassesGetFlag()
    {
        var stub = new StubGitExecutable
        {
            ResultToReturn = new GitProcessResult(0, "value\n", string.Empty),
        };
        var service = new GitConfigService(stub);

        service.GetConfigValue(new GitConfigGetOptions { RepositoryPath = RepoPath, Name = "user.email" });

        var args = stub.Invocations[0].Args;
        Assert.IsTrue(args.Contains("--get"));
        Assert.IsTrue(args.Contains("user.email"));
    }

    [TestMethod]
    public void GetConfigValue_NameIsNull_Throws()
    {
        var stub = new StubGitExecutable();
        var service = new GitConfigService(stub);

        Assert.Throws<ArgumentException>(() =>
            service.GetConfigValue(new GitConfigGetOptions { RepositoryPath = RepoPath }));
    }

    // ── SetConfigValue ───────────────────────────────────────────────────────

    [TestMethod]
    public void SetConfigValue_PassesNameAndValue()
    {
        var stub = new StubGitExecutable();
        var service = new GitConfigService(stub);

        service.SetConfigValue(new GitConfigSetOptions
        {
            RepositoryPath = RepoPath,
            Name = "user.name",
            Value = "Jane Doe",
        });

        var args = stub.Invocations[0].Args;
        Assert.AreEqual("config", args[0]);
        Assert.AreEqual("user.name", args[1]);
        Assert.AreEqual("Jane Doe", args[2]);
    }

    [TestMethod]
    public void SetConfigValue_WithScope_IncludesScopeFlag()
    {
        var stub = new StubGitExecutable();
        var service = new GitConfigService(stub);

        service.SetConfigValue(new GitConfigSetOptions
        {
            RepositoryPath = RepoPath,
            Name = "user.name",
            Value = "Jane Doe",
            Scope = GitConfigScope.Global,
        });

        var args = stub.Invocations[0].Args;
        Assert.IsTrue(args.Contains("--global"));
    }

    [TestMethod]
    public void SetConfigValue_UsesCorrectWorkingDirectory()
    {
        var stub = new StubGitExecutable();
        var service = new GitConfigService(stub);

        service.SetConfigValue(new GitConfigSetOptions
        {
            RepositoryPath = "D:\\my-repo",
            Name = "user.name",
            Value = "Jane",
        });

        Assert.AreEqual("D:\\my-repo", stub.Invocations[0].WorkingDirectory);
    }

    [TestMethod]
    public void SetConfigValue_EmptyName_Throws()
    {
        var stub = new StubGitExecutable();
        var service = new GitConfigService(stub);

        Assert.Throws<ArgumentException>(() =>
            service.SetConfigValue(new GitConfigSetOptions
            {
                RepositoryPath = RepoPath,
                Name = "",
                Value = "value",
            }));
    }
}
