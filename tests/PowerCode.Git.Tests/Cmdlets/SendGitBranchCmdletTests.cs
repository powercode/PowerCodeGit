using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class SendGitBranchCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void Remote_DefaultsToOrigin()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService());

        Assert.AreEqual("origin", cmdlet.Remote);
    }

    [TestMethod]
    public void Name_DefaultsToNull()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService());

        Assert.IsNull(cmdlet.Name);
    }

    [TestMethod]
    public void SetUpstream_DefaultsToFalse()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService());

        Assert.IsFalse(cmdlet.SetUpstream.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_DefaultSet_RemoteAndPathMapped()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual("origin", options.RemoteName);
        Assert.IsFalse(options.Force);
        Assert.IsFalse(options.Delete);
        Assert.IsFalse(options.Tags);
        Assert.IsFalse(options.All);
        Assert.IsFalse(options.DryRun);
    }

    [TestMethod]
    public void BuildOptions_ForceSet_ForceMapped()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService())
        {
            Force = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Force);
    }

    [TestMethod]
    public void BuildOptions_DeleteSet_DeleteMapped()
    {
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService())
        {
            Delete = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Delete);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefined = new GitPushOptions { RepositoryPath = "D:\\repo", Force = true };
        var cmdlet = new SendGitBranchCmdlet(new StubGitRemoteService())
        {
            Options = predefined,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(predefined, options);
    }

}
