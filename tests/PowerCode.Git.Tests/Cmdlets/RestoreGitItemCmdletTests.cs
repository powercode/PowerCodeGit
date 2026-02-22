using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class RestoreGitItemCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void BuildOptions_PathParameterSet_ReturnsCorrectOptions()
    {
        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["file.txt"],
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual(1, options.Paths?.Count);
        Assert.AreEqual("file.txt", options.Paths![0]);
        Assert.IsFalse(options.All);
        Assert.IsFalse(options.Staged);
        Assert.IsNull(options.Source);
    }

    [TestMethod]
    public void BuildOptions_StagedSwitch_SetsStagedTrue()
    {
        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["file.txt"],
        };
        cmdlet.Staged = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Staged);
    }

    [TestMethod]
    public void BuildOptions_AllParameterSet_AllTrue()
    {
        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService());
        cmdlet.All = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsTrue(options.All);
        Assert.IsFalse(options.Staged);
    }

    [TestMethod]
    public void BuildOptions_WithSource_SourcePropagated()
    {
        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["file.txt"],
            Source = "HEAD~1",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("HEAD~1", options.Source);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var prebuilt = new GitRestoreOptions
        {
            RepositoryPath = "D:\\prebuilt",
            All = true,
        };
        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Options = prebuilt,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(prebuilt, options);
    }

    [TestMethod]
    public void Hunk_IsSetCorrectly()
    {
        var hunk = new GitDiffHunk(
            filePath: "file.txt",
            oldPath: "file.txt",
            status: GitFileStatus.Modified,
            oldStart: 1,
            oldLineCount: 3,
            newStart: 1,
            newLineCount: 2,
            header: "@@ -1,3 +1,2 @@",
            content: "@@ -1,3 +1,2 @@\n context\n-removed\n context",
            linesAdded: 0,
            linesDeleted: 1);

        var cmdlet = new RestoreGitItemCmdlet(new StubGitWorkingTreeService())
        {
            Hunk = [hunk],
        };

        Assert.HasCount(1, cmdlet.Hunk);
        Assert.AreEqual("file.txt", cmdlet.Hunk![0].FilePath);
    }
}
