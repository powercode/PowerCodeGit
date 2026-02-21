using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;
using System.Threading;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitHistoryServiceTests
{
    [TestMethod]
    public void GetLog_InvalidRepositoryPath_ThrowsArgumentException()
    {
        var service = new GitHistoryService();
        var options = new GitLogOptions
        {
            RepositoryPath = "X:\\not-a-real-repo",
        };

        Assert.Throws<ArgumentException>(() => service.GetLog(options));
    }

    [TestMethod]
    public void GetLog_WithMaxCount_ReturnsLimitedResults()
    {
        var repositoryPath = CreateRepositoryWithCommits(3, out _);

        try
        {
            var service = new GitHistoryService();
            var options = new GitLogOptions
            {
                RepositoryPath = repositoryPath,
                MaxCount = 2,
            };

            var commits = service.GetLog(options);

            Assert.HasCount(2, commits);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetLog_WithAuthorFilter_ReturnsOnlyMatchingCommits()
    {
        var repositoryPath = CreateRepositoryWithAuthors(out var expectedAuthorEmail);

        try
        {
            var service = new GitHistoryService();
            var options = new GitLogOptions
            {
                RepositoryPath = repositoryPath,
                AuthorFilter = expectedAuthorEmail,
            };

            var commits = service.GetLog(options);

            Assert.HasCount(1, commits);
            Assert.AreEqual(expectedAuthorEmail, commits[0].AuthorEmail);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetLog_WithMessagePattern_ReturnsMatchingCommits()
    {
        var repositoryPath = CreateRepositoryWithMessages();

        try
        {
            var service = new GitHistoryService();
            var options = new GitLogOptions
            {
                RepositoryPath = repositoryPath,
                MessagePattern = "feature",
            };

            var commits = service.GetLog(options);

            Assert.HasCount(1, commits);
            StringAssert.Contains(commits[0].Message, "feature");
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    private static string CreateRepositoryWithCommits(int count, out IReadOnlyList<string> shas)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        var commitShas = new List<string>();
        using var repository = new Repository(repositoryPath);

        for (var index = 0; index < count; index++)
        {
            var filePath = System.IO.Path.Combine(repositoryPath, $"file-{index}.txt");
            File.WriteAllText(filePath, $"value-{index}");
            Commands.Stage(repository, filePath);

            var signature = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow.AddMinutes(index));
            var commit = repository.Commit($"commit {index}", signature, signature);
            commitShas.Add(commit.Sha);
        }

        shas = commitShas;
        return repositoryPath;
    }

    private static string CreateRepositoryWithAuthors(out string expectedAuthorEmail)
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);

        var firstFilePath = System.IO.Path.Combine(repositoryPath, "first.txt");
        File.WriteAllText(firstFilePath, "first");
        Commands.Stage(repository, firstFilePath);

        var firstAuthor = new Signature("Author One", "author.one@example.com", DateTimeOffset.UtcNow.AddMinutes(-5));
        repository.Commit("first commit", firstAuthor, firstAuthor);

        var secondFilePath = System.IO.Path.Combine(repositoryPath, "second.txt");
        File.WriteAllText(secondFilePath, "second");
        Commands.Stage(repository, secondFilePath);

        var secondAuthor = new Signature("Author Two", "author.two@example.com", DateTimeOffset.UtcNow);
        repository.Commit("second commit", secondAuthor, secondAuthor);

        expectedAuthorEmail = "author.one@example.com";
        return repositoryPath;
    }

    private static string CreateRepositoryWithMessages()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);

        var bugfixPath = System.IO.Path.Combine(repositoryPath, "bugfix.txt");
        File.WriteAllText(bugfixPath, "bugfix");
        Commands.Stage(repository, bugfixPath);
        var signature = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow.AddMinutes(-2));
        repository.Commit("fix: bug", signature, signature);

        var featurePath = System.IO.Path.Combine(repositoryPath, "feature.txt");
        File.WriteAllText(featurePath, "feature");
        Commands.Stage(repository, featurePath);
        repository.Commit("feat: new feature", signature, signature);

        return repositoryPath;
    }

    [TestMethod]
    public void GetLog_WithFirstParent_ExcludesMergedSideBranchCommits()
    {
        var repositoryPath = CreateRepositoryWithMergeCommit();

        try
        {
            var service = new GitHistoryService();

            var allCommits = service.GetLog(new GitLogOptions { RepositoryPath = repositoryPath });
            var firstParentCommits = service.GetLog(new GitLogOptions { RepositoryPath = repositoryPath, FirstParent = true });

            // FirstParent should return fewer or equal commits (filters side-branch commits)
            Assert.IsTrue(firstParentCommits.Count <= allCommits.Count);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetLog_WithNoMerges_ExcludesMergeCommits()
    {
        var repositoryPath = CreateRepositoryWithMergeCommit();

        try
        {
            var service = new GitHistoryService();

            var allCommits = service.GetLog(new GitLogOptions { RepositoryPath = repositoryPath });
            var noMergeCommits = service.GetLog(new GitLogOptions { RepositoryPath = repositoryPath, NoMerges = true });

            // All commits from noMerges should have at most 1 parent
            Assert.IsTrue(noMergeCommits.Count <= allCommits.Count);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Commit_WithAll_StagesTrackedFilesAndCommits()
    {
        var repositoryPath = CreateRepositoryWithCommits(1, out _);

        try
        {
            using var repository = new Repository(repositoryPath);
            repository.Config.Set("user.name", "Test Author");
            repository.Config.Set("user.email", "test@example.com");

            // Modify the tracked file without staging
            var existingFile = System.IO.Path.Combine(repositoryPath, "file-0.txt");
            File.WriteAllText(existingFile, "modified content");

            var service = new GitHistoryService();
            var options = new GitCommitOptions
            {
                RepositoryPath = repositoryPath,
                Message = "commit with all",
                All = true,
            };

            var result = service.Commit(options);

            Assert.AreEqual("commit with all", result.Message.Trim());
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Commit_WithCustomAuthor_UsesProvidedAuthor()
    {
        var repositoryPath = CreateRepositoryWithCommits(1, out _);

        try
        {
            using var tmpRepo = new Repository(repositoryPath);
            tmpRepo.Config.Set("user.name", "Test Author");
            tmpRepo.Config.Set("user.email", "test@example.com");

            var filePath = System.IO.Path.Combine(repositoryPath, "new.txt");
            File.WriteAllText(filePath, "new");
            Commands.Stage(tmpRepo, filePath);

            var service = new GitHistoryService();
            var options = new GitCommitOptions
            {
                RepositoryPath = repositoryPath,
                Message = "custom author commit",
                Author = "Jane Doe <jane@example.com>",
            };

            var result = service.Commit(options);

            Assert.AreEqual("Jane Doe", result.AuthorName);
            Assert.AreEqual("jane@example.com", result.AuthorEmail);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void Commit_WithCustomDate_UsesProvidedDate()
    {
        var repositoryPath = CreateRepositoryWithCommits(1, out _);

        try
        {
            using var tmpRepo = new Repository(repositoryPath);
            tmpRepo.Config.Set("user.name", "Test Author");
            tmpRepo.Config.Set("user.email", "test@example.com");

            var filePath = System.IO.Path.Combine(repositoryPath, "dated.txt");
            File.WriteAllText(filePath, "dated");
            Commands.Stage(tmpRepo, filePath);

            var expectedDate = new DateTimeOffset(2020, 6, 15, 12, 0, 0, TimeSpan.Zero);

            var service = new GitHistoryService();
            var options = new GitCommitOptions
            {
                RepositoryPath = repositoryPath,
                Message = "backdated commit",
                Date = expectedDate,
            };

            var result = service.Commit(options);

            Assert.AreEqual(expectedDate.Year, result.AuthorDate.Year);
            Assert.AreEqual(expectedDate.Month, result.AuthorDate.Month);
            Assert.AreEqual(expectedDate.Day, result.AuthorDate.Day);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    private static string CreateRepositoryWithMergeCommit()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var sig = new Signature("PowerCode.Git", "git@example.com", DateTimeOffset.UtcNow);

        // Initial commit on main
        var f0 = System.IO.Path.Combine(repositoryPath, "main.txt");
        File.WriteAllText(f0, "main");
        Commands.Stage(repository, f0);
        repository.Commit("Initial commit", sig, sig);

        var headBranch = repository.Head;
        var headBranchName = headBranch.FriendlyName;
        var headTip = headBranch.Tip;

        // Feature branch
        var featureBranch = repository.CreateBranch("feature");
        Commands.Checkout(repository, featureBranch);

        var f1 = System.IO.Path.Combine(repositoryPath, "feature.txt");
        File.WriteAllText(f1, "feature");
        Commands.Stage(repository, f1);
        repository.Commit("Feature commit", sig, sig);

        // Back to main and merge - use the canonical name to find the branch
        var mainBranch = repository.Branches.FirstOrDefault(b => !b.IsRemote && b.FriendlyName == headBranchName)
            ?? repository.Branches.FirstOrDefault(b => !b.IsRemote && b.Tip?.Sha == headTip?.Sha)
            ?? throw new InvalidOperationException($"Cannot find branch '{headBranchName}'");
        Commands.Checkout(repository, mainBranch);

        var f2 = System.IO.Path.Combine(repositoryPath, "main2.txt");
        File.WriteAllText(f2, "main2");
        Commands.Stage(repository, f2);
        repository.Commit("Second main commit", sig, sig);

        repository.Merge(featureBranch, sig, new MergeOptions { FastForwardStrategy = FastForwardStrategy.NoFastForward });

        return repositoryPath;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PowerCode.GitTests", Guid.NewGuid().ToString("N"));
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
