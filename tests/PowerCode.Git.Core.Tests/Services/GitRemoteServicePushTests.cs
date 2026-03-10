using System;
using System.IO;
using System.Linq;
using System.Threading;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitRemoteServicePushTests
{
    // ---------------------------------------------------------------------------
    // Push with Tags option
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void Push_WithTagsOption_PushesAllTagsToRemote()
    {
        var workPath = CreateRepositoryWithCommit();
        var barePath = CreateBareRepository();

        try
        {
            using (var repo = new Repository(workPath))
            {
                repo.Network.Remotes.Add("origin", barePath);

                // Create two tags to confirm all are pushed in one call.
                repo.ApplyTag("v1.0.0");
                repo.ApplyTag("v2.0.0");
            }

            var service = new GitRemoteService();
            service.Push(new GitPushOptions
            {
                RepositoryPath = workPath,
                RemoteName = "origin",
                Tags = true,
            });

            // Verify both tags landed in the bare remote.
            using var bareRepo = new Repository(barePath);
            var tagNames = bareRepo.Tags.Select(t => t.FriendlyName).ToList();
            Assert.Contains("v1.0.0", tagNames);
            Assert.Contains("v2.0.0", tagNames);
        }
        finally
        {
            DeleteDirectory(workPath);
            DeleteDirectory(barePath);
        }
    }

    [TestMethod]
    public void Push_WithTagsOption_NoTags_DoesNotThrow()
    {
        var workPath = CreateRepositoryWithCommit();
        var barePath = CreateBareRepository();

        try
        {
            using (var repo = new Repository(workPath))
            {
                repo.Network.Remotes.Add("origin", barePath);
                // Push the branch first so the bare repo has the initial commit.
                repo.Network.Push(repo.Network.Remotes["origin"], repo.Head.CanonicalName, new PushOptions());
            }

            var service = new GitRemoteService();

            // A repo with no tags should not throw when -Tags is used.
            var result = service.Push(new GitPushOptions
            {
                RepositoryPath = workPath,
                RemoteName = "origin",
                Tags = true,
            });

            Assert.IsNotNull(result);
        }
        finally
        {
            DeleteDirectory(workPath);
            DeleteDirectory(barePath);
        }
    }

    // ---------------------------------------------------------------------------
    // Push with All option
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void Push_WithAllOption_PushesAllBranchesToRemote()
    {
        var workPath = CreateRepositoryWithCommit();
        var barePath = CreateBareRepository();

        try
        {
            using (var repo = new Repository(workPath))
            {
                repo.Network.Remotes.Add("origin", barePath);

                var signature = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);

                // Create an additional branch with a commit.
                var featureBranch = repo.CreateBranch("feature/all-test");
                Commands.Checkout(repo, featureBranch);
                var featureFile = Path.Combine(workPath, "feature.txt");
                File.WriteAllText(featureFile, "feature");
                Commands.Stage(repo, featureFile);
                repo.Commit("Feature commit", signature, signature);

                Commands.Checkout(repo, repo.Branches["main"] ?? repo.Branches["master"]);
            }

            var service = new GitRemoteService();
            service.Push(new GitPushOptions
            {
                RepositoryPath = workPath,
                RemoteName = "origin",
                All = true,
            });

            using var bareRepo = new Repository(barePath);
            var remoteRefNames = bareRepo.Refs.Select(r => r.CanonicalName).ToList();
            Assert.IsTrue(
                remoteRefNames.Any(r => r.Contains("feature/all-test")),
                "feature/all-test branch should be present on remote");
        }
        finally
        {
            DeleteDirectory(workPath);
            DeleteDirectory(barePath);
        }
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static string CreateRepositoryWithCommit()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var filePath = Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "content");
        Commands.Stage(repository, filePath);

        var signature = new Signature("PowerCode.Git", "PowerCode.Git@example.com", DateTimeOffset.UtcNow);
        repository.Commit("Initial commit", signature, signature);

        return repositoryPath;
    }

    private static string CreateBareRepository()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath, isBare: true);
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
