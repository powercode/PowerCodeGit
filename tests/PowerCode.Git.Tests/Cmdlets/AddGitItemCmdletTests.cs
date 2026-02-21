using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class AddGitItemCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Path_IsSetCorrectly()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["file1.txt", "file2.txt"],
        };

        Assert.HasCount(2, cmdlet.Path);
        Assert.AreEqual("file1.txt", cmdlet.Path![0]);
        Assert.AreEqual("file2.txt", cmdlet.Path[1]);
    }

    [TestMethod]
    public void All_DefaultsToFalse()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService());

        Assert.IsFalse(cmdlet.All.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_PathParameterSet_DefaultsApplied()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["file.txt"],
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual(1, options.Paths?.Count);
        Assert.AreEqual("file.txt", options.Paths![0]);
        Assert.IsFalse(options.All);
        Assert.IsFalse(options.Update);
        Assert.IsFalse(options.Force);
    }

    [TestMethod]
    public void BuildOptions_AllParameterSet_AllTrue()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService());
        cmdlet.All = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsTrue(options.All);
        Assert.IsFalse(options.Update);
        Assert.IsFalse(options.Force);
    }

    [TestMethod]
    public void BuildOptions_UpdateParameterSet_UpdateTrue()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService());
        cmdlet.Update = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsFalse(options.All);
        Assert.IsTrue(options.Update);
        Assert.IsFalse(options.Force);
    }

    [TestMethod]
    public void BuildOptions_WithForce_ForceTrue()
    {
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["ignored.txt"],
        };
        cmdlet.Force = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Force);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var prebuilt = new GitStageOptions
        {
            RepositoryPath = "D:\\prebuilt",
            All = true,
            Force = true,
        };
        var cmdlet = new AddGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Options = prebuilt,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(prebuilt, options);
    }

    private sealed class StubGitWorkingTreeService : IGitWorkingTreeService
    {
        public GitStatusResult GetStatus(GitStatusOptions options) =>
            new(options.RepositoryPath, "main", Array.Empty<GitStatusEntry>(), 0, 0, 0);

        public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options) =>
            Array.Empty<GitDiffEntry>();

        public void Stage(GitStageOptions options)
        {
        }

        public void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null)
        {
        }

        public void Reset(string repositoryPath, string? target, GitResetMode mode)
        {
        }
    }
}
