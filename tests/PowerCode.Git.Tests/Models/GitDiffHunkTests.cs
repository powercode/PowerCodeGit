using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Tests.Models;

[TestClass]
public sealed class GitDiffHunkTests
{
    // Minimal helper that fakes enough of a GitDiffHunk for Lines parsing.
    private static GitDiffHunk MakeHunk(
        string content,
        int oldStart = 1,
        int newStart = 1,
        int oldLineCount = 0,
        int newLineCount = 0) =>
        new(
            filePath: "file.txt",
            oldPath: "file.txt",
            status: GitFileStatus.Modified,
            oldStart: oldStart,
            oldLineCount: oldLineCount,
            newStart: newStart,
            newLineCount: newLineCount,
            header: content.Split('\n')[0],
            content: content,
            linesAdded: 0,
            linesDeleted: 0);

    // ------------------------------------------------------------------ //
    // Only additions
    // ------------------------------------------------------------------ //

    [TestMethod]
    public void Lines_OnlyAdditions_AllLinesAreAdded()
    {
        var hunk = MakeHunk(
            "@@ -1,0 +1,3 @@\n+line A\n+line B\n+line C\n",
            oldStart: 1, newStart: 1);

        var lines = hunk.Lines;

        Assert.HasCount(3, lines);
        Assert.IsTrue(lines.All(l => l.Kind == GitDiffLineKind.Added));
    }

    [TestMethod]
    public void Lines_OnlyAdditions_OldLineNumberIsNull()
    {
        var hunk = MakeHunk("@@ -1,0 +1,2 @@\n+alpha\n+beta\n", newStart: 1);

        var lines = hunk.Lines;

        Assert.IsTrue(lines.All(l => l.OldLineNumber is null));
    }

    [TestMethod]
    public void Lines_OnlyAdditions_NewLineNumbersAreSequential()
    {
        var hunk = MakeHunk("@@ -0,0 +5,3 @@\n+x\n+y\n+z\n", newStart: 5);

        var lines = hunk.Lines;

        Assert.AreEqual(5, lines[0].NewLineNumber);
        Assert.AreEqual(6, lines[1].NewLineNumber);
        Assert.AreEqual(7, lines[2].NewLineNumber);
    }

    // ------------------------------------------------------------------ //
    // Only removals
    // ------------------------------------------------------------------ //

    [TestMethod]
    public void Lines_OnlyRemovals_AllLinesAreRemoved()
    {
        var hunk = MakeHunk("@@ -1,3 +1,0 @@\n-line A\n-line B\n-line C\n", oldStart: 1);

        var lines = hunk.Lines;

        Assert.HasCount(3, lines);
        Assert.IsTrue(lines.All(l => l.Kind == GitDiffLineKind.Removed));
    }

    [TestMethod]
    public void Lines_OnlyRemovals_NewLineNumberIsNull()
    {
        var hunk = MakeHunk("@@ -3,2 +3,0 @@\n-foo\n-bar\n", oldStart: 3);

        var lines = hunk.Lines;

        Assert.IsTrue(lines.All(l => l.NewLineNumber is null));
    }

    [TestMethod]
    public void Lines_OnlyRemovals_OldLineNumbersAreSequential()
    {
        var hunk = MakeHunk("@@ -10,3 +10,0 @@\n-a\n-b\n-c\n", oldStart: 10);

        var lines = hunk.Lines;

        Assert.AreEqual(10, lines[0].OldLineNumber);
        Assert.AreEqual(11, lines[1].OldLineNumber);
        Assert.AreEqual(12, lines[2].OldLineNumber);
    }

    // ------------------------------------------------------------------ //
    // Adjacent remove+add pairs → Modified
    // ------------------------------------------------------------------ //

    [TestMethod]
    public void Lines_AdjacentRemoveAddPair_EmittedAsModified()
    {
        var hunk = MakeHunk("@@ -1,1 +1,1 @@\n-old line\n+new line\n", oldStart: 1, newStart: 1);

        var lines = hunk.Lines;

        Assert.HasCount(1, lines);
        Assert.AreEqual(GitDiffLineKind.Modified, lines[0].Kind);
    }

    [TestMethod]
    public void Lines_ModifiedLine_HasBothOldAndNewLineNumbers()
    {
        var hunk = MakeHunk("@@ -5,1 +5,1 @@\n-old\n+new\n", oldStart: 5, newStart: 5);

        var lines = hunk.Lines;

        Assert.AreEqual(5, lines[0].OldLineNumber);
        Assert.AreEqual(5, lines[0].NewLineNumber);
    }

    [TestMethod]
    public void Lines_ModifiedLine_ContentIsNewText()
    {
        var hunk = MakeHunk("@@ -1,1 +1,1 @@\n-old content\n+new content\n", oldStart: 1, newStart: 1);

        var lines = hunk.Lines;

        Assert.AreEqual("new content", lines[0].Content);
    }

    [TestMethod]
    public void Lines_MultiplePairs_AllEmittedAsModified()
    {
        var hunk = MakeHunk(
            "@@ -1,3 +1,3 @@\n-a\n+A\n-b\n+B\n-c\n+C\n",
            oldStart: 1, newStart: 1);

        var lines = hunk.Lines;

        Assert.HasCount(3, lines);
        Assert.IsTrue(lines.All(l => l.Kind == GitDiffLineKind.Modified));
    }

    // ------------------------------------------------------------------ //
    // Mixed: context + changes
    // ------------------------------------------------------------------ //

    [TestMethod]
    public void Lines_MixedHunk_ContextLinesAreExcluded()
    {
        // context, remove, add, context
        var hunk = MakeHunk(
            "@@ -1,3 +1,3 @@\n context\n-removed\n+added\n context\n",
            oldStart: 1, newStart: 1);

        var lines = hunk.Lines;

        // Only the changed line should appear (as Modified)
        Assert.HasCount(1, lines);
        Assert.AreEqual(GitDiffLineKind.Modified, lines[0].Kind);
    }

    [TestMethod]
    public void Lines_MixedHunk_LineNumbersAdvancePastContextLines()
    {
        // 2 context lines, then a change at line 3
        var hunk = MakeHunk(
            "@@ -1,4 +1,4 @@\n ctx1\n ctx2\n-old3\n+new3\n",
            oldStart: 1, newStart: 1);

        var lines = hunk.Lines;

        Assert.HasCount(1, lines);
        Assert.AreEqual(3, lines[0].OldLineNumber);
        Assert.AreEqual(3, lines[0].NewLineNumber);
    }

    [TestMethod]
    public void Lines_UnpairedRemoveAfterContext_EmittedAsRemoved()
    {
        var hunk = MakeHunk(
            "@@ -1,3 +1,2 @@\n ctx\n-removed\n ctx\n",
            oldStart: 1, newStart: 1);

        var lines = hunk.Lines;

        Assert.HasCount(1, lines);
        Assert.AreEqual(GitDiffLineKind.Removed, lines[0].Kind);
        Assert.IsNull(lines[0].NewLineNumber);
    }

    // ------------------------------------------------------------------ //
    // Caching
    // ------------------------------------------------------------------ //

    [TestMethod]
    public void Lines_CalledTwice_ReturnsSameInstance()
    {
        var hunk = MakeHunk("@@ -1,1 +1,1 @@\n-old\n+new\n", oldStart: 1, newStart: 1);

        var first  = hunk.Lines;
        var second = hunk.Lines;

        Assert.AreSame(first, second);
    }
}
