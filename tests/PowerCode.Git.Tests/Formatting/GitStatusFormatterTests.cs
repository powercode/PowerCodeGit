using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Formatting;

namespace PowerCode.Git.Tests.Formatting;

[TestClass]
public sealed class GitStatusFormatterTests
{
    private const string Esc = "\x1b";

    [TestMethod]
    public void FormatBranch_ReturnsBoldGreenBranch()
    {
        var result = GitStatusFormatter.FormatBranch("main");

        Assert.AreEqual($"{Esc}[1;32mmain{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatStagedCount_Zero_ReturnsPlainZero()
    {
        var result = GitStatusFormatter.FormatStagedCount(0);

        Assert.AreEqual("0", result);
    }

    [TestMethod]
    public void FormatStagedCount_NonZero_ReturnsGreenCount()
    {
        var result = GitStatusFormatter.FormatStagedCount(3);

        Assert.AreEqual($"{Esc}[32m3{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatModifiedCount_Zero_ReturnsPlainZero()
    {
        var result = GitStatusFormatter.FormatModifiedCount(0);

        Assert.AreEqual("0", result);
    }

    [TestMethod]
    public void FormatModifiedCount_NonZero_ReturnsRedCount()
    {
        var result = GitStatusFormatter.FormatModifiedCount(5);

        Assert.AreEqual($"{Esc}[31m5{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatUntrackedCount_NonZero_ReturnsRedCount()
    {
        var result = GitStatusFormatter.FormatUntrackedCount(2);

        Assert.AreEqual($"{Esc}[31m2{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatEntry_StagedAdded_GreenIndicator()
    {
        var entry = new GitStatusEntry("file.cs", GitFileStatus.Added, GitStagingState.Staged);

        var result = GitStatusFormatter.FormatEntry(entry);

        // Should be green and start with 'A'
        StringAssert.StartsWith(result, $"{Esc}[32mA");
        Assert.Contains("file.cs", result);
        StringAssert.EndsWith(result, $"{Esc}[0m");
    }

    [TestMethod]
    public void FormatEntry_UnstagedModified_RedIndicator()
    {
        var entry = new GitStatusEntry("src/app.cs", GitFileStatus.Modified, GitStagingState.Unstaged);

        var result = GitStatusFormatter.FormatEntry(entry);

        // Should be red
        StringAssert.StartsWith(result, $"{Esc}[31m");
        Assert.Contains("M", result);
        Assert.Contains("src/app.cs", result);
    }

    [TestMethod]
    public void FormatEntries_Empty_ReturnsCleanMessage()
    {
        var result = GitStatusFormatter.FormatEntries([]);

        Assert.Contains("nothing to commit", result);
    }

    [TestMethod]
    public void FormatEntries_MixedEntries_GroupsByStagingState()
    {
        var entries = new GitStatusEntry[]
        {
            new("staged.cs", GitFileStatus.Added, GitStagingState.Staged),
            new("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new("new-file.txt", GitFileStatus.Untracked, GitStagingState.Unstaged),
        };

        var result = GitStatusFormatter.FormatEntries(entries);

        // Should have section headers
        Assert.Contains("Changes to be committed:", result);
        Assert.Contains("Changes not staged for commit:", result);
        Assert.Contains("Untracked files:", result);
        // Should list file paths
        Assert.Contains("staged.cs", result);
        Assert.Contains("modified.cs", result);
        Assert.Contains("new-file.txt", result);
    }

    [TestMethod]
    public void FormatEntries_OnlyStaged_NoUnstagedSection()
    {
        var entries = new GitStatusEntry[]
        {
            new("file.cs", GitFileStatus.Added, GitStagingState.Staged),
        };

        var result = GitStatusFormatter.FormatEntries(entries);

        Assert.Contains("Changes to be committed:", result);
        Assert.DoesNotContain(result, "Changes not staged", "Should not have unstaged section");
        Assert.DoesNotContain(result, "Untracked", "Should not have untracked section");
    }

    [TestMethod]
    public void FormatEntryStatus_StagedAdded_ReturnsGreenIndicatorInLeftPosition()
    {
        var entry = new GitStatusEntry("file.cs", GitFileStatus.Added, GitStagingState.Staged);

        var result = GitStatusFormatter.FormatEntryStatus(entry);

        // Green, indicator 'A' in left (XY) position, no file path
        Assert.AreEqual($"{Esc}[32mA {Esc}[0m", result);
    }

    [TestMethod]
    public void FormatEntryStatus_UnstagedModified_ReturnsRedIndicatorInRightPosition()
    {
        var entry = new GitStatusEntry("src/app.cs", GitFileStatus.Modified, GitStagingState.Unstaged);

        var result = GitStatusFormatter.FormatEntryStatus(entry);

        // Red, indicator 'M' in right (XY) position, no file path
        Assert.AreEqual($"{Esc}[31m M{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatEntryStatus_DoesNotContainFilePath()
    {
        var entry = new GitStatusEntry("should-not-appear.cs", GitFileStatus.Deleted, GitStagingState.Staged);

        var result = GitStatusFormatter.FormatEntryStatus(entry);

        Assert.DoesNotContain("should-not-appear.cs",
result, "FormatEntryStatus should return only the indicator, not the file path.");
    }

    [TestMethod]
    public void FormatTracking_NoTrackedBranch_ReturnsEmpty()
    {
        var status = new GitStatusResult("repo", "main", [], 0, 0, 0);

        var result = GitStatusFormatter.FormatTracking(status);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatTracking_UpToDate_ReturnsCyanBranchOnly()
    {
        var status = new GitStatusResult("repo", "main", [], 0, 0, 0,
            trackedBranchName: "origin/main", aheadBy: 0, behindBy: 0);

        var result = GitStatusFormatter.FormatTracking(status);

        Assert.AreEqual($"{Esc}[36m[origin/main]{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatTracking_AheadOnly_ShowsAhead()
    {
        var status = new GitStatusResult("repo", "main", [], 0, 0, 0,
            trackedBranchName: "origin/main", aheadBy: 3, behindBy: 0);

        var result = GitStatusFormatter.FormatTracking(status);

        Assert.AreEqual($"{Esc}[36m[origin/main: ahead 3]{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatTracking_BehindOnly_ShowsBehind()
    {
        var status = new GitStatusResult("repo", "main", [], 0, 0, 0,
            trackedBranchName: "origin/main", aheadBy: 0, behindBy: 1);

        var result = GitStatusFormatter.FormatTracking(status);

        Assert.AreEqual($"{Esc}[36m[origin/main: behind 1]{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatTracking_AheadAndBehind_ShowsBoth()
    {
        var status = new GitStatusResult("repo", "main", [], 0, 0, 0,
            trackedBranchName: "origin/main", aheadBy: 2, behindBy: 5);

        var result = GitStatusFormatter.FormatTracking(status);

        Assert.AreEqual($"{Esc}[36m[origin/main: ahead 2, behind 5]{Esc}[0m", result);
    }
}
