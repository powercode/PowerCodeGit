using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Core.Services;
using System.Threading;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitWorktreeServiceTests
{
    [TestMethod]
    public void GetWorktrees_InvalidRepositoryPath_ThrowsArgumentException()
    {
        IGitWorktreeService service = new GitWorktreeService();

        Assert.Throws<ArgumentException>(() => service.GetWorktrees("X:\\not-a-real-repo"));
    }

    [TestMethod]
    public void GetWorktrees_EmptyPath_ThrowsArgumentException()
    {
        IGitWorktreeService service = new GitWorktreeService();

        Assert.Throws<ArgumentException>(() => service.GetWorktrees(string.Empty));
    }

    [TestMethod]
    public void GetWorktrees_NoWorktrees_ReturnsEmptyList()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            IGitWorktreeService service = new GitWorktreeService();

            var worktrees = service.GetWorktrees(repositoryPath);

            Assert.HasCount(0, worktrees);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void AddWorktree_ValidArgs_CreatesWorktree()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            var service = new GitWorktreeService();

            var result = service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "test-wt",
                Path = worktreePath,
            });

            Assert.AreEqual("test-wt", result.Name);
            Assert.IsFalse(result.IsLocked);
            Assert.IsNull(result.LockReason);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void AddWorktree_WithBranch_CreatesWorktreeAtBranch()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            using (var repository = new Repository(repositoryPath))
            {
                repository.CreateBranch("feature");
            }

            var service = new GitWorktreeService();

            var result = service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "feature-wt",
                Path = worktreePath,
                Branch = "feature",
            });

            Assert.AreEqual("feature-wt", result.Name);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void AddWorktree_BranchEqualToName_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            var service = new GitWorktreeService();

            Assert.Throws<ArgumentException>(() => service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "main",
                Path = worktreePath,
                Branch = "main",
            }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void AddWorktree_Locked_CreatesLockedWorktree()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            var service = new GitWorktreeService();

            var result = service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "locked-wt",
                Path = worktreePath,
                Locked = true,
            });

            Assert.AreEqual("locked-wt", result.Name);
            Assert.IsTrue(result.IsLocked);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void GetWorktrees_AfterAdd_ReturnsAddedWorktree()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            IGitWorktreeService service = new GitWorktreeService();
            service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "my-wt",
                Path = worktreePath,
            });

            var worktrees = service.GetWorktrees(repositoryPath);

            Assert.HasCount(1, worktrees);
            Assert.AreEqual("my-wt", worktrees[0].Name);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void RemoveWorktree_ExistingWorktree_PrunesWorktree()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            IGitWorktreeService service = new GitWorktreeService();
            service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "to-remove",
                Path = worktreePath,
            });

            service.RemoveWorktree(new GitWorktreeRemoveOptions
            {
                RepositoryPath = repositoryPath,
                Name = "to-remove",
            });

            var worktrees = service.GetWorktrees(repositoryPath);
            Assert.HasCount(0, worktrees);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void RemoveWorktree_NonExistent_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorktreeService();

            Assert.Throws<ArgumentException>(() =>
                service.RemoveWorktree(new GitWorktreeRemoveOptions
                {
                    RepositoryPath = repositoryPath,
                    Name = "does-not-exist",
                }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void RemoveWorktree_LockedWorktreeWithForce_PrunesWorktree()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            IGitWorktreeService service = new GitWorktreeService();
            service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "locked-remove",
                Path = worktreePath,
                Locked = true,
            });

            service.RemoveWorktree(new GitWorktreeRemoveOptions
            {
                RepositoryPath = repositoryPath,
                Name = "locked-remove",
                Force = true,
            });

            var worktrees = service.GetWorktrees(repositoryPath);
            Assert.HasCount(0, worktrees);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void LockWorktree_UnlockedWorktree_Locks()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            IGitWorktreeService service = new GitWorktreeService();
            service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "lock-test",
                Path = worktreePath,
            });

            service.LockWorktree(new GitWorktreeLockOptions
            {
                RepositoryPath = repositoryPath,
                Name = "lock-test",
                Reason = "Testing lock",
            });

            var worktrees = service.GetWorktrees(repositoryPath);
            var locked = worktrees.Single(w => w.Name == "lock-test");
            Assert.IsTrue(locked.IsLocked);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void LockWorktree_NonExistent_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorktreeService();

            Assert.Throws<ArgumentException>(() =>
                service.LockWorktree(new GitWorktreeLockOptions
                {
                    RepositoryPath = repositoryPath,
                    Name = "does-not-exist",
                }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void UnlockWorktree_LockedWorktree_Unlocks()
    {
        var repositoryPath = CreateRepositoryWithCommit();
        var worktreePath = GenerateTemporaryPath();

        try
        {
            IGitWorktreeService service = new GitWorktreeService();
            service.AddWorktree(new GitWorktreeAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "unlock-test",
                Path = worktreePath,
                Locked = true,
            });

            service.UnlockWorktree(new GitWorktreeUnlockOptions
            {
                RepositoryPath = repositoryPath,
                Name = "unlock-test",
            });

            var worktrees = service.GetWorktrees(repositoryPath);
            var unlocked = worktrees.Single(w => w.Name == "unlock-test");
            Assert.IsFalse(unlocked.IsLocked);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
            DeleteDirectory(worktreePath);
        }
    }

    [TestMethod]
    public void UnlockWorktree_NonExistent_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorktreeService();

            Assert.Throws<ArgumentException>(() =>
                service.UnlockWorktree(new GitWorktreeUnlockOptions
                {
                    RepositoryPath = repositoryPath,
                    Name = "does-not-exist",
                }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    private static string CreateRepositoryWithCommit()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var signature = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);

        var filePath = Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "initial content");
        Commands.Stage(repository, filePath);
        repository.Commit("Initial commit", signature, signature);

        return repositoryPath;
    }

    private static string GenerateTemporaryPath()
    {
        return Path.Combine(Path.GetTempPath(), "PowerCode.GitTests", Guid.NewGuid().ToString("N"));
    }

    private static string CreateTemporaryDirectory()
    {
        var path = GenerateTemporaryPath();
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
