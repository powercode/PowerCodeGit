using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitWorktreeCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitWorktreeCmdlet(new StubGitWorktreeService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GetGitWorktreeCmdlet(null!));
    }
}

[TestClass]
public sealed class NewGitWorktreeCmdletTests
{
    [TestMethod]
    public void BuildOptions_AllParametersSet_MapsCorrectly()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "feature-wt",
            Path = "C:\\worktrees\\feature",
            Branch = "feature",
            Locked = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("feature-wt", options.Name);
        Assert.AreEqual("C:\\worktrees\\feature", options.Path);
        Assert.AreEqual("feature", options.Branch);
        Assert.IsTrue(options.Locked);
    }

    [TestMethod]
    public void BuildOptions_MinimalParameters_DefaultsCorrect()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "my-wt",
            Path = "C:\\worktrees\\my",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("my-wt", options.Name);
        Assert.AreEqual("C:\\worktrees\\my", options.Path);
        Assert.IsNull(options.Branch);
        Assert.IsFalse(options.Locked);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_OptionsPropertyAssigned()
    {
        var predefinedOptions = new GitWorktreeAddOptions
        {
            RepositoryPath = "D:\\other-repo",
            Name = "wt",
            Path = "D:\\worktrees\\wt",
            Branch = "main",
        };

        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            Options = predefinedOptions,
        };

        Assert.AreSame(predefinedOptions, cmdlet.Options);
        Assert.AreEqual("D:\\other-repo", cmdlet.Options.RepositoryPath);
    }

    [TestMethod]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new NewGitWorktreeCmdlet(null!));
    }
}

[TestClass]
public sealed class RemoveGitWorktreeCmdletTests
{
    [TestMethod]
    public void BuildOptions_NameAndForce_MapsCorrectly()
    {
        var cmdlet = new RemoveGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "old-wt",
            Force = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("old-wt", options.Name);
        Assert.IsTrue(options.Force);
    }

    [TestMethod]
    public void BuildOptions_NameOnly_ForceIsFalse()
    {
        var cmdlet = new RemoveGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "old-wt",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsFalse(options.Force);
    }

    [TestMethod]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new RemoveGitWorktreeCmdlet(null!));
    }
}

[TestClass]
public sealed class LockGitWorktreeCmdletTests
{
    [TestMethod]
    public void BuildOptions_NameAndReason_MapsCorrectly()
    {
        var cmdlet = new LockGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "my-wt",
            Reason = "Work in progress",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("my-wt", options.Name);
        Assert.AreEqual("Work in progress", options.Reason);
    }

    [TestMethod]
    public void BuildOptions_NameOnly_ReasonIsNull()
    {
        var cmdlet = new LockGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "my-wt",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsNull(options.Reason);
    }

    [TestMethod]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new LockGitWorktreeCmdlet(null!));
    }
}

[TestClass]
public sealed class UnlockGitWorktreeCmdletTests
{
    [TestMethod]
    public void BuildOptions_Name_MapsCorrectly()
    {
        var cmdlet = new UnlockGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "my-wt",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("my-wt", options.Name);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_OptionsPropertyAssigned()
    {
        var predefinedOptions = new GitWorktreeUnlockOptions
        {
            RepositoryPath = "D:\\other-repo",
            Name = "wt",
        };

        var cmdlet = new UnlockGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            Options = predefinedOptions,
        };

        Assert.AreSame(predefinedOptions, cmdlet.Options);
    }

    [TestMethod]
    public void Constructor_NullService_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new UnlockGitWorktreeCmdlet(null!));
    }
}

internal sealed class StubGitWorktreeService : IGitWorktreeService
{
    public IReadOnlyList<GitWorktreeInfo> GetWorktrees(string repositoryPath)
    {
        return Array.Empty<GitWorktreeInfo>();
    }

    public GitWorktreeInfo AddWorktree(GitWorktreeAddOptions options) =>
        new GitWorktreeInfo(options.Name, options.Path, options.Locked, null);

    public void RemoveWorktree(GitWorktreeRemoveOptions options) { }

    public void LockWorktree(GitWorktreeLockOptions options) { }

    public void UnlockWorktree(GitWorktreeUnlockOptions options) { }
}
