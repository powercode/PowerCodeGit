using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Core.Services;
using System.Threading;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitBranchServiceTests
{
    private const string DefaultBranchName = "master";

    [TestMethod]
    public void GetBranches_InvalidRepositoryPath_ThrowsArgumentException()
    {
        IGitBranchService service = new GitBranchService();

        Assert.Throws<ArgumentException>(() => service.GetBranches("X:\\not-a-real-repo"));
    }

    [TestMethod]
    public void GetBranches_EmptyPath_ThrowsArgumentException()
    {
        IGitBranchService service = new GitBranchService();

        Assert.Throws<ArgumentException>(() => service.GetBranches(string.Empty));
    }

    [TestMethod]
    public void GetBranches_ReturnsAllBranches()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });

            Assert.IsGreaterThanOrEqualTo(branches.Count, 2);
            Assert.IsTrue(branches.Any(b => b.Name == DefaultBranchName));
            Assert.IsTrue(branches.Any(b => b.Name == "feature"));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_HeadBranch_IsMarkedAsHead()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });

            var headBranches = branches.Where(b => b.IsHead).ToList();
            Assert.HasCount(1, headBranches);
            Assert.AreEqual(DefaultBranchName, headBranches[0].Name);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_BranchHasTipSha()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });

            foreach (var branch in branches.Where(b => !b.IsRemote))
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(branch.TipSha));
                Assert.AreEqual(7, branch.TipShortSha.Length);
            }
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void SwitchBranch_InvalidRepositoryPath_ThrowsArgumentException()
    {
        var service = new GitBranchService();

        Assert.Throws<ArgumentException>(() => service.SwitchBranch(new GitSwitchOptions { RepositoryPath = "X:\\not-a-real-repo", BranchName = "main" }));
    }

    [TestMethod]
    public void SwitchBranch_EmptyBranchName_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

                Assert.Throws<ArgumentException>(() => service.SwitchBranch(new GitSwitchOptions { RepositoryPath = repositoryPath, BranchName = string.Empty }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void SwitchBranch_NonexistentBranch_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            Assert.Throws<ArgumentException>(() => service.SwitchBranch(new GitSwitchOptions { RepositoryPath = repositoryPath, BranchName = "nonexistent" }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void SwitchBranch_ValidBranch_SwitchesAndReturnsInfo()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var result = service.SwitchBranch(new GitSwitchOptions { RepositoryPath = repositoryPath, BranchName = "feature" });

            Assert.AreEqual("feature", result.Name);
            Assert.IsTrue(result.IsHead);
            Assert.IsFalse(result.IsRemote);

            using var repository = new Repository(repositoryPath);
            Assert.AreEqual("feature", repository.Head.FriendlyName);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void SwitchBranch_WithCreate_CreatesAndSwitchesBranch()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var result = service.SwitchBranch(new GitSwitchOptions
            {
                RepositoryPath = repositoryPath,
                BranchName = "new-branch",
                Create = true,
            });

            Assert.AreEqual("new-branch", result.Name);
            Assert.IsTrue(result.IsHead);

            using var repository = new Repository(repositoryPath);
            Assert.AreEqual("new-branch", repository.Head.FriendlyName);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void SwitchBranch_WithDetach_DetachesHeadAtCommit()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            string sha;
            using (var repository = new Repository(repositoryPath))
            {
                sha = repository.Head.Tip.Sha;
            }

            var result = service.SwitchBranch(new GitSwitchOptions
            {
                RepositoryPath = repositoryPath,
                Detach = true,
                Committish = sha,
            });

            Assert.IsTrue(result.IsHead);
            Assert.AreEqual(sha, result.TipSha);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void CreateBranch_ValidOptions_CreatesBranchAtHead()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var result = service.CreateBranch(new GitBranchCreateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "new-feature",
            });

            Assert.AreEqual("new-feature", result.Name);
            Assert.IsTrue(result.IsHead);
            Assert.IsFalse(result.IsRemote);

            using var repository = new Repository(repositoryPath);
            Assert.AreEqual("new-feature", repository.Head.FriendlyName);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void CreateBranch_ExistingBranchNoForce_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            Assert.Throws<ArgumentException>(() => service.CreateBranch(new GitBranchCreateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "feature", // already created in CreateRepositoryWithBranches
            }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void CreateBranch_ExistingBranchWithForce_OverwritesBranch()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            // Advance master with a new commit so feature is behind
            using (var repo = new Repository(repositoryPath))
            {
                var sig = new Signature("Test", "t@t.com", DateTimeOffset.UtcNow);
                var file = Path.Combine(repositoryPath, "extra.txt");
                File.WriteAllText(file, "extra");
                Commands.Stage(repo, file);
                repo.Commit("Extra commit", sig, sig);
            }

            var result = service.CreateBranch(new GitBranchCreateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "feature",
                Force = true,
            });

            Assert.AreEqual("feature", result.Name);
            Assert.IsTrue(result.IsHead);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void CreateBranch_WithStartPoint_CreatesBranchAtSpecifiedCommit()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        try
        {
            var service = new GitBranchService();
            string firstSha;

            using (var repo = new Repository(repositoryPath))
            {
                var sig = new Signature("Test", "t@t.com", DateTimeOffset.UtcNow);
                var file = Path.Combine(repositoryPath, "f1.txt");
                File.WriteAllText(file, "content1");
                Commands.Stage(repo, file);
                var firstCommit = repo.Commit("First", sig, sig);
                firstSha = firstCommit.Sha;

                File.WriteAllText(file, "content2");
                Commands.Stage(repo, file);
                repo.Commit("Second", sig, sig);
            }

            var result = service.CreateBranch(new GitBranchCreateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "at-first",
                StartPoint = firstSha,
            });

            Assert.AreEqual("at-first", result.Name);
            Assert.AreEqual(firstSha, result.TipSha);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_OptionsWithNoFilters_ReturnsAllLocalBranches()    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });

            Assert.IsGreaterThanOrEqualTo(branches.Count, 2);
            Assert.IsTrue(branches.Any(b => b.Name == DefaultBranchName));
            Assert.IsTrue(branches.Any(b => b.Name == "feature"));
            Assert.IsTrue(branches.All(b => !b.IsRemote));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_ListAll_ReturnsBothLocalAndRemoteBranches()
    {
        var repositoryPath = CreateRepositoryWithRemote();

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions
            {
                RepositoryPath = repositoryPath,
                ListAll = true,
            });

            Assert.IsTrue(branches.Any(b => !b.IsRemote), "Expected at least one local branch.");
            Assert.IsTrue(branches.Any(b => b.IsRemote), "Expected at least one remote branch.");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_ListRemote_ReturnsOnlyRemoteBranches()
    {
        var repositoryPath = CreateRepositoryWithRemote();

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions
            {
                RepositoryPath = repositoryPath,
                ListRemote = true,
            });

            Assert.IsTrue(branches.All(b => b.IsRemote));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_PatternFilter_MatchesGlob()
    {
        var repositoryPath = CreateRepositoryWithNamedBranches("feature/login", "feature/dashboard", "bugfix/crash");

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions
            {
                RepositoryPath = repositoryPath,
                Pattern = "feature/*",
            });

            Assert.HasCount(2, branches);
            Assert.IsTrue(branches.All(b => b.Name.StartsWith("feature/", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_MergedFilter_ReturnsOnlyMergedBranches()
    {
        var repositoryPath = CreateRepositoryWithMergedBranch(out var mergedBranch, out var unmergedBranch);

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions
            {
                RepositoryPath = repositoryPath,
                MergedInto = "HEAD",
            });

            Assert.IsTrue(branches.Any(b => b.Name == mergedBranch), $"Merged branch '{mergedBranch}' should appear.");
            Assert.IsFalse(branches.Any(b => b.Name == unmergedBranch), $"Unmerged branch '{unmergedBranch}' should not appear.");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetBranches_NotMergedFilter_ReturnsOnlyUnmergedBranches()
    {
        var repositoryPath = CreateRepositoryWithMergedBranch(out var mergedBranch, out var unmergedBranch);

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(new GitBranchListOptions
            {
                RepositoryPath = repositoryPath,
                NotMergedInto = "HEAD",
            });

            Assert.IsFalse(branches.Any(b => b.Name == mergedBranch), $"Merged branch '{mergedBranch}' should not appear.");
            Assert.IsTrue(branches.Any(b => b.Name == unmergedBranch), $"Unmerged branch '{unmergedBranch}' should appear.");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void DeleteBranch_ValidOptions_DeletesBranch()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            service.DeleteBranch(new GitBranchDeleteOptions
            {
                RepositoryPath = repositoryPath,
                Name = "feature",
            });

            var branches = service.GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });
            Assert.IsFalse(branches.Any(b => b.Name == "feature"));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void DeleteBranch_UnmergedBranchNoForce_ThrowsInvalidOperationException()
    {
        var repositoryPath = CreateRepositoryWithUnmergedBranch();

        try
        {
            var service = new GitBranchService();

            Assert.Throws<InvalidOperationException>(() => service.DeleteBranch(new GitBranchDeleteOptions
            {
                RepositoryPath = repositoryPath,
                Name = "unmerged",
            }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void DeleteBranch_UnmergedBranchWithForce_DeletesBranch()
    {
        var repositoryPath = CreateRepositoryWithUnmergedBranch();

        try
        {
            var service = new GitBranchService();

            service.DeleteBranch(new GitBranchDeleteOptions
            {
                RepositoryPath = repositoryPath,
                Name = "unmerged",
                Force = true,
            });

            var branches = service.GetBranches(new GitBranchListOptions { RepositoryPath = repositoryPath });
            Assert.IsFalse(branches.Any(b => b.Name == "unmerged"));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    private static string CreateRepositoryWithUnmergedBranch()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("Test", "t@t.com", DateTimeOffset.UtcNow);
        var file = Path.Combine(repositoryPath, "f.txt");
        File.WriteAllText(file, "content");
        Commands.Stage(repository, file);
        repository.Commit("Initial", sig, sig);

        var branch = repository.CreateBranch("unmerged");
        Commands.Checkout(repository, branch);
        File.WriteAllText(file, "changed");
        Commands.Stage(repository, file);
        repository.Commit("Unmerged commit", sig, sig);

        Commands.Checkout(repository, repository.Branches[DefaultBranchName]!);
        return repositoryPath;
    }

    private static string CreateRepositoryWithRemote()
    {
        // Create a "remote" repo and a clone of it so remote-tracking refs exist
        var remotePath = CreateTemporaryDirectory();
        var localPath = CreateTemporaryDirectory();
        Repository.Init(remotePath);

        using (var remote = new Repository(remotePath))
        {
            var sig = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);
            var filePath = Path.Combine(remotePath, "file.txt");
            File.WriteAllText(filePath, "content");
            Commands.Stage(remote, filePath);
            remote.Commit("Initial commit", sig, sig);
        }

        Repository.Clone(remotePath, localPath);
        return localPath;
    }

    private static string CreateRepositoryWithNamedBranches(params string[] branchNames)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);
        var filePath = Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "initial content");
        Commands.Stage(repository, filePath);
        repository.Commit("Initial commit", sig, sig);

        foreach (var branchName in branchNames)
        {
            repository.CreateBranch(branchName);
        }

        return repositoryPath;
    }

    private static string CreateRepositoryWithMergedBranch(out string mergedBranchName, out string unmergedBranchName)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);

        // Initial commit on master/main
        var filePath = Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "initial");
        Commands.Stage(repository, filePath);
        repository.Commit("Initial commit", sig, sig);

        // Create 'merged-branch', add a commit, then merge it back
        var toMerge = repository.CreateBranch("merged-branch");
        Commands.Checkout(repository, toMerge);
        File.WriteAllText(filePath, "merged change");
        Commands.Stage(repository, filePath);
        repository.Commit("Merged feature commit", sig, sig);

        Commands.Checkout(repository, repository.Branches[DefaultBranchName]!);
        repository.Merge(toMerge, sig, new MergeOptions { FastForwardStrategy = FastForwardStrategy.FastForwardOnly });

        // Create 'unmerged-branch' with its own ahead commit
        var ahead = repository.CreateBranch("unmerged-branch");
        Commands.Checkout(repository, ahead);
        File.WriteAllText(filePath, "unmerged change");
        Commands.Stage(repository, filePath);
        repository.Commit("Unmerged feature commit", sig, sig);

        // Go back to default branch (so HEAD is not unmerged-branch)
        Commands.Checkout(repository, repository.Branches[DefaultBranchName]!);

        mergedBranchName = "merged-branch";
        unmergedBranchName = "unmerged-branch";
        return repositoryPath;
    }

    private static string CreateRepositoryWithBranches()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var signature = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);

        var filePath = Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "initial content");
        Commands.Stage(repository, filePath);
        repository.Commit("Initial commit", signature, signature);

        repository.CreateBranch("feature");

        return repositoryPath;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "PowerCode.GitTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
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
                ClearReadOnlyAttributes(path);
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(100);
            }
        }

        ClearReadOnlyAttributes(path);
        Directory.Delete(path, recursive: true);
    }

    private static void ClearReadOnlyAttributes(string directoryPath)
    {
        var directoryInfo = new DirectoryInfo(directoryPath);

        foreach (var fileInfo in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            fileInfo.Attributes = FileAttributes.Normal;
        }

        foreach (var subDirectoryInfo in directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
        {
            subDirectoryInfo.Attributes = FileAttributes.Normal;
        }

        directoryInfo.Attributes = FileAttributes.Normal;
    }
}
