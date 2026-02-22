using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Tests.Models;

[TestClass]
public sealed class DiffHunkParserTests
{
    private static GitDiffEntry CreateEntry(
        string patch,
        string newPath = "file.txt",
        string oldPath = "file.txt",
        GitFileStatus status = GitFileStatus.Modified,
        int linesAdded = 0,
        int linesDeleted = 0) =>
        new(oldPath, newPath, status, linesAdded, linesDeleted, patch);

    [TestMethod]
    public void Parse_NullPatch_ReturnsEmpty()
    {
        var entry = CreateEntry(patch: null!);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.HasCount(0, hunks);
    }

    [TestMethod]
    public void Parse_EmptyPatch_ReturnsEmpty()
    {
        var entry = CreateEntry(patch: string.Empty);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.HasCount(0, hunks);
    }

    [TestMethod]
    public void Parse_SingleHunk_ReturnsSingleHunk()
    {
        var patch = "@@ -1,3 +1,4 @@\n line1\n-old\n+new\n+added\n line3\n";
        var entry = CreateEntry(patch, linesAdded: 2, linesDeleted: 1);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.HasCount(1, hunks);
        Assert.AreEqual(1, hunks[0].OldStart);
        Assert.AreEqual(3, hunks[0].OldLineCount);
        Assert.AreEqual(1, hunks[0].NewStart);
        Assert.AreEqual(4, hunks[0].NewLineCount);
    }

    [TestMethod]
    public void Parse_MultipleHunks_ReturnsAllHunks()
    {
        var patch = "@@ -1,3 +1,4 @@\n line1\n-old\n+new\n+added\n line3\n"
                  + "@@ -10,3 +11,2 @@\n line10\n-removed\n line12\n";
        var entry = CreateEntry(patch, linesAdded: 2, linesDeleted: 2);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.HasCount(2, hunks);
        Assert.AreEqual(1, hunks[0].OldStart);
        Assert.AreEqual(10, hunks[1].OldStart);
        Assert.AreEqual(11, hunks[1].NewStart);
        Assert.AreEqual(2, hunks[1].NewLineCount);
    }

    [TestMethod]
    public void Parse_HunkCounts_MatchLinesAddedDeleted()
    {
        var patch = "@@ -1,3 +1,4 @@\n line1\n-old\n+new\n+added\n line3\n";
        var entry = CreateEntry(patch);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.AreEqual(2, hunks[0].LinesAdded);
        Assert.AreEqual(1, hunks[0].LinesDeleted);
    }

    [TestMethod]
    public void Parse_RenamedFile_CarriesOldAndNewPath()
    {
        var patch = "@@ -1,2 +1,2 @@\n-old content\n+new content\n";
        var entry = CreateEntry(
            patch,
            oldPath: "old-name.txt",
            newPath: "new-name.txt",
            status: GitFileStatus.Renamed);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.HasCount(1, hunks);
        Assert.AreEqual("old-name.txt", hunks[0].OldPath);
        Assert.AreEqual("new-name.txt", hunks[0].FilePath);
        Assert.AreEqual(GitFileStatus.Renamed, hunks[0].Status);
    }

    [TestMethod]
    public void Parse_HunkHeaderWithFunctionContext_PreservesContext()
    {
        var patch = "@@ -10,5 +10,6 @@ public void MyMethod()\n context\n-removed\n+added1\n+added2\n context\n";
        var entry = CreateEntry(patch);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.HasCount(1, hunks);
        Assert.Contains("public void MyMethod()", hunks[0].Header);
    }

    [TestMethod]
    public void Parse_OmittedLineCount_DefaultsToOne()
    {
        // When line count is omitted it defaults to 1: @@ -5 +5 @@
        var patch = "@@ -5 +5 @@\n-old\n+new\n";
        var entry = CreateEntry(patch);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.HasCount(1, hunks);
        Assert.AreEqual(1, hunks[0].OldLineCount);
        Assert.AreEqual(1, hunks[0].NewLineCount);
    }

    [TestMethod]
    public void Parse_NullEntry_ThrowsArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => DiffHunkParser.Parse(null!));
    }

    [TestMethod]
    public void Parse_ContentDoesNotIncludeNextHunk()
    {
        var patch = "@@ -1,2 +1,3 @@\n context\n+added\n context\n"
                  + "@@ -20,2 +21,2 @@\n-old\n+new\n";
        var entry = CreateEntry(patch);

        var hunks = DiffHunkParser.Parse(entry);

        Assert.DoesNotContain("-20,2", hunks[0].Content);
        Assert.Contains("-20,2", hunks[1].Content);
    }
}
