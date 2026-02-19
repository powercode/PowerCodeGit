using System.Collections;
using System.Management.Automation;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;
using PowerGit.Completers;

namespace PowerGit.Tests.Completers;

[TestClass]
public sealed class GitRemoteCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestMethod]
    public void CompleteArgument_EmptyWord_ReturnsAllRemotes()
    {
        var service = new StubGitRemoteService(
        [
            CreateRemote("origin", "https://github.com/user/repo.git"),
            CreateRemote("upstream", "https://github.com/org/repo.git"),
        ]);
        var completer = new GitRemoteCompleterAttribute.RemoteCompleter(service);

        var results = completer.CompleteArgument("Get-GitLog", "Remote", "", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void CompleteArgument_PrefixFilter_ReturnsOnlyMatching()
    {
        var service = new StubGitRemoteService(
        [
            CreateRemote("origin", "https://github.com/user/repo.git"),
            CreateRemote("upstream", "https://github.com/org/repo.git"),
        ]);
        var completer = new GitRemoteCompleterAttribute.RemoteCompleter(service);

        var results = completer.CompleteArgument("Get-GitLog", "Remote", "up", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("upstream", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CaseInsensitiveMatch_ReturnsMatching()
    {
        var service = new StubGitRemoteService(
        [
            CreateRemote("Origin", "https://github.com/user/repo.git"),
        ]);
        var completer = new GitRemoteCompleterAttribute.RemoteCompleter(service);

        var results = completer.CompleteArgument("Get-GitLog", "Remote", "origin", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Origin", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_NoMatch_ReturnsEmpty()
    {
        var service = new StubGitRemoteService(
        [
            CreateRemote("origin", "https://github.com/user/repo.git"),
        ]);
        var completer = new GitRemoteCompleterAttribute.RemoteCompleter(service);

        var results = completer.CompleteArgument("Get-GitLog", "Remote", "xyz", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_Tooltip_ContainsFetchUrl()
    {
        var service = new StubGitRemoteService(
        [
            CreateRemote("origin", "https://github.com/user/repo.git"),
        ]);
        var completer = new GitRemoteCompleterAttribute.RemoteCompleter(service);

        var results = completer.CompleteArgument("Get-GitLog", "Remote", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("https://github.com/user/repo.git", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitRemoteService();
        var completer = new GitRemoteCompleterAttribute.RemoteCompleter(service);

        var results = completer.CompleteArgument("Get-GitLog", "Remote", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_AllResults_HaveParameterValueType()
    {
        var service = new StubGitRemoteService(
        [
            CreateRemote("origin", "https://github.com/user/repo.git"),
            CreateRemote("upstream", "https://github.com/org/repo.git"),
        ]);
        var completer = new GitRemoteCompleterAttribute.RemoteCompleter(service);

        var results = completer.CompleteArgument("Get-GitLog", "Remote", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    private static GitRemoteInfo CreateRemote(string name, string url)
    {
        return new GitRemoteInfo(name, url, url);
    }

    private sealed class StubGitRemoteService(IReadOnlyList<GitRemoteInfo> remotes) : IGitRemoteService
    {
        public IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath) => remotes;
    }

    private sealed class ThrowingGitRemoteService : IGitRemoteService
    {
        public IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath) =>
            throw new InvalidOperationException("Not a git repository");
    }
}
