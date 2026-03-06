using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;
using System.Threading;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitTagServiceTests
{
    [TestMethod]
    public void GetTags_InvalidRepositoryPath_ThrowsArgumentException()
    {
        var service = new GitTagService();

        Assert.Throws<ArgumentException>(() => service.GetTags("X:\\not-a-real-repo"));
    }

    [TestMethod]
    public void GetTags_EmptyPath_ThrowsArgumentException()
    {
        var service = new GitTagService();

        Assert.Throws<ArgumentException>(() => service.GetTags(string.Empty));
    }

    [TestMethod]
    public void GetTags_NoTags_ReturnsEmptyList()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitTagService();

            var tags = service.GetTags(repositoryPath);

            Assert.HasCount(0, tags);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetTags_LightweightTag_ReturnsTag()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repository = new Repository(repositoryPath))
            {
                repository.Tags.Add("v1.0.0", repository.Head.Tip);
            }

            var service = new GitTagService();

            var tags = service.GetTags(repositoryPath);

            Assert.HasCount(1, tags);
            Assert.AreEqual("v1.0.0", tags[0].Name);
            Assert.IsFalse(tags[0].IsAnnotated);
            Assert.IsNull(tags[0].TaggerName);
            Assert.IsNull(tags[0].TaggerEmail);
            Assert.IsNull(tags[0].TagDate);
            Assert.IsNull(tags[0].Message);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetTags_AnnotatedTag_IncludesMetadata()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repository = new Repository(repositoryPath))
            {
                var tagger = new Signature("Tagger Name", "tagger@example.com", DateTimeOffset.UtcNow);
                repository.Tags.Add("v2.0.0", repository.Head.Tip, tagger, "Release v2.0.0");
            }

            var service = new GitTagService();

            var tags = service.GetTags(repositoryPath);

            Assert.HasCount(1, tags);
            Assert.AreEqual("v2.0.0", tags[0].Name);
            Assert.IsTrue(tags[0].IsAnnotated);
            Assert.AreEqual("Tagger Name", tags[0].TaggerName);
            Assert.AreEqual("tagger@example.com", tags[0].TaggerEmail);
            Assert.IsNotNull(tags[0].TagDate);
            Assert.AreEqual("Release v2.0.0", tags[0].Message);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetTags_MultipleTags_ReturnsAll()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repository = new Repository(repositoryPath))
            {
                repository.Tags.Add("v1.0.0", repository.Head.Tip);

                var tagger = new Signature("Tagger", "tagger@example.com", DateTimeOffset.UtcNow);
                repository.Tags.Add("v2.0.0", repository.Head.Tip, tagger, "Annotated release");
            }

            var service = new GitTagService();

            var tags = service.GetTags(repositoryPath);

            Assert.HasCount(2, tags);
            Assert.IsTrue(tags.Any(t => t.Name == "v1.0.0"));
            Assert.IsTrue(tags.Any(t => t.Name == "v2.0.0"));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetTags_TagHasValidSha()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repository = new Repository(repositoryPath))
            {
                repository.Tags.Add("v1.0.0", repository.Head.Tip);
            }

            var service = new GitTagService();

            var tags = service.GetTags(repositoryPath);

            Assert.HasCount(1, tags);
            Assert.IsFalse(string.IsNullOrWhiteSpace(tags[0].Sha));
            Assert.AreEqual(7, tags[0].ShortSha.Length);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetTags_WithPattern_ReturnsMatchingTagsOnly()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repository = new Repository(repositoryPath))
            {
                repository.Tags.Add("v1.0.0", repository.Head.Tip);
                repository.Tags.Add("v2.0.0", repository.Head.Tip);
                var tagger = new Signature("T", "t@t.com", DateTimeOffset.UtcNow);
                repository.Tags.Add("release-alpha", repository.Head.Tip, tagger, "alpha");
            }

            var service = new GitTagService();
            var tags = service.GetTags(new GitTagListOptions
            {
                RepositoryPath = repositoryPath,
                Pattern = "v*",
            });

            Assert.HasCount(2, tags);
            Assert.IsTrue(tags.All(t => t.Name.StartsWith("v", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetTags_WithSortByVersion_ReturnsSortedByVersion()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repository = new Repository(repositoryPath))
            {
                repository.Tags.Add("v10.0.0", repository.Head.Tip);
                repository.Tags.Add("v2.0.0", repository.Head.Tip);
                repository.Tags.Add("v1.0.0", repository.Head.Tip);
            }

            var service = new GitTagService();
            var tags = service.GetTags(new GitTagListOptions
            {
                RepositoryPath = repositoryPath,
                SortBy = "version",
            });

            // v1 < v2 < v10 when sorted by version
            Assert.AreEqual("v1.0.0", tags[0].Name);
            Assert.AreEqual("v2.0.0", tags[1].Name);
            Assert.AreEqual("v10.0.0", tags[2].Name);
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
        var filePath = Path.Combine(repositoryPath, "file.txt");
        File.WriteAllText(filePath, "content");
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
