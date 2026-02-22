using System.IO;
using System.Threading;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitRebaseServiceTests
{
    private static string repoPath = string.Empty;

    [ClassInitialize]
    public static void ClassInitialize(TestContext _)
    {
        repoPath = CreateTemporaryRepository();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        DeleteDirectory(repoPath);
    }

    // ── Start — non-interactive arg building ─────────────────────────────────

    [TestMethod]
    public void Start_NonInteractive_PassesRebaseAndUpstream()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main" });

        var args = stub.Invocations[0].Args;
        Assert.AreEqual("rebase", args[0]);
        Assert.AreEqual("main", args[^1]);
    }

    [TestMethod]
    public void Start_NonInteractive_WithAutoSquash_IncludesFlag()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", AutoSquash = true });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--autosquash"));
    }

    [TestMethod]
    public void Start_NonInteractive_WithExec_IncludesFlagAndCommand()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", Exec = "dotnet test" });

        var args = stub.Invocations[0].Args.ToList();
        CollectionAssert.Contains(args, "--exec");
        Assert.AreEqual("dotnet test", args[args.IndexOf("--exec") + 1]);
    }

    [TestMethod]
    public void Start_NonInteractive_WithRebaseMerges_IncludesFlag()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", RebaseMerges = true });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--rebase-merges"));
    }

    [TestMethod]
    public void Start_NonInteractive_WithUpdateRefs_IncludesFlag()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", UpdateRefs = true });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--update-refs"));
    }

    [TestMethod]
    public void Start_NonInteractive_WithAllNewFlags_IncludesAllFlags()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions
        {
            RepositoryPath = repoPath,
            Upstream = "main",
            AutoSquash = true,
            Exec = "make check",
            RebaseMerges = true,
            UpdateRefs = true,
        });

        var args = stub.Invocations[0].Args;
        Assert.IsTrue(args.Contains("--autosquash"));
        Assert.IsTrue(args.Contains("--exec"));
        Assert.IsTrue(args.Contains("make check"));
        Assert.IsTrue(args.Contains("--rebase-merges"));
        Assert.IsTrue(args.Contains("--update-refs"));
    }

    // ── Start — interactive arg building ────────────────────────────────────

    [TestMethod]
    public void Start_Interactive_BeginsWithRebaseDashI()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", Interactive = true });

        var args = stub.Invocations[0].Args;
        Assert.AreEqual("rebase", args[0]);
        Assert.AreEqual("-i", args[1]);
    }

    [TestMethod]
    public void Start_Interactive_WithAutoSquash_IncludesFlag()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", Interactive = true, AutoSquash = true });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--autosquash"));
    }

    [TestMethod]
    public void Start_Interactive_WithExec_IncludesFlagAndCommand()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", Interactive = true, Exec = "dotnet test" });

        var args = stub.Invocations[0].Args.ToList();
        CollectionAssert.Contains(args, "--exec");
        Assert.AreEqual("dotnet test", args[args.IndexOf("--exec") + 1]);
    }

    [TestMethod]
    public void Start_Interactive_WithRebaseMerges_IncludesFlag()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", Interactive = true, RebaseMerges = true });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--rebase-merges"));
    }

    [TestMethod]
    public void Start_Interactive_WithUpdateRefs_IncludesFlag()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions { RepositoryPath = repoPath, Upstream = "main", Interactive = true, UpdateRefs = true });

        Assert.IsTrue(stub.Invocations[0].Args.Contains("--update-refs"));
    }

    [TestMethod]
    public void Start_Interactive_UpstreamIsLastArg()
    {
        var stub = new StubGitExecutable();
        var service = new GitRebaseService(stub);

        service.Start(new GitRebaseOptions
        {
            RepositoryPath = repoPath,
            Upstream = "main",
            Interactive = true,
            AutoSquash = true,
            Exec = "make test",
            RebaseMerges = true,
            UpdateRefs = true,
        });

        Assert.AreEqual("main", stub.Invocations[0].Args[^1]);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string CreateTemporaryRepository()
    {
        var path = Path.Combine(Path.GetTempPath(), "PowerCode.GitTests", System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        Repository.Init(path);

        using var repository = new Repository(path);
        var sig = new Signature("Test", "test@example.com", System.DateTimeOffset.UtcNow);
        var filePath = Path.Combine(path, "file.txt");
        File.WriteAllText(filePath, "initial");
        Commands.Stage(repository, filePath);
        repository.Commit("Initial commit", sig, sig);

        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                foreach (var file in new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = System.IO.FileAttributes.Normal;
                }

                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
        }
    }
}
