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
        // When no ancestry commits are available, fall back to generic description.
        var historyService = new StubGitHistoryService([]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD^", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("parent of HEAD", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_WithCommits_TooltipContainsCommitShortSha()
    {
        var commits = new[]
        {
            CreateCommit("aaa0000000000000000000000000000000000000", "HEAD commit"),
            CreateCommit("bbb1111111111111111111111111111111111111", "Parent commit"),
            CreateCommit("ccc2222222222222222222222222222222222222", "Grandparent commit"),
        };
        var historyService = new StubGitHistoryService(commits);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD^", null!, BoundParameters).ToList();

        // HEAD^ tooltip should contain the parent's short SHA and message
        Assert.HasCount(1, results);
        Assert.Contains("bbb1111", results[0].ToolTip);
        Assert.Contains("Parent commit", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_WithCommits_Head1TooltipContainsCommitInfo()
    {
        var commits = new[]
        {
            CreateCommit("aaa0000000000000000000000000000000000000", "HEAD commit"),
            CreateCommit("bbb1111111111111111111111111111111111111", "Parent commit"),
            CreateCommit("ccc2222222222222222222222222222222222222", "Grandparent commit"),
        };
        var historyService = new StubGitHistoryService(commits);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD~1", null!, BoundParameters).ToList();

        // "HEAD~1" is a prefix of both "HEAD~1" and "HEAD~10" so multiple results are expected.
        // Verify specifically that the HEAD~1 entry carries the resolved commit info.
        var head1 = results.Single(r => r.CompletionText == "HEAD~1");
        Assert.Contains("bbb1111", head1.ToolTip);
        Assert.Contains("Parent commit", head1.ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_IncludeRelativeRefs_WithCommits_OutOfRangeDepth_FallsBackToDescription()
    {
        // Only 2 commits (HEAD + one ancestor); HEAD~2 has no resolved commit.
        var commits = new[]
        {
            CreateCommit("aaa0000000000000000000000000000000000000", "HEAD commit"),
            CreateCommit("bbb1111111111111111111111111111111111111", "Parent commit"),
        };
        var historyService = new StubGitHistoryService(commits);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, false, true, historyService, null, null);

        var results = completer.CompleteArgument("Start-GitRebase", "Upstream", "HEAD~2", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.Contains("HEAD~2", results[0].ToolTip);
        Assert.Contains("2 commits before HEAD", results[0].ToolTip);
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

    // ── Decoration / logical-name completions ─────────────────────────────

    [TestMethod]
    public void CompleteArgument_CommitWithLocalBranchDecoration_NoBranchCompleter_EmitsBranchName()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Initial commit",
                [new GitDecoration("main", GitDecorationType.LocalBranch)]),
        ]);
        // includeBranches = false  ⇒ logical name is NOT covered elsewhere
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("main", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CommitWithLocalBranchDecoration_WithBranchCompleter_NoDuplicate()
    {
        var sha = "abc1234def5678abc1234def5678abc1234def567";
        var historyService = new StubGitHistoryService(
        [
            CreateCommit(sha, "Initial commit",
                [new GitDecoration("main", GitDecorationType.LocalBranch)]),
        ]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
        ]);
        // includeBranches = true  ⇒ "main" is already offered by GetBranchCompletions
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, false, false, historyService, branchService, null);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        // Only "main" from the branch completer; the commit section must not add another entry
        Assert.HasCount(1, results);
        Assert.AreEqual("main", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CommitWithLocalBranchDecoration_WithBranchCompleter_ShaPrefixTyped_EmitsSha()
    {
        var sha = "abc1234def5678abc1234def5678abc1234def567";
        var historyService = new StubGitHistoryService(
        [
            CreateCommit(sha, "Initial commit",
                [new GitDecoration("main", GitDecorationType.LocalBranch)]),
        ]);
        var branchService = new StubGitBranchService(
        [
            CreateBranch("main", isHead: true),
        ]);
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, false, false, historyService, branchService, null);

        // User typed "abc" — a SHA prefix; the branch completer won't match so the
        // commit section must fall back to the raw SHA.
        var results = completer.CompleteArgument("Get-GitLog", "Commit", "abc", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("abc1234", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CommitWithHeadDecoration_AlwaysEmitsHead()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Latest",
                [new GitDecoration("HEAD", GitDecorationType.Head)]),
        ]);
        // No branch/tag completers enabled
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("HEAD", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CommitWithHeadAndBranchDecoration_BranchCompleterEnabled_EmitsHead()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Latest",
                [
                    new GitDecoration("HEAD", GitDecorationType.Head),
                    new GitDecoration("main", GitDecorationType.LocalBranch),
                ]),
        ]);
        var branchService = new StubGitBranchService([CreateBranch("main", isHead: true)]);
        // includeBranches = true covers "main", but HEAD has no dedicated completer
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, true, false, false, false, service, branchService, null);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        // "main" from branch completer + "HEAD" from commit completer
        Assert.HasCount(2, results);
        Assert.IsTrue(results.Any(r => r.CompletionText == "HEAD"));
        Assert.IsTrue(results.Any(r => r.CompletionText == "main"));
    }

    [TestMethod]
    public void CompleteArgument_CommitWithTagDecoration_NoTagCompleter_EmitsTagName()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Release",
                [new GitDecoration("v1.0.0", GitDecorationType.Tag)]),
        ]);
        // includeTags = false  ⇒ logical name is NOT covered elsewhere
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("v1.0.0", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CommitWithTagDecoration_WithTagCompleter_NoDuplicate()
    {
        var historyService = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Release",
                [new GitDecoration("v1.0.0", GitDecorationType.Tag)]),
        ]);
        var tagService = new StubGitTagService([CreateTag("v1.0.0")]);
        // includeTags = true  ⇒ "v1.0.0" is already offered by GetTagCompletions
        var completer = new GitCommittishCompleterAttribute.CommittishCompleter(
            100, false, false, false, true, false, historyService, null, tagService);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        // Only the tag completer entry; commit section must not add another
        Assert.HasCount(1, results);
        Assert.AreEqual("v1.0.0", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CommitWithMultipleDecorations_NoExternalCompleters_EmitsAllNames()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Merge",
                [
                    new GitDecoration("HEAD", GitDecorationType.Head),
                    new GitDecoration("main", GitDecorationType.LocalBranch),
                    new GitDecoration("v2.0.0", GitDecorationType.Tag),
                ]),
        ]);
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "", null!, BoundParameters).ToList();

        Assert.HasCount(3, results);
        Assert.IsTrue(results.Any(r => r.CompletionText == "HEAD"));
        Assert.IsTrue(results.Any(r => r.CompletionText == "main"));
        Assert.IsTrue(results.Any(r => r.CompletionText == "v2.0.0"));
    }

    [TestMethod]
    public void CompleteArgument_DecoratedCommit_LogicalNamePrefixFilter_OnlyMatchingNamesEmitted()
    {
        var service = new StubGitHistoryService(
        [
            CreateCommit("abc1234def5678abc1234def5678abc1234def567", "Latest",
                [
                    new GitDecoration("HEAD", GitDecorationType.Head),
                    new GitDecoration("main", GitDecorationType.LocalBranch),
                ]),
        ]);
        var completer = CreateCommitOnlyCompleter(100, false, service);

        var results = completer.CompleteArgument("Get-GitLog", "Commit", "ma", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("main", results[0].CompletionText);
    }

    // ── Factory helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a completer with only commit completions (baseline behaviour).
    /// </summary>
    private static GitCommittishCompleterAttribute.CommittishCompleter CreateCommitOnlyCompleter(
        int maxCount, bool allBranches, IGitHistoryService service) =>
        new(maxCount, allBranches, false, false, false, false, service, null, null);

    private static GitCommitInfo CreateCommit(string sha, string message,
        IReadOnlyList<GitDecoration>? decorations = null)
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
            [],
            decorations);
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

        public GitBranchInfo SetBranch(GitBranchSetOptions options) =>
            throw new NotImplementedException();
    }

    private sealed class StubGitTagService(IReadOnlyList<GitTagInfo> tags) : IGitTagService
    {
        public IReadOnlyList<GitTagInfo> GetTags(GitTagListOptions options) => tags;

        public GitTagInfo CreateTag(GitTagCreateOptions options) =>
            throw new NotImplementedException();

        public void DeleteTag(GitTagDeleteOptions options) =>
            throw new NotImplementedException();
    }
}
