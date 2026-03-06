using System;
using System.IO;
using System.Linq;
using System.Threading;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Core.Services;

namespace PowerCode.Git.Core.Tests.Services;

[TestClass]
public sealed class GitConfigServiceTests
{
    // ── Argument validation ──────────────────────────────────────────────────

    [TestMethod]
    public void GetConfigEntries_NullOptions_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentNullException>(() => service.GetConfigEntries(null!));
    }

    [TestMethod]
    public void GetConfigEntries_InvalidRepositoryPath_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentException>(() =>
            service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = "X:\\not-a-real-repo" }));
    }

    [TestMethod]
    public void GetConfigValue_NullOptions_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentNullException>(() => service.GetConfigValue(null!));
    }

    [TestMethod]
    public void GetConfigValue_NullName_Throws()
    {
        var service = new GitConfigService();

        // Name validation fires before repo validation, so path need not be real.
        Assert.Throws<ArgumentException>(() =>
            service.GetConfigValue(new GitConfigGetOptions { RepositoryPath = "X:\\not-a-real-repo" }));
    }

    [TestMethod]
    public void SetConfigValue_NullOptions_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentNullException>(() => service.SetConfigValue(null!));
    }

    [TestMethod]
    public void SetConfigValue_EmptyName_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentException>(() =>
            service.SetConfigValue(new GitConfigSetOptions
            {
                RepositoryPath = "X:\\not-a-real-repo",
                Name = "",
                Value = "value",
            }));
    }

    // ── GetConfigEntries ─────────────────────────────────────────────────────

    [TestMethod]
    public void GetConfigEntries_ReturnsLocalEntry()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane");

        try
        {
            var service = new GitConfigService();

            var entries = service.GetConfigEntries(new GitConfigGetOptions { RepositoryPath = repoPath });

            var match = entries.FirstOrDefault(e => e.Name == "user.name" && e.Scope == GitConfigScope.Local);
            Assert.IsNotNull(match);
            Assert.AreEqual("Jane", match.Value);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void GetConfigEntries_LocalScope_FiltersToLocalOnly()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane");

        try
        {
            var service = new GitConfigService();

            var entries = service.GetConfigEntries(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Scope = GitConfigScope.Local,
            });

            var match = entries.FirstOrDefault(e => e.Name == "user.name");
            Assert.IsNotNull(match);
            // Service always populates Scope from the ConfigurationLevel of each entry.
            Assert.IsTrue(entries.All(e => e.Scope == GitConfigScope.Local));
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void GetConfigEntries_ShowScope_PopulatesScopeField()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane");

        try
        {
            var service = new GitConfigService();

            var entries = service.GetConfigEntries(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Scope = GitConfigScope.Local,
            });

            var match = entries.FirstOrDefault(e => e.Name == "user.name");
            Assert.IsNotNull(match);
            Assert.AreEqual(GitConfigScope.Local, match.Scope);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void GetConfigEntries_AlwaysPopulatesScope()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane");

        try
        {
            var service = new GitConfigService();

            var entries = service.GetConfigEntries(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
            });

            // Scope is always derived from the ConfigurationLevel of each entry.
            var match = entries.FirstOrDefault(e => e.Name == "user.name");
            Assert.IsNotNull(match);
            Assert.IsNotNull(match.Scope);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    // ── GetConfigValue ───────────────────────────────────────────────────────

    [TestMethod]
    public void GetConfigValue_Found_ReturnsEntry()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane Doe");

        try
        {
            var service = new GitConfigService();

            var entry = service.GetConfigValue(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Name = "user.name",
            });

            Assert.IsNotNull(entry);
            Assert.AreEqual("user.name", entry.Name);
            Assert.AreEqual("Jane Doe", entry.Value);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void GetConfigValue_NotFound_ReturnsNull()
    {
        var repoPath = CreateRepository();

        try
        {
            var service = new GitConfigService();

            var entry = service.GetConfigValue(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Name = "nonexistent.key",
            });

            Assert.IsNull(entry);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void GetConfigValue_WithLocalScope_ReturnsEntry()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane");

        try
        {
            var service = new GitConfigService();

            var entry = service.GetConfigValue(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Name = "user.name",
                Scope = GitConfigScope.Local,
            });

            Assert.IsNotNull(entry);
            Assert.AreEqual("Jane", entry.Value);
            Assert.AreEqual(GitConfigScope.Local, entry.Scope);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void GetConfigValue_AlwaysPopulatesScope()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane");

        try
        {
            var service = new GitConfigService();

            var entry = service.GetConfigValue(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Name = "user.name",
            });

            Assert.IsNotNull(entry);
            // Scope is always derived from the ConfigurationLevel of the returned entry.
            Assert.AreEqual(GitConfigScope.Local, entry.Scope);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    // ── SetConfigValue ───────────────────────────────────────────────────────

    [TestMethod]
    public void SetConfigValue_WritesLocalEntry()
    {
        var repoPath = CreateRepository();

        try
        {
            var service = new GitConfigService();

            service.SetConfigValue(new GitConfigSetOptions
            {
                RepositoryPath = repoPath,
                Name = "user.name",
                Value = "Jane Doe",
            });

            // Verify the value was written to the local config via LibGit2Sharp directly.
            using var repo = new Repository(repoPath);
            var entry = repo.Config.Get<string>("user.name", ConfigurationLevel.Local);
            Assert.IsNotNull(entry);
            Assert.AreEqual("Jane Doe", entry.Value);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void SetConfigValue_WithLocalScope_WritesLocalEntry()
    {
        var repoPath = CreateRepository();

        try
        {
            var service = new GitConfigService();

            service.SetConfigValue(new GitConfigSetOptions
            {
                RepositoryPath = repoPath,
                Name = "user.email",
                Value = "jane@example.com",
                Scope = GitConfigScope.Local,
            });

            using var repo = new Repository(repoPath);
            var entry = repo.Config.Get<string>("user.email", ConfigurationLevel.Local);
            Assert.IsNotNull(entry);
            Assert.AreEqual("jane@example.com", entry.Value);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    // ── UnsetConfigValue — argument validation ───────────────────────────────

    [TestMethod]
    public void UnsetConfigValue_NullOptions_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentNullException>(() => service.UnsetConfigValue(null!));
    }

    [TestMethod]
    public void UnsetConfigValue_EmptyName_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentException>(() =>
            service.UnsetConfigValue(new GitConfigUnsetOptions
            {
                RepositoryPath = "X:\\not-a-real-repo",
                Name = "",
            }));
    }

    [TestMethod]
    public void UnsetConfigValue_InvalidRepositoryPath_Throws()
    {
        var service = new GitConfigService();

        Assert.Throws<ArgumentException>(() =>
            service.UnsetConfigValue(new GitConfigUnsetOptions
            {
                RepositoryPath = "X:\\not-a-real-repo",
                Name = "user.name",
            }));
    }

    // ── UnsetConfigValue — behaviour ─────────────────────────────────────────

    [TestMethod]
    public void UnsetConfigValue_RemovesLocalEntry()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.name", "Jane Doe");

        try
        {
            var service = new GitConfigService();

            service.UnsetConfigValue(new GitConfigUnsetOptions
            {
                RepositoryPath = repoPath,
                Name = "user.name",
            });

            using var repo = new Repository(repoPath);
            var entry = repo.Config.Get<string>("user.name", ConfigurationLevel.Local);
            Assert.IsNull(entry);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void UnsetConfigValue_WithLocalScope_RemovesLocalEntry()
    {
        var repoPath = CreateRepositoryWithLocalConfig("user.email", "jane@example.com");

        try
        {
            var service = new GitConfigService();

            service.UnsetConfigValue(new GitConfigUnsetOptions
            {
                RepositoryPath = repoPath,
                Name = "user.email",
                Scope = GitConfigScope.Local,
            });

            using var repo = new Repository(repoPath);
            var entry = repo.Config.Get<string>("user.email", ConfigurationLevel.Local);
            Assert.IsNull(entry);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void UnsetConfigValue_EntryReadBackViaService_ReturnsNull()
    {
        var repoPath = CreateRepositoryWithLocalConfig("push.default", "simple");

        try
        {
            var service = new GitConfigService();

            service.UnsetConfigValue(new GitConfigUnsetOptions
            {
                RepositoryPath = repoPath,
                Name = "push.default",
                Scope = GitConfigScope.Local,
            });

            var entry = service.GetConfigValue(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Name = "push.default",
                Scope = GitConfigScope.Local,
            });

            Assert.IsNull(entry);
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    [TestMethod]
    public void UnsetConfigValue_SetThenUnset_EntryAbsentInListByScope()
    {
        var repoPath = CreateRepository();

        try
        {
            var service = new GitConfigService();

            service.SetConfigValue(new GitConfigSetOptions
            {
                RepositoryPath = repoPath,
                Name = "core.autocrlf",
                Value = "input",
            });

            service.UnsetConfigValue(new GitConfigUnsetOptions
            {
                RepositoryPath = repoPath,
                Name = "core.autocrlf",
            });

            var entries = service.GetConfigEntries(new GitConfigGetOptions
            {
                RepositoryPath = repoPath,
                Scope = GitConfigScope.Local,
            });

            Assert.IsFalse(entries.Any(e => e.Name == "core.autocrlf"));
        }
        finally
        {
            DeleteDirectory(repoPath);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a bare-minimum git repository in a temporary directory.
    /// </summary>
    private static string CreateRepository()
    {
        var path = CreateTemporaryDirectory();
        Repository.Init(path);

        return path;
    }

    /// <summary>
    /// Creates a repository and writes a single local config entry.
    /// </summary>
    private static string CreateRepositoryWithLocalConfig(string key, string value)
    {
        var path = CreateRepository();

        using var repo = new Repository(path);
        repo.Config.Set(key, value);

        return path;
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
