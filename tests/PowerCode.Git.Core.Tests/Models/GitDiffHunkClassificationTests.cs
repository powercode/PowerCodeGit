using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Core.Tests.Models;

[TestClass]
public sealed class GitDiffHunkClassificationTests
{
    // ── Hunk factories ───────────────────────────────────────────────────────

    /// <summary>Creates a hunk containing only added lines.</summary>
    private static GitDiffHunk PureAdditionHunk(params string[] addedLines)
    {
        var linesAdded = addedLines.Length;
        var content = $"@@ -10,0 +11,{linesAdded} @@\n" + string.Join("\n", addedLines.Select(l => "+" + l));
        return new GitDiffHunk(
            filePath: "file.txt", oldPath: "file.txt", status: GitFileStatus.Modified,
            oldStart: 10, oldLineCount: 0, newStart: 11, newLineCount: linesAdded,
            header: $"@@ -10,0 +11,{linesAdded} @@", content: content,
            linesAdded: linesAdded, linesDeleted: 0);
    }

    /// <summary>Creates a hunk containing only removed lines.</summary>
    private static GitDiffHunk PureDeletionHunk(params string[] removedLines)
    {
        var linesDeleted = removedLines.Length;
        var content = $"@@ -10,{linesDeleted} +10,0 @@\n" + string.Join("\n", removedLines.Select(l => "-" + l));
        return new GitDiffHunk(
            filePath: "file.txt", oldPath: "file.txt", status: GitFileStatus.Modified,
            oldStart: 10, oldLineCount: linesDeleted, newStart: 10, newLineCount: 0,
            header: $"@@ -10,{linesDeleted} +10,0 @@", content: content,
            linesAdded: 0, linesDeleted: linesDeleted);
    }

    /// <summary>Creates a hunk with a single remove/add pair.</summary>
    private static GitDiffHunk SingleEditHunk(string oldLine, string newLine)
    {
        var content = "@@ -10,1 +10,1 @@\n" + $"-{oldLine}\n+{newLine}";
        return new GitDiffHunk(
            filePath: "file.txt", oldPath: "file.txt", status: GitFileStatus.Modified,
            oldStart: 10, oldLineCount: 1, newStart: 10, newLineCount: 1,
            header: "@@ -10,1 +10,1 @@", content: content,
            linesAdded: 1, linesDeleted: 1);
    }

    /// <summary>Creates a hunk with paired edits and extra unpaired additions.</summary>
    private static GitDiffHunk EditPlusAdditionsHunk(
        (string Old, string New)[] edits,
        string[] extraAdds)
    {
        var linesAdded = edits.Length + extraAdds.Length;
        var linesDeleted = edits.Length;
        var contentLines = new List<string>
        {
            $"@@ -10,{linesDeleted} +10,{linesAdded} @@"
        };

        foreach (var (old, @new) in edits)
        {
            contentLines.Add("-" + old);
            contentLines.Add("+" + @new);
        }

        contentLines.AddRange(extraAdds.Select(l => "+" + l));

        return new GitDiffHunk(
            filePath: "file.txt", oldPath: "file.txt", status: GitFileStatus.Modified,
            oldStart: 10, oldLineCount: linesDeleted, newStart: 10, newLineCount: linesAdded,
            header: $"@@ -10,{linesDeleted} +10,{linesAdded} @@",
            content: string.Join("\n", contentLines),
            linesAdded: linesAdded, linesDeleted: linesDeleted);
    }

    // ── Pure Addition ────────────────────────────────────────────────────────

    [TestMethod]
    public void ChangeKind_PureAddition_ReturnsAddition()
    {
        var hunk = PureAdditionHunk("new line 1", "new line 2");

        Assert.AreEqual(GitHunkChangeKind.Addition, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_PureAddition_SingleLine_ReturnsAddition()
    {
        var hunk = PureAdditionHunk("Console.WriteLine(\"hello\");");

        Assert.AreEqual(GitHunkChangeKind.Addition, hunk.ChangeKind);
    }

    // ── Pure Deletion ────────────────────────────────────────────────────────

    [TestMethod]
    public void ChangeKind_PureDeletion_ReturnsDeletion()
    {
        var hunk = PureDeletionHunk("old line 1", "old line 2");

        Assert.AreEqual(GitHunkChangeKind.Deletion, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_PureDeletion_SingleLine_ReturnsDeletion()
    {
        var hunk = PureDeletionHunk("var unused = GetExpensiveValue();");

        Assert.AreEqual(GitHunkChangeKind.Deletion, hunk.ChangeKind);
    }

    // ── Change (high-similarity edits) ───────────────────────────────────────

    [TestMethod]
    public void ChangeKind_SingleDigitChange_ReturnsChange()
    {
        // "var x = 42;" → "var x = 43;" — one char differs, very high similarity
        var hunk = SingleEditHunk("var x = 42;", "var x = 43;");

        Assert.AreEqual(GitHunkChangeKind.Change, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_ShortLineSmallEdit_ReturnsChange()
    {
        // "ab" → "ac": distance=1, max=2, sim=0.5, threshold(2)=0.4 → Change
        var hunk = SingleEditHunk("ab", "ac");

        Assert.AreEqual(GitHunkChangeKind.Change, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_TranspositionEdit_ReturnsChange()
    {
        // "var fisrt = 1;" → "var first = 1;" — transposition 'i','r' → high similarity
        var hunk = SingleEditHunk("var fisrt = 1;", "var first = 1;");

        Assert.AreEqual(GitHunkChangeKind.Change, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_MultipleHighSimilarityEdits_ReturnsChange()
    {
        // Two minimal edits — both pairs are high-similarity
        var content = "@@ -10,2 +10,2 @@\n-var x = 42;\n+var x = 43;\n-var y = 10;\n+var y = 11;";
        var hunk = new GitDiffHunk(
            "file.txt", "file.txt", GitFileStatus.Modified,
            10, 2, 10, 2,
            "@@ -10,2 +10,2 @@", content,
            linesAdded: 2, linesDeleted: 2);

        Assert.AreEqual(GitHunkChangeKind.Change, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_EditsPlusFewUnrelatedAdds_ReturnsChange()
    {
        // Two high-similarity edits + two unrelated additions.
        // linesAdded=4, linesDeleted=2 → total=6
        // highSimilarityPairs=2 → ratio=4/6=0.67 ≥ 0.5 → Change
        var hunk = EditPlusAdditionsHunk(
            edits:
            [
                ("var x = 42;", "var x = 43;"),
                ("var y = 10;", "var y = 11;"),
            ],
            extraAdds: ["// added comment", "Logger.Debug(\"checkpoint\");"]);

        Assert.AreEqual(GitHunkChangeKind.Change, hunk.ChangeKind);
    }

    // ── Mixed (rewrites / imbalanced content) ────────────────────────────────

    [TestMethod]
    public void ChangeKind_CompletelyDifferentShortLines_ReturnsMixed()
    {
        // "ab" → "xy": distance=2, max=2, sim=0, threshold(2)=0.4 → pair fails → Mixed
        var hunk = SingleEditHunk("ab", "xy");

        Assert.AreEqual(GitHunkChangeKind.Mixed, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_CompletelyDifferentLongLines_ReturnsMixed()
    {
        // Long lines with unrelated content — similarity will be very low,
        // well below the logarithmic threshold of ~0.8 for lines of this length.
        var hunk = SingleEditHunk(
            "public string FormatDescription(string verb, GitDiffHunk hunk, int max)",
            "private static void ProcessRecord(CancellationToken cancellationToken)");

        Assert.AreEqual(GitHunkChangeKind.Mixed, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_LowSimilarityMediumLines_ReturnsMixed()
    {
        // Lines of ~20 chars with high edit distance.
        // threshold(20) ≈ min(0.8, 0.3 + 0.1*log2(20)) ≈ 0.73
        // These lines share very little content so similarity will be below that.
        var hunk = SingleEditHunk("return Foo(a, b, c);", "throw new InvalidOp();");

        Assert.AreEqual(GitHunkChangeKind.Mixed, hunk.ChangeKind);
    }

    [TestMethod]
    public void ChangeKind_MostlyUnrelatedAddsWithFewEdits_ReturnsMixed()
    {
        // One high-similarity edit + many unrelated additions.
        // linesAdded=5, linesDeleted=1 → total=6
        // highSimilarityPairs=1 → ratio=2/6=0.33 < 0.5 → Mixed
        var hunk = EditPlusAdditionsHunk(
            edits: [("var x = 42;", "var x = 43;")],
            extraAdds:
            [
                "// NOTE: refactored below",
                "var y = ComputeY();",
                "var z = ComputeZ(y);",
                "Logger.Debug(z.ToString());"
            ]);

        Assert.AreEqual(GitHunkChangeKind.Mixed, hunk.ChangeKind);
    }

    // ── Lines: Similarity is populated for Modified pairs ───────────────────

    [TestMethod]
    public void Lines_ModifiedPair_HasSimilarity()
    {
        var hunk = SingleEditHunk("var x = 42;", "var x = 43;");

        var modifiedLine = hunk.Lines.Single(l => l.Kind == GitDiffLineKind.Modified);

        Assert.IsNotNull(modifiedLine.Similarity);
        Assert.IsGreaterThan(0.8, modifiedLine.Similarity.Value);
    }

    [TestMethod]
    public void Lines_ModifiedPair_HasMaxLineLength()
    {
        var hunk = SingleEditHunk("var x = 42;", "var x = 43;");

        var modifiedLine = hunk.Lines.Single(l => l.Kind == GitDiffLineKind.Modified);

        // Both lines are 11 chars, so MaxLineLength == 11
        Assert.AreEqual(11, modifiedLine.MaxLineLength);
    }

    [TestMethod]
    public void Lines_AddedLine_HasNullSimilarity()
    {
        var hunk = PureAdditionHunk("some new line");

        var addedLine = hunk.Lines.Single(l => l.Kind == GitDiffLineKind.Added);

        Assert.IsNull(addedLine.Similarity);
        Assert.IsNull(addedLine.MaxLineLength);
    }

    [TestMethod]
    public void Lines_RemovedLine_HasNullSimilarity()
    {
        var hunk = PureDeletionHunk("some old line");

        var removedLine = hunk.Lines.Single(l => l.Kind == GitDiffLineKind.Removed);

        Assert.IsNull(removedLine.Similarity);
        Assert.IsNull(removedLine.MaxLineLength);
    }
}
