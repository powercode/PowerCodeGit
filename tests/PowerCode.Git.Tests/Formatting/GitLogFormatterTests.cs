using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Formatting;

namespace PowerCode.Git.Tests.Formatting;

[TestClass]
public sealed class GitLogFormatterTests
{
    private const string Esc = "\x1b";

    [TestMethod]
    public void FormatShortSha_ReturnsYellowColoredSha()
    {
        var result = GitLogFormatter.FormatShortSha("abc1234");

        Assert.AreEqual($"{Esc}[33mabc1234{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatFullSha_ReturnsYellowColoredSha()
    {
        var sha = "abc1234567890def1234567890abcdef12345678";

        var result = GitLogFormatter.FormatFullSha(sha);

        Assert.AreEqual($"{Esc}[33m{sha}{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatDecorations_NullDecorations_ReturnsEmpty()
    {
        var result = GitLogFormatter.FormatDecorations(null);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatDecorations_EmptyList_ReturnsEmpty()
    {
        var result = GitLogFormatter.FormatDecorations([]);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatDecorations_HeadAndLocalBranch_ShowsHeadArrowBranch()
    {
        var decorations = new[]
        {
            new GitDecoration("HEAD", GitDecorationType.Head),
            new GitDecoration("main", GitDecorationType.LocalBranch),
        };

        var result = GitLogFormatter.FormatDecorations(decorations);

        // Should contain HEAD -> main pattern
        Assert.Contains("HEAD", result);
        Assert.Contains(" -> ", result);
        Assert.Contains("main", result);
        // Verify HEAD is bold cyan
        Assert.Contains($"{Esc}[1;36mHEAD{Esc}[0m", result);
        // Verify branch is bold green
        Assert.Contains($"{Esc}[1;32mmain{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatDecorations_DetachedHead_ShowsHeadAlone()
    {
        var decorations = new[]
        {
            new GitDecoration("HEAD", GitDecorationType.Head),
        };

        var result = GitLogFormatter.FormatDecorations(decorations);

        Assert.Contains($"{Esc}[1;36mHEAD{Esc}[0m", result);
        // Should not contain arrow
        Assert.DoesNotContain(result, "->", "Detached HEAD should not contain arrow");
    }

    [TestMethod]
    public void FormatDecorations_TagOnly_ShowsBoldYellowTag()
    {
        var decorations = new[]
        {
            new GitDecoration("tag: v1.0", GitDecorationType.Tag),
        };

        var result = GitLogFormatter.FormatDecorations(decorations);

        Assert.Contains($"{Esc}[1;33mtag: v1.0{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatDecorations_RemoteBranch_ShowsBoldRed()
    {
        var decorations = new[]
        {
            new GitDecoration("origin/main", GitDecorationType.RemoteBranch),
        };

        var result = GitLogFormatter.FormatDecorations(decorations);

        Assert.Contains($"{Esc}[1;31morigin/main{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatDecorations_MultipleDecorations_SeparatedByCommas()
    {
        var decorations = new[]
        {
            new GitDecoration("HEAD", GitDecorationType.Head),
            new GitDecoration("main", GitDecorationType.LocalBranch),
            new GitDecoration("origin/main", GitDecorationType.RemoteBranch),
            new GitDecoration("tag: v1.0", GitDecorationType.Tag),
        };

        var result = GitLogFormatter.FormatDecorations(decorations);

        // Should have opening and closing parens
        StringAssert.StartsWith(result, $"{Esc}[33m({Esc}[0m");
        StringAssert.EndsWith(result, $"{Esc}[33m){Esc}[0m");
        // Should contain all decorations
        Assert.Contains("HEAD", result);
        Assert.Contains("main", result);
        Assert.Contains("origin/main", result);
        Assert.Contains("tag: v1.0", result);
    }

    [TestMethod]
    public void FormatOneline_WithDecorations_IncludesAllParts()
    {
        var commit = new GitCommitInfo(
            "abc1234567890def1234567890abcdef12345678",
            "Author",
            "author@example.com",
            DateTimeOffset.Now,
            "Committer",
            "committer@example.com",
            DateTimeOffset.Now,
            "Initial commit",
            "Initial commit\n\nFull body",
            [],
            [new GitDecoration("main", GitDecorationType.LocalBranch)]);

        var result = GitLogFormatter.FormatOneline(commit);

        // Should contain yellow SHA
        Assert.Contains($"{Esc}[33mabc1234{Esc}[0m", result);
        // Should contain decoration parens with branch
        Assert.Contains("main", result);
        // Should contain the message
        Assert.Contains("Initial commit", result);
    }

    [TestMethod]
    public void FormatOneline_WithoutDecorations_OmitsDecorationSection()
    {
        var commit = new GitCommitInfo(
            "abc1234567890def1234567890abcdef12345678",
            "Author",
            "author@example.com",
            DateTimeOffset.Now,
            "Committer",
            "committer@example.com",
            DateTimeOffset.Now,
            "Fix bug",
            "Fix bug",
            []);

        var result = GitLogFormatter.FormatOneline(commit);

        // Should not contain parens
        Assert.DoesNotContain(result, "(", "No decorations should mean no parentheses");
        // Should start with yellow SHA and end with message
        Assert.Contains($"{Esc}[33mabc1234{Esc}[0m", result);
        Assert.Contains("Fix bug", result);
    }
}
