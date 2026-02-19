using System.Collections;
using System.Management.Automation;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;
using PowerGit.Completers;

namespace PowerGit.Tests.Completers;

[TestClass]
public sealed class GitCommittishCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };
    private static readonly DateTimeOffset FixedDate = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void CompleteArgument_EmptyWord_ReturnsAllCommits()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
            CreateCommit("def5678abc1234def5678abc1234def5678abc123", "Add feature"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void CompleteArgument_ShortShaPrefix_ReturnsMatchingCommit()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
            CreateCommit("def5678abc1234def5678abc1234def5678abc123", "Add feature"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "abc", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("abc1234", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_FullShaPrefix_ReturnsMatchingCommit()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "abc1234def", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
    }

    [TestMethod]
    public void CompleteArgument_MessageSubstring_ReturnsMatchingCommit()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
            CreateCommit("def5678abc1234def5678abc1234def5678abc123", "Add feature"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "feature", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("def5678", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CaseInsensitiveMessageMatch_ReturnsMatching()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Fix Bug"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "fix bug", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
    }

    [TestMethod]
    public void CompleteArgument_NoMatch_ReturnsEmpty()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "zzz", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_ResultTooltip_ContainsAuthorAndDate()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        StringAssert.Contains(results[0].ToolTip, "Test Author");
        StringAssert.Contains(results[0].ToolTip, "2025-06-15");
    }

    [TestMethod]
    public void CompleteArgument_ListItemText_ContainsShortShaAndMessage()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        StringAssert.Contains(results[0].ListItemText, "abc1234");
        StringAssert.Contains(results[0].ListItemText, "Initial commit");
    }

    [TestMethod]
    public void CompleteArgument_ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitHistoryService();
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_MaxCountAndAllBranches_PassedToOptions()
    {
        var service = new CapturingGitHistoryService();
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(50, true, service);

        completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.IsNotNull(service.CapturedOptions);
        Assert.AreEqual(50, service.CapturedOptions.MaxCount);
        Assert.IsTrue(service.CapturedOptions.AllBranches);
        Assert.AreEqual("C:\\repo", service.CapturedOptions.RepositoryPath);
    }

    [TestMethod]
    public void CompleteArgument_AllResults_HaveParameterValueType()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
            CreateCommit("def5678abc1234def5678abc1234def5678abc123", "Add feature"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    private static GitCommitInfo CreateCommit(string sha, string message)
    {
        return new GitCommitInfo(
            sha,
            "Test Author",
            "test@example.com",
            FixedDate,
            "Test Committer",
            "committer@example.com",
            FixedDate,
            message,
            message,
            []);
    }

    private sealed class StubGitHistoryService(IReadOnlyList<GitCommitInfo> commits) : IGitHistoryService
    {
        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options) => commits;
    }

    private sealed class ThrowingGitHistoryService : IGitHistoryService
    {
        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options) =>
            throw new InvalidOperationException("Not a git repository");
    }

    private sealed class CapturingGitHistoryService : IGitHistoryService
    {
        public GitLogOptions? CapturedOptions { get; private set; }

        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options)
        {
            CapturedOptions = options;
            return [];
        }
    }
}
