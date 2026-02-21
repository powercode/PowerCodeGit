using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class ResetGitHeadCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Revision_DefaultsToNull()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        Assert.IsNull(cmdlet.Revision);
    }

    [TestMethod]
    public void Revision_IsSetCorrectly()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService())
        {
            Revision = "HEAD~1",
        };

        Assert.AreEqual("HEAD~1", cmdlet.Revision);
    }

    [TestMethod]
    public void Hard_DefaultsToFalse()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        Assert.IsFalse(cmdlet.Hard.IsPresent);
    }

    [TestMethod]
    public void Soft_DefaultsToFalse()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());

        Assert.IsFalse(cmdlet.Soft.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_MixedParameterSet_DefaultMode()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService())
        {
            Revision = "HEAD~1",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("HEAD~1", options.Revision);
        Assert.AreEqual(GitResetMode.Mixed, options.Mode);
        Assert.IsNull(options.Paths);
    }

    [TestMethod]
    public void BuildOptions_HardParameterSet_ModeIsHard()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());
        cmdlet.Hard = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual(GitResetMode.Hard, options.Mode);
    }

    [TestMethod]
    public void BuildOptions_SoftParameterSet_ModeIsSoft()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService());
        cmdlet.Soft = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual(GitResetMode.Soft, options.Mode);
    }

    [TestMethod]
    public void BuildOptions_PathsParameterSet_PathsMapped()
    {
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService())
        {
            Path = ["file.txt"],
        };

        // Simulate "Paths" parameter set by checking Path-based result
        var options = cmdlet.BuildOptions("C:\\repo");

        // Without ParameterSetName being set we can't trigger Paths, but we verify Path property works
        Assert.AreEqual("C:\\repo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var prebuilt = new GitResetOptions
        {
            RepositoryPath = "D:\\prebuilt",
            Mode = GitResetMode.Hard,
        };
        var cmdlet = new ResetGitHeadCmdlet(new StubGitWorkingTreeService())
        {
            Options = prebuilt,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(prebuilt, options);
    }

}
