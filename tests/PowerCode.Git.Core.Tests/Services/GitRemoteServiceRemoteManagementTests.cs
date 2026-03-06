using System;
using System.IO;
using System.Threading;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitRemoteServiceRemoteManagementTests
{
    // ---------------------------------------------------------------------------
    // GetRemotes(GitRemoteListOptions)
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void GetRemotes_WithListOptions_NoFilter_ReturnsAll()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://example.com/repo.git");
                repo.Network.Remotes.Add("upstream", "https://example.com/upstream.git");
            }

            var service = new GitRemoteService();
            var options = new GitRemoteListOptions { RepositoryPath = repositoryPath };

            var remotes = service.GetRemotes(options);

            Assert.HasCount(2, remotes);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetRemotes_WithListOptions_FilterByName_ReturnsSingleRemote()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://example.com/repo.git");
                repo.Network.Remotes.Add("upstream", "https://example.com/upstream.git");
            }

            var service = new GitRemoteService();
            var options = new GitRemoteListOptions { RepositoryPath = repositoryPath, Name = "origin" };

            var remotes = service.GetRemotes(options);

            Assert.HasCount(1, remotes);
            Assert.AreEqual("origin", remotes[0].Name);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void GetRemotes_WithListOptions_FilterByNonExistentName_ReturnsEmpty()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://example.com/repo.git");
            }

            var service = new GitRemoteService();
            var options = new GitRemoteListOptions { RepositoryPath = repositoryPath, Name = "nonexistent" };

            var remotes = service.GetRemotes(options);

            Assert.HasCount(0, remotes);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ---------------------------------------------------------------------------
    // AddRemote
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void AddRemote_ValidNameAndUrl_ReturnsGitRemoteInfo()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();
            var options = new GitRemoteAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "upstream",
                Url = "https://example.com/upstream.git",
            };

            var result = service.AddRemote(options);

            Assert.AreEqual("upstream", result.Name);
            Assert.AreEqual("https://example.com/upstream.git", result.FetchUrl);
            Assert.AreEqual("https://example.com/upstream.git", result.PushUrl);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void AddRemote_WithPushUrl_SetsPushUrlDistinctFromFetchUrl()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();
            var options = new GitRemoteAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
                Url = "https://example.com/repo.git",
                PushUrl = "git@example.com:user/repo.git",
            };

            var result = service.AddRemote(options);

            Assert.AreEqual("https://example.com/repo.git", result.FetchUrl);
            Assert.AreEqual("git@example.com:user/repo.git", result.PushUrl);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void AddRemote_AfterAdd_AppersInGetRemotes()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();
            service.AddRemote(new GitRemoteAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "myremote",
                Url = "https://example.com/myremote.git",
            });

            var remotes = service.GetRemotes(repositoryPath);

            Assert.HasCount(1, remotes);
            Assert.AreEqual("myremote", remotes[0].Name);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void AddRemote_DuplicateName_ThrowsNameConflictException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();
            var options = new GitRemoteAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
                Url = "https://example.com/repo1.git",
            };

            service.AddRemote(options);

            Assert.Throws<NameConflictException>(() => service.AddRemote(new GitRemoteAddOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
                Url = "https://example.com/repo2.git",
            }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ---------------------------------------------------------------------------
    // RemoveRemote
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void RemoveRemote_ExistingRemote_RemovesSuccessfully()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://example.com/repo.git");
            }

            var service = new GitRemoteService();
            service.RemoveRemote(new GitRemoteRemoveOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
            });

            var remotes = service.GetRemotes(repositoryPath);

            Assert.HasCount(0, remotes);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void RemoveRemote_NonExistentName_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();

            Assert.Throws<ArgumentException>(() => service.RemoveRemote(new GitRemoteRemoveOptions
            {
                RepositoryPath = repositoryPath,
                Name = "nonexistent",
            }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ---------------------------------------------------------------------------
    // RenameRemote
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void RenameRemote_ExistingRemote_ReturnsRenamedInfo()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://example.com/repo.git");
            }

            var service = new GitRemoteService();
            var result = service.RenameRemote(repositoryPath, "origin", "upstream");

            Assert.AreEqual("upstream", result.Name);
            Assert.AreEqual("https://example.com/repo.git", result.FetchUrl);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void RenameRemote_OldNameAbsentAfterRename()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://example.com/repo.git");
            }

            var service = new GitRemoteService();
            service.RenameRemote(repositoryPath, "origin", "upstream");

            var remotes = service.GetRemotes(repositoryPath);

            Assert.HasCount(1, remotes);
            Assert.AreEqual("upstream", remotes[0].Name);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void RenameRemote_NonExistentName_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();

            Assert.Throws<ArgumentException>(() =>
                service.RenameRemote(repositoryPath, "nonexistent", "newname"));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ---------------------------------------------------------------------------
    // UpdateRemoteUrl
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void UpdateRemoteUrl_SetFetchUrl_UpdatesFetchUrl()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://old.example.com/repo.git");
            }

            var service = new GitRemoteService();
            var result = service.UpdateRemoteUrl(new GitRemoteUpdateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
                Url = "https://new.example.com/repo.git",
            });

            Assert.AreEqual("https://new.example.com/repo.git", result.FetchUrl);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void UpdateRemoteUrl_SetPushUrl_UpdatesPushUrl()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://example.com/repo.git");
            }

            var service = new GitRemoteService();
            var result = service.UpdateRemoteUrl(new GitRemoteUpdateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
                PushUrl = "git@example.com:user/repo.git",
            });

            Assert.AreEqual("git@example.com:user/repo.git", result.PushUrl);
            Assert.AreEqual("https://example.com/repo.git", result.FetchUrl);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void UpdateRemoteUrl_SetBothUrls_UpdatesBoth()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            using (var repo = new Repository(repositoryPath))
            {
                repo.Network.Remotes.Add("origin", "https://old.example.com/repo.git");
            }

            var service = new GitRemoteService();
            var result = service.UpdateRemoteUrl(new GitRemoteUpdateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
                Url = "https://new.example.com/repo.git",
                PushUrl = "git@new.example.com:user/repo.git",
            });

            Assert.AreEqual("https://new.example.com/repo.git", result.FetchUrl);
            Assert.AreEqual("git@new.example.com:user/repo.git", result.PushUrl);
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void UpdateRemoteUrl_NoUrlOrPushUrl_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();

            Assert.Throws<ArgumentException>(() => service.UpdateRemoteUrl(new GitRemoteUpdateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "origin",
                // Neither Url nor PushUrl set
            }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    [TestMethod]
    public void UpdateRemoteUrl_NonExistentRemote_ThrowsArgumentException()
    {
        var repositoryPath = CreateRepositoryWithCommit();

        try
        {
            var service = new GitRemoteService();

            Assert.Throws<ArgumentException>(() => service.UpdateRemoteUrl(new GitRemoteUpdateOptions
            {
                RepositoryPath = repositoryPath,
                Name = "nonexistent",
                Url = "https://example.com/repo.git",
            }));
        }
        finally
        {
            DeleteDirectory(repositoryPath);
        }
    }

    // ---------------------------------------------------------------------------
    // Helpers shared with other service tests
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
