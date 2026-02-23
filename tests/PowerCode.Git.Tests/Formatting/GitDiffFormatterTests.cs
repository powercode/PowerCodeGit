using PowerCode.Git.Formatting;

namespace PowerCode.Git.Tests.Formatting;

[TestClass]
public sealed class GitDiffFormatterTests
{
    private const string Esc = "\x1b";
    private const string Reset = $"{Esc}[0m";
    private const string Green = $"{Esc}[32m";
    private const string Red = $"{Esc}[31m";
    private const string Cyan = $"{Esc}[36m";
    private const string Bold = $"{Esc}[1m";

    // AnnotateContent — unit tests for the internal whitespace-annotation helper.

    [TestMethod]
    public void AnnotateContent_NoWhitespace_ReturnsSameLine()
    {
        var result = GitDiffFormatter.AnnotateContent("+hello");

        Assert.AreEqual("+hello", result);
    }

    [TestMethod]
    public void AnnotateContent_OnlyMarker_ReturnsSameLine()
    {
        var result = GitDiffFormatter.AnnotateContent("+");

        Assert.AreEqual("+", result);
    }

    [TestMethod]
    public void AnnotateContent_LeadingSpaces_ReplacedWithDot()
    {
        var result = GitDiffFormatter.AnnotateContent("+  hello");

        Assert.AreEqual($"+{GitDiffFormatter.VisibleSpace}{GitDiffFormatter.VisibleSpace}hello", result);
    }

    [TestMethod]
    public void AnnotateContent_TrailingSpaces_ReplacedWithDot()
    {
        var result = GitDiffFormatter.AnnotateContent("+hello  ");

        Assert.AreEqual($"+hello{GitDiffFormatter.VisibleSpace}{GitDiffFormatter.VisibleSpace}", result);
    }

    [TestMethod]
    public void AnnotateContent_LeadingTab_ReplacedWithArrow()
    {
        var result = GitDiffFormatter.AnnotateContent("+\thello");

        Assert.AreEqual($"+{GitDiffFormatter.VisibleTab}hello", result);
    }

    [TestMethod]
    public void AnnotateContent_TrailingTab_ReplacedWithArrow()
    {
        var result = GitDiffFormatter.AnnotateContent("+hello\t");

        Assert.AreEqual($"+hello{GitDiffFormatter.VisibleTab}", result);
    }

    [TestMethod]
    public void AnnotateContent_TrailingCr_ReplacedWithLeft()
    {
        var result = GitDiffFormatter.AnnotateContent("+hello\r");

        Assert.AreEqual($"+hello{GitDiffFormatter.VisibleCr}", result);
    }

    [TestMethod]
    public void AnnotateContent_MiddleWhitespace_NotReplaced()
    {
        // Spaces in the middle of the content must not be annotated.
        var result = GitDiffFormatter.AnnotateContent("+hello world");

        Assert.AreEqual("+hello world", result);
    }

    [TestMethod]
    public void AnnotateContent_BothLeadingAndTrailing_BothReplaced()
    {
        var result = GitDiffFormatter.AnnotateContent("- foo ");

        Assert.AreEqual($"-{GitDiffFormatter.VisibleSpace}foo{GitDiffFormatter.VisibleSpace}", result);
    }

    [TestMethod]
    public void AnnotateContent_ContextLine_MarkerPreservedAndWhitespaceAnnotated()
    {
        // Context lines start with a space (the diff marker).
        var result = GitDiffFormatter.AnnotateContent("  trailing ");

        // First char is the diff marker ' '.
        Assert.AreEqual($" {GitDiffFormatter.VisibleSpace}trailing{GitDiffFormatter.VisibleSpace}", result);
    }

    [TestMethod]
    public void AnnotateContent_AllWhitespaceContent_AllReplaced()
    {
        var result = GitDiffFormatter.AnnotateContent("+   ");

        Assert.AreEqual(
            $"+{GitDiffFormatter.VisibleSpace}{GitDiffFormatter.VisibleSpace}{GitDiffFormatter.VisibleSpace}",
            result);
    }

    // FormatPatch — integration tests for coloring + annotation.

    [TestMethod]
    public void FormatPatch_Null_ReturnsEmpty()
    {
        var result = GitDiffFormatter.FormatPatch(null);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatPatch_Empty_ReturnsEmpty()
    {
        var result = GitDiffFormatter.FormatPatch(string.Empty);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatPatch_AddedLine_IsGreen()
    {
        var result = GitDiffFormatter.FormatPatch("+hello");

        Assert.AreEqual($"{Green}+hello{Reset}", result);
    }

    [TestMethod]
    public void FormatPatch_RemovedLine_IsRed()
    {
        var result = GitDiffFormatter.FormatPatch("-hello");

        Assert.AreEqual($"{Red}-hello{Reset}", result);
    }

    [TestMethod]
    public void FormatPatch_HunkHeader_IsCyan()
    {
        var result = GitDiffFormatter.FormatPatch("@@ -1,4 +1,5 @@");

        Assert.AreEqual($"{Cyan}@@ -1,4 +1,5 @@{Reset}", result);
    }

    [TestMethod]
    public void FormatPatch_DiffHeader_IsBold()
    {
        var result = GitDiffFormatter.FormatPatch("diff --git a/foo.cs b/foo.cs");

        Assert.AreEqual($"{Bold}diff --git a/foo.cs b/foo.cs{Reset}", result);
    }

    [TestMethod]
    public void FormatPatch_PlusHeader_IsBold()
    {
        var result = GitDiffFormatter.FormatPatch("+++ b/foo.cs");

        Assert.AreEqual($"{Bold}+++ b/foo.cs{Reset}", result);
    }

    [TestMethod]
    public void FormatPatch_MinusHeader_IsBold()
    {
        var result = GitDiffFormatter.FormatPatch("--- a/foo.cs");

        Assert.AreEqual($"{Bold}--- a/foo.cs{Reset}", result);
    }

    [TestMethod]
    public void FormatPatch_AddedLineWithTrailingSpaces_GreenWithVisibleSpaces()
    {
        var result = GitDiffFormatter.FormatPatch("+hello   ");

        Assert.AreEqual(
            $"{Green}+hello{GitDiffFormatter.VisibleSpace}{GitDiffFormatter.VisibleSpace}{GitDiffFormatter.VisibleSpace}{Reset}",
            result);
    }

    [TestMethod]
    public void FormatPatch_RemovedLineWithLeadingTab_RedWithVisibleTab()
    {
        var result = GitDiffFormatter.FormatPatch("-\tindented");

        Assert.AreEqual($"{Red}-{GitDiffFormatter.VisibleTab}indented{Reset}", result);
    }

    [TestMethod]
    public void FormatPatch_ContextLine_NoColor()
    {
        var result = GitDiffFormatter.FormatPatch(" context");

        Assert.AreEqual(" context", result);
    }

    [TestMethod]
    public void FormatPatch_MultipleLines_EachLineColoredAndAnnotatedIndependently()
    {
        var patch = $"diff --git a/f.cs b/f.cs\n@@ -1 +1 @@\n-old  \n+new  \n context";

        var result = GitDiffFormatter.FormatPatch(patch);

        var lines = result.Split('\n');
        Assert.HasCount(5, lines);
        StringAssert.StartsWith(lines[0], Bold);    // diff header
        StringAssert.StartsWith(lines[1], Cyan);    // hunk header
        StringAssert.StartsWith(lines[2], Red);     // removed
        StringAssert.StartsWith(lines[3], Green);   // added
        // context — no ANSI color prefix
        Assert.IsFalse(lines[4].StartsWith(Esc, StringComparison.Ordinal));
    }

    [TestMethod]
    public void FormatPatch_TrailingWhitespace_VisibleInBothAddedAndRemoved()
    {
        var patch = "-old line  \n+new line\t";

        var result = GitDiffFormatter.FormatPatch(patch);
        var lines = result.Split('\n');

        // Removed line: trailing two spaces become visible dots.
        StringAssert.Contains(lines[0],
            $"{GitDiffFormatter.VisibleSpace}{GitDiffFormatter.VisibleSpace}{Reset}");

        // Added line: trailing tab becomes visible arrow.
        StringAssert.Contains(lines[1], $"{GitDiffFormatter.VisibleTab}{Reset}");
    }
}
