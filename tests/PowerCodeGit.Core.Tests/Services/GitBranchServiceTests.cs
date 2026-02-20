using LibGit2Sharp;
using PowerCodeGit.Abstractions.Models;
using PowerCodeGit.Core.Services;
using System.Threading;

namespace PowerCodeGit.Core.Tests.Services;

[TestClass]
public sealed class GitBranchServiceTests
{
    private const string DefaultBranchName = "master";

    [TestMethod]
    public void GetBranches_InvalidRepositoryPath_ThrowsArgumentException()
    {
        var service = new GitBranchService();

        Assert.Throws<ArgumentException>(() => service.GetBranches("X:\\not-a-real-repo"));
    }

    [TestMethod]
    public void GetBranches_EmptyPath_ThrowsArgumentException()
    {
        var service = new GitBranchService();

        Assert.Throws<ArgumentException>(() => service.GetBranches(string.Empty));
    }

    [TestMethod]
    public void GetBranches_ReturnsAllBranches()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

            var branches = service.GetBranches(repositoryPath);

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

            var branches = service.GetBranches(repositoryPath);

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

            var branches = service.GetBranches(repositoryPath);

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

        Assert.Throws<ArgumentException>(() => service.SwitchBranch("X:\\not-a-real-repo", "main"));
    }

    [TestMethod]
    public void SwitchBranch_EmptyBranchName_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithBranches();

        try
        {
            var service = new GitBranchService();

                Assert.Throws<ArgumentException>(() => service.SwitchBranch(repositoryPath, string.Empty));
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

            Assert.Throws<ArgumentException>(() => service.SwitchBranch(repositoryPath, "nonexistent"));
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

            var result = service.SwitchBranch(repositoryPath, "feature");

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

    private static string CreateRepositoryWithBranches()
    {
        var repositoryPath = CreateTemporaryDirectory();
        Repository.Init(repositoryPath);

        using var repository = new Repository(repositoryPath);
        var signature = new Signature("PowerCodeGit", "powercodegit@example.com", DateTimeOffset.UtcNow);

        var filePath = Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "initial content");
        Commands.Stage(repository, filePath);
        repository.Commit("Initial commit", signature, signature);

        repository.CreateBranch("feature");

        return repositoryPath;
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "PowerCodeGitTests", Guid.NewGuid().ToString("N"));
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
