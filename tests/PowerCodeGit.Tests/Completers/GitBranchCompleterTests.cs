using System.Collections;
using System.Management.Automation;
using PowerCodeGit.Abstractions.Models;
using PowerCodeGit.Abstractions.Services;
using PowerCodeGit.Completers;

namespace PowerCodeGit.Tests.Completers;

[TestClass]
public sealed class GitBranchCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestMethod]
    public void CompleteArgument_EmptyWord_ReturnsAllLocalBranches()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
            CreateBranch("develop"),
            CreateBranch("feature/login"),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "", null!, BoundParameters).ToList();

        Assert.HasCount(3, results);
    }

    [TestMethod]
    public void CompleteArgument_PrefixFilter_ReturnsOnlyMatching()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
            CreateBranch("develop"),
            CreateBranch("feature/login"),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "feat", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("feature/login", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CaseInsensitiveMatch_ReturnsMatching()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("Main", isHead: true),
            CreateBranch("develop"),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "main", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Main", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRemoteFalse_ExcludesRemoteBranches()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
            CreateBranch("origin/main", isRemote: true),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("main", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRemoteTrue_IncludesRemoteBranches()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
            CreateBranch("origin/main", isRemote: true),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: true, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void CompleteArgument_HeadBranch_TooltipContainsHeadMarker()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        StringAssert.Contains(results[0].ToolTip, "HEAD");
    }

    [TestMethod]
    public void CompleteArgument_RemoteBranch_TooltipContainsRemotePrefix()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("origin/main", isRemote: true),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: true, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        StringAssert.Contains(results[0].ToolTip, "Remote");
    }

    [TestMethod]
    public void CompleteArgument_NoMatchingBranches_ReturnsEmpty()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "xyz", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitBranchService();
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_AllResults_HaveParameterValueType()
    {
        var service = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
            CreateBranch("develop"),
        ]);
        var completer = new GitBranchCompleterAttribute.BranchCompleter(includeRemote: false, service);

        var results = completer.CompleteArgument("Get-GitBranch", "Name", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    private static GitBranchInfo CreateBranch(string name, bool isHead = false, bool isRemote = false)
    {
        return new GitBranchInfo(name, isHead, isRemote, "abc1234def5678abc1234def5678abc1234def567", null, null, null);
    }

    private sealed class StubGitBranchService(IReadOnlyList<GitBranchInfo> branches) : IGitBranchService
    {
        public IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath) => branches;

        public GitBranchInfo SwitchBranch(string repositoryPath, string branchName) =>
            throw new NotImplementedException();
    }

    private sealed class ThrowingGitBranchService : IGitBranchService
    {
        public IReadOnlyList<GitBranchInfo> GetBranches(string repositoryPath) =>
            throw new InvalidOperationException("Not a git repository");

        public GitBranchInfo SwitchBranch(string repositoryPath, string branchName) =>
            throw new NotImplementedException();
    }
}
