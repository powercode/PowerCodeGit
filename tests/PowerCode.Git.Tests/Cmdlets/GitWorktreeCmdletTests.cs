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

    [TestMethod]
    public void BuildOptions_Pipeline_DerivesNameAndBranchFromInputBranch()
    {
        var branchInfo = new GitBranchInfo("main", false, false, "abc1234567", null, null, null);

        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Path = "C:\\worktrees\\main",
            InputBranch = branchInfo,
        };

        var options = cmdlet.BuildOptions("C:\\repo", "Pipeline");

        Assert.AreEqual("main.wt", options.Name);
        Assert.AreEqual("main", options.Branch);
        Assert.AreEqual("C:\\worktrees\\main", options.Path);
    }

    [TestMethod]
    public void BuildOptions_Pipeline_SlashesInBranchReplacedWithDash()
    {
        var branchInfo = new GitBranchInfo("feature/my-feature", false, false, "abc1234567", null, null, null);

        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Path = "C:\\worktrees\\feature",
            InputBranch = branchInfo,
        };

        var options = cmdlet.BuildOptions("C:\\repo", "Pipeline");

        Assert.AreEqual("feature-my-feature.wt", options.Name);
        Assert.AreEqual("feature/my-feature", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_Pipeline_LockedIsRespected()
    {
        var branchInfo = new GitBranchInfo("develop", false, false, "abc1234567", null, null, null);

        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Path = "C:\\worktrees\\develop",
            InputBranch = branchInfo,
            Locked = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo", "Pipeline");

        Assert.IsTrue(options.Locked);
    }

    [TestMethod]
    public void BuildOptions_Pipeline_NoPath_DefaultsToRepoNameDashBranch()
    {
        var branchInfo = new GitBranchInfo("main", false, false, "abc1234567", null, null, null);

        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repos\\MyProject",
            InputBranch = branchInfo,
        };

        var options = cmdlet.BuildOptions("C:\\repos\\MyProject", "Pipeline");

        Assert.AreEqual("C:\\repos\\MyProject-main", options.Path);
        Assert.AreEqual("main.wt", options.Name);
        Assert.AreEqual("main", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_Pipeline_NoPath_SlashBranch_DefaultsCorrectly()
    {
        var branchInfo = new GitBranchInfo("feature/login", false, false, "abc1234567", null, null, null);

        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "D:\\dev\\PowerCodeGit",
            InputBranch = branchInfo,
        };

        var options = cmdlet.BuildOptions("D:\\dev\\PowerCodeGit", "Pipeline");

        Assert.AreEqual("D:\\dev\\PowerCodeGit-feature-login", options.Path);
        Assert.AreEqual("feature-login.wt", options.Name);
        Assert.AreEqual("feature/login", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_BranchOnly_DerivesNameAndPath()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repos\\MyProject",
            Branch = "feature",
        };

        var options = cmdlet.BuildOptions("C:\\repos\\MyProject", "Create");

        Assert.AreEqual("feature.wt", options.Name);
        Assert.AreEqual("C:\\repos\\MyProject-feature", options.Path);
        Assert.AreEqual("feature", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_BranchWithSlash_DerivesNameAndPathWithSafeName()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "D:\\dev\\MyRepo",
            Branch = "feature/my-branch",
        };

        var options = cmdlet.BuildOptions("D:\\dev\\MyRepo", "Create");

        Assert.AreEqual("feature-my-branch.wt", options.Name);
        Assert.AreEqual("D:\\dev\\MyRepo-feature-my-branch", options.Path);
        Assert.AreEqual("feature/my-branch", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_BranchAndExplicitName_UsesProvidedName()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repos\\MyProject",
            Branch = "feature/login",
            Name = "login-wt",
        };

        var options = cmdlet.BuildOptions("C:\\repos\\MyProject", "Create");

        Assert.AreEqual("login-wt", options.Name);
        Assert.AreEqual("C:\\repos\\MyProject-feature-login", options.Path);
        Assert.AreEqual("feature/login", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_BranchAndExplicitPath_UsesProvidedPath()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repos\\MyProject",
            Branch = "hotfix/p1",
            Path = "C:\\worktrees\\hotfix",
        };

        var options = cmdlet.BuildOptions("C:\\repos\\MyProject", "Create");

        Assert.AreEqual("hotfix-p1.wt", options.Name);
        Assert.AreEqual("C:\\worktrees\\hotfix", options.Path);
        Assert.AreEqual("hotfix/p1", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_BranchAndExplicitNameAndPath_UsesBothProvided()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repos\\MyProject",
            Branch = "feature/my-branch",
            Name = "my-wt",
            Path = "C:\\worktrees\\my",
        };

        var options = cmdlet.BuildOptions("C:\\repos\\MyProject", "Create");

        Assert.AreEqual("my-wt", options.Name);
        Assert.AreEqual("C:\\worktrees\\my", options.Path);
        Assert.AreEqual("feature/my-branch", options.Branch);
    }

    [TestMethod]
    public void BuildOptions_BranchOnly_LockedIsRespected()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repos\\MyProject",
            Branch = "develop",
            Locked = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repos\\MyProject", "Create");

        Assert.IsTrue(options.Locked);
    }

    [TestMethod]
    public void BuildOptions_NoBranch_UsesExplicitNameAndPath()
    {
        var cmdlet = new NewGitWorktreeCmdlet(new StubGitWorktreeService())
        {
            RepoPath = "C:\\repo",
            Name = "my-wt",
            Path = "C:\\worktrees\\my",
        };

        var options = cmdlet.BuildOptions("C:\\repo", "Create");

        Assert.AreEqual("my-wt", options.Name);
        Assert.AreEqual("C:\\worktrees\\my", options.Path);
        Assert.IsNull(options.Branch);
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
    public IReadOnlyList<GitWorktreeInfo> GetWorktrees(GitWorktreeListOptions options)
    {
        return Array.Empty<GitWorktreeInfo>();
    }

    public GitWorktreeInfo AddWorktree(GitWorktreeAddOptions options) =>
        new GitWorktreeInfo(options.Name, options.Path, options.Locked, null);

    public void RemoveWorktree(GitWorktreeRemoveOptions options) { }

    public void LockWorktree(GitWorktreeLockOptions options) { }

    public void UnlockWorktree(GitWorktreeUnlockOptions options) { }
}
