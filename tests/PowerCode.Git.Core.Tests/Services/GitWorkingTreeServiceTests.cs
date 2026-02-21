using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;
using System.Threading;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitWorkingTreeServiceTests
{
    [TestMethod]
    public void GetStatus_InvalidRepositoryPath_ThrowsArgumentException()
    {
        var service = new GitWorkingTreeService();

        Assert.Throws<ArgumentException>(() => service.GetStatus(new GitStatusOptions { RepositoryPath = "X:\\not-a-real-repo" }));
    }

    [TestMethod]
    public void GetStatus_EmptyPath_ThrowsArgumentException()
    {
        var service = new GitWorkingTreeService();

        Assert.Throws<ArgumentException>(() => service.GetStatus(new GitStatusOptions { RepositoryPath = string.Empty }));
    }

    [TestMethod]
    public void GetStatus_CleanRepo_ReturnsZeroCounts()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorkingTreeService();

            var result = service.GetStatus(new GitStatusOptions { RepositoryPath = repositoryPath });

            Assert.AreEqual(0, result.StagedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.AreEqual(0, result.UntrackedCount);
            Assert.HasCount(0, result.Entries);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetStatus_WithUntrackedFile_ReportsUntracked()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            File.WriteAllText(Path.Combine(repositoryPath, "untracked.txt"), "new file");

            var service = new GitWorkingTreeService();
            var result = service.GetStatus(new GitStatusOptions { RepositoryPath = repositoryPath });

            Assert.AreEqual(1, result.UntrackedCount);
            Assert.AreEqual(0, result.StagedCount);
            Assert.AreEqual(0, result.ModifiedCount);
            Assert.IsTrue(result.Entries.Any(e =>
                e.Status == GitFileStatus.Untracked &&
                e.FilePath == "untracked.txt"));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetStatus_WithStagedChange_ReportsStaged()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using var repository = new Repository(repositoryPath);
            var filePath = Path.Combine(repositoryPath, "staged.txt");
            File.WriteAllText(filePath, "staged content");
            Commands.Stage(repository, filePath);

            var service = new GitWorkingTreeService();
            var result = service.GetStatus(new GitStatusOptions { RepositoryPath = repositoryPath });

            Assert.AreEqual(1, result.StagedCount);
            Assert.IsTrue(result.Entries.Any(e =>
                e.StagingState == GitStagingState.Staged &&
                e.Status == GitFileStatus.Added));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetStatus_WithModifiedFile_ReportsModified()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            File.WriteAllText(Path.Combine(repositoryPath, "file-0.txt"), "modified content");

            var service = new GitWorkingTreeService();
            var result = service.GetStatus(new GitStatusOptions { RepositoryPath = repositoryPath });

            Assert.AreEqual(1, result.ModifiedCount);
            Assert.IsTrue(result.Entries.Any(e =>
                e.StagingState == GitStagingState.Unstaged &&
                e.Status == GitFileStatus.Modified));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetStatus_ReturnsCurrentBranch()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorkingTreeService();
            var result = service.GetStatus(new GitStatusOptions { RepositoryPath = repositoryPath });

            Assert.IsFalse(string.IsNullOrWhiteSpace(result.CurrentBranch));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetStatus_WithPaths_FiltersEntriesToMatchingPaths()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorkingTreeService();
            // Create two untracked files in different locations
            File.WriteAllText(System.IO.Path.Combine(repositoryPath, "match.txt"), "a");
            File.WriteAllText(System.IO.Path.Combine(repositoryPath, "other.txt"), "b");

            var result = service.GetStatus(new GitStatusOptions
            {
                RepositoryPath = repositoryPath,
                Paths = ["match.txt"],
            });

            Assert.IsTrue(result.Entries.Any(e => e.FilePath == "match.txt"));
            Assert.IsFalse(result.Entries.Any(e => e.FilePath == "other.txt"));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetStatus_NoUntrackedFiles_ExcludesUntracked()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorkingTreeService();
            File.WriteAllText(System.IO.Path.Combine(repositoryPath, "untracked.txt"), "x");

            var result = service.GetStatus(new GitStatusOptions
            {
                RepositoryPath = repositoryPath,
                UntrackedFilesMode = GitUntrackedFilesMode.No,
            });

            Assert.IsFalse(result.Entries.Any(e => e.Status == GitFileStatus.Untracked));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetDiff_InvalidRepositoryPath_ThrowsArgumentException()
    {
        var service = new GitWorkingTreeService();
        var options = new GitDiffOptions { RepositoryPath = "X:\\not-a-real-repo" };

        Assert.Throws<ArgumentException>(() => service.GetDiff(options));
    }

    [TestMethod]
    public void GetDiff_NullOptions_ThrowsArgumentNullException()
    {
        var service = new GitWorkingTreeService();

        Assert.Throws<ArgumentNullException>(() => service.GetDiff(null!));
    }

    [TestMethod]
    public void GetDiff_NoChanges_ReturnsEmptyList()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitWorkingTreeService();
            var options = new GitDiffOptions { RepositoryPath = repositoryPath };

            var entries = service.GetDiff(options);

            Assert.HasCount(0, entries);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetDiff_UnstagedChanges_ReturnsDiffEntries()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            File.WriteAllText(Path.Combine(repositoryPath, "file-0.txt"), "modified content");

            var service = new GitWorkingTreeService();
            var options = new GitDiffOptions { RepositoryPath = repositoryPath };

            var entries = service.GetDiff(options);

            Assert.HasCount(1, entries);
            Assert.AreEqual(GitFileStatus.Modified, entries[0].Status);
            Assert.IsTrue(entries[0].LinesAdded > 0 || entries[0].LinesDeleted > 0);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetDiff_StagedChanges_ReturnsStagedDiff()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using var repository = new Repository(repositoryPath);
            var filePath = Path.Combine(repositoryPath, "new-staged.txt");
            File.WriteAllText(filePath, "staged content");
            Commands.Stage(repository, filePath);

            var service = new GitWorkingTreeService();
            var options = new GitDiffOptions { RepositoryPath = repositoryPath, Staged = true };

            var entries = service.GetDiff(options);

            Assert.HasCount(1, entries);
            Assert.AreEqual(GitFileStatus.Added, entries[0].Status);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetDiff_PatchIsPopulated()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            File.WriteAllText(Path.Combine(repositoryPath, "file-0.txt"), "modified content");

            var service = new GitWorkingTreeService();
            var options = new GitDiffOptions { RepositoryPath = repositoryPath };

            var entries = service.GetDiff(options);

            Assert.HasCount(1, entries);
            Assert.IsFalse(string.IsNullOrWhiteSpace(entries[0].Patch));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetDiff_CommitMode_DiffsWorkingTreeAgainstCommit()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using var repository = new Repository(repositoryPath);
            var sha = repository.Head.Tip.Sha;

            // Modify file after last commit
            File.WriteAllText(Path.Combine(repositoryPath, "file-0.txt"), "changed after initial");

            var service = new GitWorkingTreeService();
            var entries = service.GetDiff(new GitDiffOptions
            {
                RepositoryPath = repositoryPath,
                Commit = sha,
            });

            Assert.IsTrue(entries.Count > 0);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetDiff_RangeMode_DiffsBetweenCommits()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using var repository = new Repository(repositoryPath);
            var firstSha = repository.Head.Tip.Sha;

            // Make a second commit
            var sig = new Signature("Test", "t@t.com", DateTimeOffset.UtcNow);
            File.WriteAllText(Path.Combine(repositoryPath, "file-0.txt"), "second commit content");
            Commands.Stage(repository, Path.Combine(repositoryPath, "file-0.txt"));
            repository.Commit("Second commit", sig, sig);
            var secondSha = repository.Head.Tip.Sha;

            var service = new GitWorkingTreeService();
            var entries = service.GetDiff(new GitDiffOptions
            {
                RepositoryPath = repositoryPath,
                FromCommit = firstSha,
                ToCommit = secondSha,
            });

            Assert.IsTrue(entries.Count > 0);
            Assert.AreEqual(GitFileStatus.Modified, entries[0].Status);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Stage_WithUpdate_StagesOnlyTrackedFiles()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using var repository = new Repository(repositoryPath);

            // Modify tracked file
            var trackedFile = Path.Combine(repositoryPath, "file-0.txt");
            File.WriteAllText(trackedFile, "modified content");

            // Add untracked file (should NOT be staged by Update)
            var untrackedFile = Path.Combine(repositoryPath, "untracked.txt");
            File.WriteAllText(untrackedFile, "new file");

            var service = new GitWorkingTreeService();
            service.Stage(new GitStageOptions { RepositoryPath = repositoryPath, Update = true });

            var result = service.GetStatus(new GitStatusOptions { RepositoryPath = repositoryPath });

            // The tracked modified file should be staged
            Assert.IsTrue(result.StagedCount >= 1);

            // The untracked file should still be untracked (not staged)
            Assert.IsTrue(result.UntrackedCount >= 1);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Stage_WithForce_StagesIgnoredFiles()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using var repository = new Repository(repositoryPath);

            // Create a .gitignore and an ignored file
            var gitignorePath = Path.Combine(repositoryPath, ".gitignore");
            File.WriteAllText(gitignorePath, "ignored.txt\n");
            Commands.Stage(repository, gitignorePath);
            var sig = new Signature("Test", "t@t.com", DateTimeOffset.UtcNow);
            repository.Commit("Add .gitignore", sig, sig);

            var ignoredFile = Path.Combine(repositoryPath, "ignored.txt");
            File.WriteAllText(ignoredFile, "this is ignored");

            var service = new GitWorkingTreeService();
            service.Stage(new GitStageOptions
            {
                RepositoryPath = repositoryPath,
                Paths = ["ignored.txt"],
                Force = true,
            });

            var result = service.GetStatus(new GitStatusOptions { RepositoryPath = repositoryPath });

            // The forced-staged ignored file should be staged
            Assert.IsTrue(result.StagedCount >= 1);
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
        var filePath = Path.Combine(repositoryPath, "file-0.txt");
        File.WriteAllText(filePath, "initial content");
        Commands.Stage(repository, filePath);

        var signature = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);
        repository.Commit("Initial commit", signature, signature);

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
