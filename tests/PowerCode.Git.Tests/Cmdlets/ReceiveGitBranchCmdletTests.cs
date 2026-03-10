using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class ReceiveGitBranchCmdletTests
{
    private static ReceiveGitBranchCmdlet MakeCmdlet() =>
        new(new StubGitRemoteService(), new StubGitBranchService());

    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = MakeCmdlet();

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = MakeCmdlet();
        cmdlet.RepoPath = "D:\\other-repo";

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void MergeStrategy_DefaultsToMerge()
    {
        var cmdlet = MakeCmdlet();

        Assert.AreEqual(GitMergeStrategy.Merge, cmdlet.MergeStrategy);
    }

    [TestMethod]
    public void MergeStrategy_IsSetCorrectly()
    {
        var cmdlet = MakeCmdlet();
        cmdlet.MergeStrategy = GitMergeStrategy.FastForward;

        Assert.AreEqual(GitMergeStrategy.FastForward, cmdlet.MergeStrategy);
    }

    [TestMethod]
    public void Prune_DefaultsToFalse()
    {
        var cmdlet = MakeCmdlet();

        Assert.IsFalse(cmdlet.Prune.IsPresent);
    }

    [TestMethod]
    public void BuildOptions_DefaultSet_MergeStrategyAndPathMapped()
    {
        var cmdlet = MakeCmdlet();

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.AreEqual(GitMergeStrategy.Merge, options.MergeStrategy);
        Assert.IsFalse(options.AutoStash);
        Assert.IsNull(options.Tags);
    }

    [TestMethod]
    public void BuildOptions_AutoStashSet_AutoStashMapped()
    {
        var cmdlet = MakeCmdlet();
        cmdlet.AutoStash = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.AutoStash);
    }

    [TestMethod]
    public void BuildOptions_TagsSet_TagsTrueInOptions()
    {
        var cmdlet = MakeCmdlet();
        cmdlet.Tags = new System.Management.Automation.SwitchParameter(true);

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.Tags);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefined = new GitPullOptions { RepositoryPath = "D:\\repo", AutoStash = true };
        var cmdlet = MakeCmdlet();
        cmdlet.Options = predefined;

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(predefined, options);
    }

    [TestMethod]
    public void Action_DefaultsToCreate()
    {
        var cmdlet = MakeCmdlet();

        Assert.AreEqual(ReceiveBranchAction.Create, cmdlet.Action);
    }

    [TestMethod]
    public void Action_CanBeSetToCreateOrUpdate()
    {
        var cmdlet = MakeCmdlet();
        cmdlet.Action = ReceiveBranchAction.CreateOrUpdate;

        Assert.AreEqual(ReceiveBranchAction.CreateOrUpdate, cmdlet.Action);
    }

    [TestMethod]
    public void Action_CanBeSetToUpdateOnly()
    {
        var cmdlet = MakeCmdlet();
        cmdlet.Action = ReceiveBranchAction.UpdateOnly;

        Assert.AreEqual(ReceiveBranchAction.UpdateOnly, cmdlet.Action);
    }

    [TestMethod]
    public void InputBranch_LocalName_StripsRemotePrefix()
    {
        // Confirm that a piped GitBranchInfo from Get-GitBranch -Remote carries LocalName
        var remoteBranch = new GitBranchInfo(
            "origin/feature/xyz", isHead: false, isRemote: true,
            tipSha: new string('a', 40), trackedBranchName: null,
            aheadBy: null, behindBy: null);

        Assert.AreEqual("feature/xyz", remoteBranch.LocalName);
    }
}

