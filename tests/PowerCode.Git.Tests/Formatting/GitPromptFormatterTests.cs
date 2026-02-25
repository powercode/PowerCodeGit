using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Formatting;

namespace PowerCode.Git.Tests.Formatting;

[TestClass]
public sealed class GitPromptFormatterTests
{
    private const string Esc = "\x1b";
    private const string Reset = $"{Esc}[0m";
    private const string BoldGreen = $"{Esc}[1;32m";
    private const string BoldYellow = $"{Esc}[1;33m";
    private const string BoldCyan = $"{Esc}[1;36m";
    private const string BoldRed = $"{Esc}[1;31m";
    private const string Green = $"{Esc}[32m";
    private const string Red = $"{Esc}[31m";
    private const string Dim = $"{Esc}[2m";
    private const string Yellow = $"{Esc}[33m";
    private const string ArrowUp = "\u2191";
    private const string ArrowDown = "\u2193";
    private const string StashFlag = "\u2691";
    private const string GlyphGit = "\ue702";
    private const string GlyphGitHub = "\uf09b";
    private const string GlyphGitLab = "\uf296";
    private const string GlyphBitbucket = "\uf171";
    private const string GlyphAzureDevOps = "\uebd8";

    // ── Provider detection ───────────────────────────────────────────────────

    [TestMethod]
    public void DetectProvider_NullUrl_ReturnsUnknown()
    {
        var result = GitPromptFormatter.DetectProvider(null);

        Assert.AreEqual(GitUpstreamProvider.Unknown, result);
    }

    [TestMethod]
    public void DetectProvider_EmptyUrl_ReturnsUnknown()
    {
        var result = GitPromptFormatter.DetectProvider(string.Empty);

        Assert.AreEqual(GitUpstreamProvider.Unknown, result);
    }

    [TestMethod]
    public void DetectProvider_GitHubHttps_ReturnsGitHub()
    {
        var result = GitPromptFormatter.DetectProvider("https://github.com/user/repo.git");

        Assert.AreEqual(GitUpstreamProvider.GitHub, result);
    }

    [TestMethod]
    public void DetectProvider_GitHubSsh_ReturnsGitHub()
    {
        var result = GitPromptFormatter.DetectProvider("git@github.com:user/repo.git");

        Assert.AreEqual(GitUpstreamProvider.GitHub, result);
    }

    [TestMethod]
    public void DetectProvider_GitLabHttps_ReturnsGitLab()
    {
        var result = GitPromptFormatter.DetectProvider("https://gitlab.com/user/repo.git");

        Assert.AreEqual(GitUpstreamProvider.GitLab, result);
    }

    [TestMethod]
    public void DetectProvider_GitLabSsh_ReturnsGitLab()
    {
        var result = GitPromptFormatter.DetectProvider("git@gitlab.com:user/repo.git");

        Assert.AreEqual(GitUpstreamProvider.GitLab, result);
    }

    [TestMethod]
    public void DetectProvider_SelfHostedGitLab_ReturnsGitLab()
    {
        var result = GitPromptFormatter.DetectProvider("https://gitlab.mycompany.com/project/repo");

        Assert.AreEqual(GitUpstreamProvider.GitLab, result);
    }

    [TestMethod]
    public void DetectProvider_BitbucketHttps_ReturnsBitbucket()
    {
        var result = GitPromptFormatter.DetectProvider("https://bitbucket.org/user/repo.git");

        Assert.AreEqual(GitUpstreamProvider.Bitbucket, result);
    }

    [TestMethod]
    public void DetectProvider_BitbucketSsh_ReturnsBitbucket()
    {
        var result = GitPromptFormatter.DetectProvider("git@bitbucket.org:user/repo.git");

        Assert.AreEqual(GitUpstreamProvider.Bitbucket, result);
    }

    [TestMethod]
    public void DetectProvider_AzureDevOpsHttps_ReturnsAzureDevOps()
    {
        var result = GitPromptFormatter.DetectProvider("https://dev.azure.com/org/project/_git/repo");

        Assert.AreEqual(GitUpstreamProvider.AzureDevOps, result);
    }

    [TestMethod]
    public void DetectProvider_VisualStudioCom_ReturnsAzureDevOps()
    {
        var result = GitPromptFormatter.DetectProvider("https://org.visualstudio.com/project/_git/repo");

        Assert.AreEqual(GitUpstreamProvider.AzureDevOps, result);
    }

    [TestMethod]
    public void DetectProvider_UnknownHost_ReturnsUnknown()
    {
        var result = GitPromptFormatter.DetectProvider("https://mygitserver.internal/user/repo.git");

        Assert.AreEqual(GitUpstreamProvider.Unknown, result);
    }

    // ── GetProviderIcon ──────────────────────────────────────────────────────

    [TestMethod]
    public void GetProviderIcon_Unknown_ReturnsGitGlyph()
    {
        Assert.AreEqual(GlyphGit, GitPromptFormatter.GetProviderIcon(GitUpstreamProvider.Unknown));
    }

    [TestMethod]
    public void GetProviderIcon_GitHub_ReturnsGitHubGlyph()
    {
        Assert.AreEqual(GlyphGitHub, GitPromptFormatter.GetProviderIcon(GitUpstreamProvider.GitHub));
    }

    [TestMethod]
    public void GetProviderIcon_GitLab_ReturnsGitLabGlyph()
    {
        Assert.AreEqual(GlyphGitLab, GitPromptFormatter.GetProviderIcon(GitUpstreamProvider.GitLab));
    }

    [TestMethod]
    public void GetProviderIcon_Bitbucket_ReturnsBitbucketGlyph()
    {
        Assert.AreEqual(GlyphBitbucket, GitPromptFormatter.GetProviderIcon(GitUpstreamProvider.Bitbucket));
    }

    [TestMethod]
    public void GetProviderIcon_AzureDevOps_ReturnsAzureGlyph()
    {
        Assert.AreEqual(GlyphAzureDevOps, GitPromptFormatter.GetProviderIcon(GitUpstreamProvider.AzureDevOps));
    }

    // ── Format — basic output ────────────────────────────────────────────────

    [TestMethod]
    public void Format_CleanRepoGitHubUpstream_ReturnsBoldGreenIconAndBranch()
    {
        var status = MakeStatus(branch: "main", provider: GitUpstreamProvider.GitHub);

        var result = GitPromptFormatter.Format(status);

        Assert.AreEqual(
            $"{BoldGreen}{GlyphGitHub}{Reset} {BoldGreen}main{Reset}",
            result);
    }

    [TestMethod]
    public void Format_DirtyRepo_ReturnsBoldYellowIconAndBranch()
    {
        var status = MakeStatus(branch: "main", provider: GitUpstreamProvider.GitHub, stagedCount: 1);

        var result = GitPromptFormatter.Format(status);

        StringAssert.StartsWith(result, $"{BoldYellow}{GlyphGitHub}{Reset} {BoldYellow}main{Reset}");
    }

    [TestMethod]
    public void Format_DetachedHead_WrapsBranchInParentheses()
    {
        var status = MakeStatus(branch: "abc1234", isDetachedHead: true);

        var result = GitPromptFormatter.Format(status, noColor: true);

        StringAssert.Contains(result, "(abc1234)");
    }

    [TestMethod]
    public void Format_WithAheadBehind_IncludesArrows()
    {
        var status = MakeStatus(
            branch: "main",
            trackedBranchName: "origin/main",
            aheadBy: 2,
            behindBy: 1);

        var result = GitPromptFormatter.Format(status, noColor: true);

        StringAssert.Contains(result, $"{ArrowUp}2");
        StringAssert.Contains(result, $"{ArrowDown}1");
    }

    [TestMethod]
    public void Format_AheadByZero_DoesNotIncludeUpArrow()
    {
        var status = MakeStatus(
            branch: "main",
            trackedBranchName: "origin/main",
            aheadBy: 0,
            behindBy: 0);

        var result = GitPromptFormatter.Format(status, noColor: true);

        Assert.DoesNotContain(result, ArrowUp);
        Assert.DoesNotContain(result, ArrowDown);
    }

    [TestMethod]
    public void Format_WithWorkingTreeCounts_IncludesPlusAndTildeAndQuestion()
    {
        var status = MakeStatus(
            branch: "main",
            stagedCount: 3,
            modifiedCount: 2,
            untrackedCount: 1);

        var result = GitPromptFormatter.Format(status, noColor: true);

        StringAssert.Contains(result, "+3");
        StringAssert.Contains(result, "~2");
        StringAssert.Contains(result, "?1");
    }

    [TestMethod]
    public void Format_WithStash_IncludesStashGlyph()
    {
        var status = MakeStatus(branch: "main", stashCount: 2);

        var result = GitPromptFormatter.Format(status, noColor: true);

        StringAssert.Contains(result, $"{StashFlag}2");
    }

    [TestMethod]
    public void Format_ZeroStash_OmitsStashGlyph()
    {
        var status = MakeStatus(branch: "main", stashCount: 0);

        var result = GitPromptFormatter.Format(status, noColor: true);

        Assert.DoesNotContain(result, StashFlag);
    }

    // ── Format — noColor ─────────────────────────────────────────────────────

    [TestMethod]
    public void Format_NoColor_ContainsNoAnsiEscapes()
    {
        var status = MakeStatus(
            branch: "main",
            provider: GitUpstreamProvider.GitHub,
            trackedBranchName: "origin/main",
            aheadBy: 1,
            behindBy: 1,
            stagedCount: 2,
            modifiedCount: 1,
            untrackedCount: 3,
            stashCount: 1);

        var result = GitPromptFormatter.Format(status, noColor: true);

        Assert.DoesNotContain(result, Esc);
    }

    // ── Format — hideUpstream ────────────────────────────────────────────────

    [TestMethod]
    public void Format_HideUpstream_OmitsIconAndAheadBehind()
    {
        var status = MakeStatus(
            branch: "main",
            provider: GitUpstreamProvider.GitHub,
            trackedBranchName: "origin/main",
            aheadBy: 2,
            behindBy: 1);

        var result = GitPromptFormatter.Format(status, noColor: true, hideUpstream: true);

        Assert.DoesNotContain(result, GlyphGitHub);
        Assert.DoesNotContain(result, ArrowUp);
        Assert.DoesNotContain(result, ArrowDown);
        StringAssert.Contains(result, "main");
    }

    // ── Format — hideCounts ──────────────────────────────────────────────────

    [TestMethod]
    public void Format_HideCounts_OmitsFileCountSegments()
    {
        var status = MakeStatus(
            branch: "main",
            stagedCount: 3,
            modifiedCount: 2,
            untrackedCount: 1);

        var result = GitPromptFormatter.Format(status, noColor: true, hideCounts: true);

        Assert.DoesNotContain(result, "+3");
        Assert.DoesNotContain(result, "~2");
        Assert.DoesNotContain(result, "?1");
    }

    // ── Format — hideStash ───────────────────────────────────────────────────

    [TestMethod]
    public void Format_HideStash_OmitsStashSegment()
    {
        var status = MakeStatus(branch: "main", stashCount: 3);

        var result = GitPromptFormatter.Format(status, noColor: true, hideStash: true);

        Assert.DoesNotContain(result, StashFlag);
    }

    // ── Format — color coding ────────────────────────────────────────────────

    [TestMethod]
    public void Format_StagedCount_IsGreen()
    {
        var status = MakeStatus(branch: "main", stagedCount: 2);

        var result = GitPromptFormatter.Format(status);

        StringAssert.Contains(result, $"{Green}+2{Reset}");
    }

    [TestMethod]
    public void Format_ModifiedCount_IsRed()
    {
        var status = MakeStatus(branch: "main", modifiedCount: 1);

        var result = GitPromptFormatter.Format(status);

        StringAssert.Contains(result, $"{Red}~1{Reset}");
    }

    [TestMethod]
    public void Format_UntrackedCount_IsDim()
    {
        var status = MakeStatus(branch: "main", untrackedCount: 4);

        var result = GitPromptFormatter.Format(status);

        StringAssert.Contains(result, $"{Dim}?4{Reset}");
    }

    [TestMethod]
    public void Format_StashCount_IsYellow()
    {
        var status = MakeStatus(branch: "main", stashCount: 1);

        var result = GitPromptFormatter.Format(status);

        StringAssert.Contains(result, $"{Yellow}{StashFlag}1{Reset}");
    }

    [TestMethod]
    public void Format_AheadCount_IsBoldCyan()
    {
        var status = MakeStatus(
            branch: "main",
            trackedBranchName: "origin/main",
            aheadBy: 3,
            behindBy: 0);

        var result = GitPromptFormatter.Format(status);

        StringAssert.Contains(result, $"{BoldCyan}{ArrowUp}3{Reset}");
    }

    [TestMethod]
    public void Format_BehindCount_IsBoldRed()
    {
        var status = MakeStatus(
            branch: "main",
            trackedBranchName: "origin/main",
            aheadBy: 0,
            behindBy: 2);

        var result = GitPromptFormatter.Format(status);

        StringAssert.Contains(result, $"{BoldRed}{ArrowDown}2{Reset}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GitPromptStatus MakeStatus(
        string branch = "main",
        GitUpstreamProvider provider = GitUpstreamProvider.Unknown,
        string? trackedBranchName = null,
        int? aheadBy = null,
        int? behindBy = null,
        int stagedCount = 0,
        int modifiedCount = 0,
        int untrackedCount = 0,
        int stashCount = 0,
        bool isDetachedHead = false) =>
        new(
            repositoryPath: "C:\\repo",
            branchName: branch,
            upstreamProvider: provider,
            trackedBranchName: trackedBranchName,
            aheadBy: aheadBy,
            behindBy: behindBy,
            stagedCount: stagedCount,
            modifiedCount: modifiedCount,
            untrackedCount: untrackedCount,
            stashCount: stashCount,
            isDetachedHead: isDetachedHead);
}
