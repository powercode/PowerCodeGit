using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Formatting;

namespace PowerCode.Git.Tests.Formatting;

[TestClass]
public sealed class GitBranchFormatterTests
{
    private const string Esc = "\x1b";

    [TestMethod]
    public void FormatBranchName_HeadBranch_ReturnsBoldGreenWithStar()
    {
        var branch = new GitBranchInfo("main", isHead: true, isRemote: false, "abc1234", null, null, null);

        var result = GitBranchFormatter.FormatBranchName(branch);

        Assert.AreEqual($"{Esc}[1;32m* main{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatBranchName_RemoteBranch_ReturnsRedName()
    {
        var branch = new GitBranchInfo("origin/main", isHead: false, isRemote: true, "abc1234", null, null, null);

        var result = GitBranchFormatter.FormatBranchName(branch);

        Assert.AreEqual($"  {Esc}[31morigin/main{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatBranchName_LocalBranch_ReturnsPlainWithIndent()
    {
        var branch = new GitBranchInfo("develop", isHead: false, isRemote: false, "abc1234", null, null, null);

        var result = GitBranchFormatter.FormatBranchName(branch);

        Assert.AreEqual("  develop", result);
    }

    [TestMethod]
    public void FormatTracking_NoTrackedBranch_ReturnsEmpty()
    {
        var branch = new GitBranchInfo("main", isHead: true, isRemote: false, "abc1234", null, null, null);

        var result = GitBranchFormatter.FormatTracking(branch);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatTracking_InSync_ReturnsCyanBracketedName()
    {
        var branch = new GitBranchInfo("main", isHead: true, isRemote: false, "abc1234", "origin/main", 0, 0);

        var result = GitBranchFormatter.FormatTracking(branch);

        Assert.AreEqual($"{Esc}[36m[origin/main]{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatTracking_AheadAndBehind_ShowsBothCounters()
    {
        var branch = new GitBranchInfo("main", isHead: true, isRemote: false, "abc1234", "origin/main", 2, 1);

        var result = GitBranchFormatter.FormatTracking(branch);

        Assert.AreEqual($"{Esc}[36m[origin/main: ahead 2, behind 1]{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatTracking_AheadOnly_ShowsOnlyAhead()
    {
        var branch = new GitBranchInfo("main", isHead: true, isRemote: false, "abc1234", "origin/main", 3, 0);

        var result = GitBranchFormatter.FormatTracking(branch);

        Assert.AreEqual($"{Esc}[36m[origin/main: ahead 3]{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatTracking_BehindOnly_ShowsOnlyBehind()
    {
        var branch = new GitBranchInfo("main", isHead: true, isRemote: false, "abc1234", "origin/main", 0, 5);

        var result = GitBranchFormatter.FormatTracking(branch);

        Assert.AreEqual($"{Esc}[36m[origin/main: behind 5]{Esc}[0m", result);
    }

    [TestMethod]
    public void FormatDescription_NullDescription_ReturnsEmpty()
    {
        var branch = new GitBranchInfo("main", isHead: true, isRemote: false, "abc1234", null, null, null);

        var result = GitBranchFormatter.FormatDescription(branch);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void FormatDescription_HasDescription_ReturnsDimText()
    {
        var branch = new GitBranchInfo("feature/x", isHead: false, isRemote: false, "abc1234", null, null, null, description: "My feature branch");

        var result = GitBranchFormatter.FormatDescription(branch);

        Assert.AreEqual($"{Esc}[2mMy feature branch{Esc}[0m", result);
    }
}
