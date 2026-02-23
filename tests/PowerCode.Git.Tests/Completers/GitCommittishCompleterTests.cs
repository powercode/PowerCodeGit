using System.Collections;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Tests.Completers;

[TestClass]
public sealed class GitCommittishCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };
    private static readonly DateTimeOffset FixedDate = new(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);

    // ── Commit-only completions (baseline behaviour) ──────────────────────

    [TestMethod]
    public void CompleteArgument_EmptyWord_ReturnsAllCommits()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
            CreateCommit("def5678abc1234def5678abc1234def5678abc123", "Add feature"),
        ]);
        var completer = CreateCommitOnlyCompleter(100, false, service);

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
        var completer = CreateCommitOnlyCompleter(100, false, service);

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
        var completer = CreateCommitOnlyCompleter(100, false, service);

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
        var completer = CreateCommitOnlyCompleter(100, false, service);

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
        var completer = CreateCommitOnlyCompleter(100, false, service);

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
        var completer = CreateCommitOnlyCompleter(100, false, service);

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
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("Test Author", results[0].ToolTip);
        Assert.Contains("2025-06-15", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_ListItemText_ContainsShortShaAndMessage()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("abc1234", results[0].ListItemText);
        Assert.Contains("Initial commit", results[0].ListItemText);
    }

    [TestMethod]
    public void CompleteArgument_ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitHistoryService();
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_MaxCountAndAllBranches_PassedToOptions()
    {
        var service = new CapturingGitHistoryService();
        var completer = CreateCommitOnlyCompleter(50, true, service);

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
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    // ── IncludeRelativeRefs ───────────────────────────────────────────────

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_EmptyWord_ReturnsHeadRefsAndCommits()
    {
        var historyService = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "", null!, BoundParameters).ToList();

        // 1 HEAD^ + 10 HEAD~N + 1 commit = 12
        Assert.HasCount(12, results);
        Assert.AreEqual("HEAD^", results[0].CompletionText);
        Assert.AreEqual("HEAD~1", results[1].CompletionText);
        Assert.AreEqual("HEAD~10", results[10].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_HeadPrefix_FiltersToMatching()
    {
        var historyService = new StubGitHistoryService([]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD~", null!, BoundParameters).ToList();

        Assert.HasCount(10, results);
        Assert.AreEqual("HEAD~1", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_ExactDigit_FiltersToMatchingDepths()
    {
        var historyService = new StubGitHistoryService([]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD~5", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("HEAD~5", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_CaseInsensitive_ReturnsMatches()
    {
        var historyService = new StubGitHistoryService([]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "head", null!, BoundParameters).ToList();

        // All HEAD refs match "head" (case-insensitive)
        Assert.HasCount(11, results);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_NonMatchingWord_ReturnsNoRefs()
    {
        var historyService = new StubGitHistoryService([]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "main", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_Tooltip_ContainsDescription()
    {
        var historyService = new StubGitHistoryService([]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD^", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("parent of HEAD", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_RelativeRefsDisabled_NoHeadRefs()
    {
        var historyService = new StubGitHistoryService([]);
        var completer = CreateCommitOnlyCompleter(100, false, historyService);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    // ── IncludeBranches ───────────────────────────────────────────────────

    [TestMethod]
    public void CompleteArgument_IncludeBranches_EmptyWord_ReturnsBranchesAndCommits()
    {
        var historyService = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
            CreateBranch("feature/login"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, false, false, historyService, branchService, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "", null!, BoundParameters).ToList();

        // 2 branches + 1 commit = 3
        Assert.HasCount(3, results);
    }

    [TestMethod]
    public void CompleteArgument_IncludeBranches_PrefixFilter_MatchesBranchNames()
    {
        var historyService = new StubGitHistoryService([]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main"),
            CreateBranch("feature/login"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, false, false, historyService, branchService, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "fea", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("feature/login", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_IncludeBranches_ExcludesRemoteByDefault()
    {
        var historyService = new StubGitHistoryService([]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main"),
            CreateBranch("origin/main", isRemote: true),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, false, false, historyService, branchService, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("main", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRemoteBranches_ShowsRemoteBranches()
    {
        var historyService = new StubGitHistoryService([]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main"),
            CreateBranch("origin/main", isRemote: true),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, true, false, false, historyService, branchService, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void CompleteArgument_IncludeBranches_HeadBranch_TooltipContainsStar()
    {
        var historyService = new StubGitHistoryService([]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, false, false, historyService, branchService, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("* main (HEAD)", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRemoteBranches_RemoteBranch_TooltipContainsRemote()
    {
        var historyService = new StubGitHistoryService([]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("origin/main", isRemote: true),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, true, false, false, historyService, branchService, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("Remote branch:", results[0].ToolTip);
    }

    // ── IncludeTags ───────────────────────────────────────────────────────

    [TestMethod]
    public void CompleteArgument_IncludeTags_EmptyWord_ReturnsTagsAndCommits()
    {
        var historyService = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var tagService = new StubGitTagService(
        [
            CreateTag("v1.0.0"),
            CreateTag("v2.0.0", isAnnotated: true, message: "Release 2.0"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, true, false, historyService, null, tagService);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        // 2 tags + 1 commit = 3
        Assert.HasCount(3, results);
    }

    [TestMethod]
    public void CompleteArgument_IncludeTags_PrefixFilter_MatchesTagNames()
    {
        var historyService = new StubGitHistoryService([]);
        var tagService = new StubGitTagService(
        [
            CreateTag("v1.0.0"),
            CreateTag("v2.0.0"),
            CreateTag("release-3"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, true, false, historyService, null, tagService);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "v", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void CompleteArgument_IncludeTags_AnnotatedTag_TooltipContainsMessage()
    {
        var historyService = new StubGitHistoryService([]);
        var tagService = new StubGitTagService(
        [
            CreateTag("v1.0.0", isAnnotated: true, message: "First release"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, true, false, historyService, null, tagService);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("First release", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_IncludeTags_LightweightTag_TooltipHasTagPrefix()
    {
        var historyService = new StubGitHistoryService([]);
        var tagService = new StubGitTagService(
        [
            CreateTag("v1.0.0"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, true, false, historyService, null, tagService);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("Tag: v1.0.0", results[0].ToolTip);
    }

    // ── Combined completions ──────────────────────────────────────────────

    [TestMethod]
    public void CompleteArgument_AllIncluded_OrderIsRelativeRefsThenBranchesThenTagsThenCommits()
    {
        var historyService = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "HEAD improvement"),
        ]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("HEAD-branch"),
        ]);
        var tagService = new StubGitTagService(
        [
            CreateTag("HEAD-tag"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, true, true, historyService, branchService, tagService);

        // All match "HEAD"
        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD", null!, BoundParameters).ToList();

        // Relative refs come first (HEAD^ + HEAD~1..~10 = 11), then branches (1), then tags (1), then commits (1)
        Assert.HasCount(14, results);
        Assert.AreEqual("HEAD^", results[0].CompletionText);
        Assert.AreEqual("HEAD~1", results[1].CompletionText);
        Assert.AreEqual("HEAD-branch", results[11].CompletionText);
        Assert.AreEqual("HEAD-tag", results[12].CompletionText);
        Assert.AreEqual("abc1234", results[13].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_AllIncluded_AllResultsHaveParameterValueType()
    {
        var historyService = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit"),
        ]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main"),
        ]);
        var tagService = new StubGitTagService(
        [
            CreateTag("v1.0.0"),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, true, true, historyService, branchService, tagService);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    // ── Factory helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a completer with only commit completions (baseline behaviour).
    /// </summary>
    private static GitCommittishCompleterAttribute.CommittishCompleter CreateCommitOnlyCompleter(
        int maxCount, bool allBranches, IGitHistoryService service) =>
        new(maxCount, allBranches, false, false, false, false, service, null, null);

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

    private static GitBranchInfo CreateBranch(string name, bool isHead = false, bool isRemote = false)
    {
        return new GitBranchInfo(name, isHead, isRemote, "abc1234def5678abc1234def5678abc1234def567", null, null, null);
    }

    private static GitTagInfo CreateTag(string name, bool isAnnotated = false, string? message = null)
    {
        return new GitTagInfo(name, "abc1234def5678abc1234def5678abc1234def567", isAnnotated, null, null, null, message);
    }

    // ── Stubs ─────────────────────────────────────────────────────────────

    private sealed class StubGitHistoryService(IReadOnlyList<GitCommitInfo> commits) : IGitHistoryService
    {
        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options) => commits;

        public GitCommitInfo Commit(GitCommitOptions options) =>
            throw new NotImplementedException();
    }

    private sealed class ThrowingGitHistoryService : IGitHistoryService
    {
        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options) =>
            throw new InvalidOperationException("Not a git repository");

        public GitCommitInfo Commit(GitCommitOptions options) =>
            throw new NotImplementedException();
    }

    private sealed class CapturingGitHistoryService : IGitHistoryService
    {
        public GitLogOptions? CapturedOptions { get; private set; }

        public IReadOnlyList<GitCommitInfo> GetLog(GitLogOptions options)
        {
            CapturedOptions = options;
            return [];
        }

        public GitCommitInfo Commit(GitCommitOptions options) =>
            throw new NotImplementedException();
    }

    private sealed class StubGitBranchService(IReadOnlyList<GitBranchInfo> branches) : IGitBranchService
    {
        public IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options) => branches;

        public GitBranchInfo SwitchBranch(GitSwitchOptions options) =>
            throw new NotImplementedException();

        public GitBranchInfo CreateBranch(GitBranchCreateOptions options) =>
            throw new NotImplementedException();

        public void DeleteBranch(GitBranchDeleteOptions options) =>
            throw new NotImplementedException();
    }

    private sealed class StubGitTagService(IReadOnlyList<GitTagInfo> tags) : IGitTagService
    {
        public IReadOnlyList<GitTagInfo> GetTags(GitTagListOptions options) => tags;

        public GitTagInfo CreateTag(GitTagCreateOptions options) =>
            throw new NotImplementedException();
    }
}
