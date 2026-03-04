using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;
using System.Linq;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitHistoryRewriteServiceTests
{
    // ─── Validation tests ───────────────────────────────────────────────────────

    [TestMethod]
    public void Rewrite_InvalidRepositoryPath_ThrowsArgumentException()
    {
        var service = new GitHistoryRewriteService();
        var options = new GitHistoryRewriteOptions
        {
            RepositoryPath = @"X:\not-a-real-repo",
        };

        Assert.Throws<ArgumentException>(() => service.Rewrite(options, commitFilter: _ => null));
    }

    [TestMethod]
    public void Rewrite_NoFilter_ThrowsArgumentException()
    {
        var service = new GitHistoryRewriteService();
        var repositoryPath = CreateRepositoryWithCommits(1);

        try
        {
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
            };

            Assert.Throws<ArgumentException>(() => service.Rewrite(options));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ─── CommitFilter tests ─────────────────────────────────────────────────────

    [TestMethod]
    public void Rewrite_CommitFilter_ReturningNull_KeepsCommitUnchanged()
    {
        var repositoryPath = CreateRepositoryWithCommits(2);

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
                BackupRefsNamespace = "refs/original/",
            };

            // CommitFilter returns null → no changes → empty result list.
            var results = service.Rewrite(options, commitFilter: _ => null);

            Assert.HasCount(0, results);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Rewrite_CommitFilter_RewritesAuthorEmail()
    {
        const string OldEmail = "old@example.com";
        const string NewEmail = "new@example.com";

        var repositoryPath = CreateRepositoryWithCommits(2, authorEmail: OldEmail);

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
                BackupRefsNamespace = "refs/original/",
            };

            var results = service.Rewrite(options, commitFilter: commit =>
            {
                // In the Core layer, the object is a LibGit2Sharp.Commit — safe to cast directly.
                var c = (Commit)commit;
                return c.Author.Email == OldEmail
                    ? new GitCommitRewriteOverrides { AuthorEmail = NewEmail, CommitterEmail = NewEmail }
                    : null;
            });

            // Both commits should have been rewritten.
            Assert.HasCount(2, results);
            Assert.IsTrue(results.All(r => r.HeaderModified));

            // Verify the new commits have the updated author email in the repository.
            using var repository = new Repository(repositoryPath);
            foreach (var commit in repository.Head.Commits)
            {
                Assert.AreEqual(NewEmail, commit.Author.Email,
                    $"Commit '{commit.MessageShort}' still has old author email.");
            }
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Rewrite_CommitFilter_RewritesMessage()
    {
        var repositoryPath = CreateRepositoryWithCommits(1);

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
            };

            var results = service.Rewrite(options, commitFilter: _ =>
                new GitCommitRewriteOverrides { Message = "Rewritten message\n" });

            Assert.HasCount(1, results);
            Assert.IsTrue(results[0].MessageModified);

            using var repository = new Repository(repositoryPath);
            Assert.AreEqual("Rewritten message\n", repository.Head.Tip.Message);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ─── TreeFilter tests ───────────────────────────────────────────────────────

    [TestMethod]
    public void Rewrite_TreeFilter_RemovesMatchingFiles()
    {
        var repositoryPath = CreateRepositoryWithFiles(new[] { "keep.txt", "remove.exe" });

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
                PruneEmptyCommits = false,
            };

            // Keep everything except .exe files.
            var results = service.Rewrite(options, treeFilter: entry =>
            {
                // In the Core layer, the tree entry context is a TreeEntryContext — cast directly.
                var ctx = (TreeEntryContext)entry;
                return !ctx.Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            });

            Assert.IsTrue(results.Any(r => r.TreeModified),
                "Expected at least one commit to report TreeModified.");

            // Verify the .exe is gone from HEAD.
            using var repository = new Repository(repositoryPath);
            Assert.IsNull(repository.Head.Tip.Tree["remove.exe"],
                "remove.exe should have been removed from HEAD.");
            Assert.IsNotNull(repository.Head.Tip.Tree["keep.txt"],
                "keep.txt should remain in HEAD.");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Rewrite_TreeFilter_WithPruneEmptyCommits_RemovesEntirelyEmptyCommit()
    {
        // Creates one commit that only adds a .exe — after filtering it will be empty.
        var repositoryPath = CreateRepositoryWithTwoSeparateCommits("only-binary.exe", "legit.txt");

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
                PruneEmptyCommits = true,
            };

            service.Rewrite(options, treeFilter: entry =>
            {
                var ctx = (TreeEntryContext)entry;
                return !ctx.Path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            });

            // The commit that only added the .exe should have been pruned.
            using var repository = new Repository(repositoryPath);
            var allMessages = repository.Head.Commits
                .Select(c => c.MessageShort)
                .ToList();

            Assert.IsFalse(allMessages.Any(m => m.Contains("only-binary")),
                $"The commit that only added the .exe file should have been pruned. Found: {string.Join(", ", allMessages)}");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ─── TagNameRewriter tests ──────────────────────────────────────────────────

    [TestMethod]
    public void Rewrite_TagNameRewriter_RenamesTags()
    {
        var repositoryPath = CreateRepositoryWithTag("v1.0");

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
            };

            service.Rewrite(options,
                commitFilter: _ => null,
                tagNameRewriter: (oldName, _, _) =>
                    oldName.StartsWith("v", StringComparison.Ordinal)
                        ? "release/" + oldName[1..]
                        : null);

            using var repository = new Repository(repositoryPath);
            var tagNames = repository.Tags.Select(t => t.FriendlyName).ToList();

            Assert.Contains("release/1.0", (System.Collections.Generic.IEnumerable<string>)tagNames);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ─── Dry-run tests ──────────────────────────────────────────────────────────

    [TestMethod]
    public void Rewrite_DryRun_ReportsChangesWithoutModifyingRepository()
    {
        const string OldEmail = "old@example.com";
        var repositoryPath = CreateRepositoryWithCommits(2, authorEmail: OldEmail);

        string originalHeadSha;
        using (var repo = new Repository(repositoryPath))
        {
            originalHeadSha = repo.Head.Tip.Sha;
        }

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
                DryRun = true,
            };

            var results = service.Rewrite(options, commitFilter: _ =>
                new GitCommitRewriteOverrides { AuthorEmail = "new@example.com" });

            // Should report changes.
            Assert.IsNotEmpty(results, "Dry run should report affected commits.");
            Assert.IsTrue(results.All(r => r.HeaderModified));

            // But the repository must be unchanged.
            using var repoAfter = new Repository(repositoryPath);
            Assert.AreEqual(originalHeadSha, repoAfter.Head.Tip.Sha,
                "Dry run must not change the HEAD commit SHA.");

            foreach (var commit in repoAfter.Head.Commits)
            {
                Assert.AreEqual(OldEmail, commit.Author.Email,
                    "Dry run must not change commit author emails.");
            }
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ─── Backup refs tests ──────────────────────────────────────────────────────

    [TestMethod]
    public void Rewrite_CommitFilter_CreatesBackupRefs()
    {
        var repositoryPath = CreateRepositoryWithCommits(1);

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
                BackupRefsNamespace = "refs/original/",
            };

            service.Rewrite(options, commitFilter: _ =>
                new GitCommitRewriteOverrides { AuthorEmail = "new@example.com" });

            using var repository = new Repository(repositoryPath);
            var backupRefs = repository.Refs
                .Where(r => r.CanonicalName.StartsWith("refs/original/", StringComparison.Ordinal))
                .ToList();

            Assert.IsNotEmpty(backupRefs, "Expected backup refs under refs/original/ to be created.");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ─── Specific-refs tests ────────────────────────────────────────────────────

    [TestMethod]
    public void Rewrite_SpecificRefs_OnlyRewritesTargetedRef()
    {
        var repositoryPath = CreateRepositoryWithTwoBranches("main", "feature",
            out var mainTipSha, out _);

        try
        {
            var service = new GitHistoryRewriteService();
            var options = new GitHistoryRewriteOptions
            {
                RepositoryPath = repositoryPath,
                Refs = ["feature"],
            };

            service.Rewrite(options, commitFilter: _ =>
                new GitCommitRewriteOverrides { AuthorEmail = "modified@example.com" });

            using var repository = new Repository(repositoryPath);

            // main should be untouched.
            var mainBranch = repository.Branches.First(b => b.FriendlyName == "main");
            Assert.AreEqual(mainTipSha, mainBranch.Tip.Sha,
                "main branch should not have been rewritten.");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ─── Repository helpers ─────────────────────────────────────────────────────

    private static string CreateRepositoryWithCommits(
        int count,
        string authorName = "Test Author",
        string authorEmail = "test@example.com")
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);

        for (var i = 0; i < count; i++)
        {
            var sig = new Signature(authorName, authorEmail, DateTimeOffset.UtcNow.AddMinutes(-count + i));
            var filePath = System.IO.Path.Combine(repositoryPath, $"file{i}.txt");
            File.WriteAllText(filePath, $"Content for commit {i}");
            Commands.Stage(repository, filePath);
            repository.Commit($"Commit {i}", sig, sig);
        }

        return repositoryPath;
    }

    private static string CreateRepositoryWithFiles(string[] fileNames)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("Test Author", "test@example.com", DateTimeOffset.UtcNow);

        foreach (var fileName in fileNames)
        {
            var filePath = System.IO.Path.Combine(repositoryPath, fileName);
            File.WriteAllText(filePath, $"Content of {fileName}");
        }

        Commands.Stage(repository, "*");
        repository.Commit("Add all files", sig, sig);

        return repositoryPath;
    }

    /// <summary>
    /// Creates a repo where the first commit only touches <paramref name="firstFile"/>
    /// and the second only touches <paramref name="secondFile"/>. Useful for testing
    /// <c>PruneEmptyCommits</c> when one commit becomes entirely empty after filtering.
    /// </summary>
    private static string CreateRepositoryWithTwoSeparateCommits(string firstFile, string secondFile)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("Test Author", "test@example.com", DateTimeOffset.UtcNow);

        // First commit: only-binary.exe
        var path1 = System.IO.Path.Combine(repositoryPath, firstFile);
        File.WriteAllText(path1, "binary content");
        Commands.Stage(repository, path1);
        repository.Commit($"Add {firstFile} (only-binary)", sig, sig);

        // Second commit: legit.txt
        var path2 = System.IO.Path.Combine(repositoryPath, secondFile);
        File.WriteAllText(path2, "text content");
        Commands.Stage(repository, path2);
        repository.Commit($"Add {secondFile}", sig, sig);

        return repositoryPath;
    }

    private static string CreateRepositoryWithTag(string tagName)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("Test Author", "test@example.com", DateTimeOffset.UtcNow);

        var filePath = System.IO.Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "content");
        Commands.Stage(repository, filePath);
        var commit = repository.Commit("Initial commit", sig, sig);

        // Lightweight tag pointing at the commit.
        repository.Tags.Add(tagName, commit);

        return repositoryPath;
    }

    private static string CreateRepositoryWithTwoBranches(
        string mainBranchName,
        string featureBranchName,
        out string mainTipSha,
        out string featureTipSha)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("Test Author", "test@example.com", DateTimeOffset.UtcNow);

        // Initial commit on default branch.
        var f0 = System.IO.Path.Combine(repositoryPath, "main.txt");
        File.WriteAllText(f0, "main");
        Commands.Stage(repository, f0);
        repository.Commit("Initial commit on main", sig, sig);

        // Rename the initial branch to mainBranchName (in case the default differs).
        if (repository.Head.FriendlyName != mainBranchName)
        {
            repository.Branches.Rename(repository.Head.FriendlyName, mainBranchName);
        }

        mainTipSha = repository.Head.Tip.Sha;

        // Create and check out feature branch.
        var featureBranch = repository.CreateBranch(featureBranchName);
        Commands.Checkout(repository, featureBranch);

        var f1 = System.IO.Path.Combine(repositoryPath, "feature.txt");
        File.WriteAllText(f1, "feature");
        Commands.Stage(repository, f1);
        repository.Commit("Feature commit", sig, sig);
        featureTipSha = repository.Head.Tip.Sha;

        // Switch back to main.
        Commands.Checkout(repository, repository.Branches[mainBranchName]);

        return repositoryPath;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "PowerCode.GitRewriteTests", Guid.NewGuid().ToString("N"));
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
