using System.Collections.Generic;
using System.Management.Automation;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class SelectGitCommitCmdletTests
{
    [TestMethod]
    public void BuildOptions_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService());

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual(@"C:\repo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_RepoPathSpecified_UsesRepoPath()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService())
        {
            RepoPath = @"D:\myrepo",
        };

        var options = cmdlet.BuildOptions(@"C:\ignored");

        Assert.AreEqual(@"D:\myrepo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_ContainsSet_MapsToOptions()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService())
        {
            Contains = "TODO",
            BoundParameterOverrides = new HashSet<string> { nameof(SelectGitCommitCmdlet.Contains) },
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual("TODO", options.Contains);
        Assert.IsNull(options.Match);
    }

    [TestMethod]
    public void BuildOptions_MatchSet_MapsToOptions()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService())
        {
            Match = "TODO|FIXME",
            BoundParameterOverrides = new HashSet<string> { nameof(SelectGitCommitCmdlet.Match) },
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual("TODO|FIXME", options.Match);
        Assert.IsNull(options.Contains);
    }

    [TestMethod]
    public void BuildOptions_FirstBound_SetsMaxCount()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService())
        {
            First = 10,
            BoundParameterOverrides = new HashSet<string> { nameof(SelectGitCommitCmdlet.First) },
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual(10, options.MaxCount);
    }

    [TestMethod]
    public void BuildOptions_FirstNotBound_MaxCountIsNull()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService());

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.IsNull(options.MaxCount);
    }

    [TestMethod]
    public void BuildOptions_FromSet_MapsToOptions()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService())
        {
            From = "main",
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual("main", options.From);
    }

    [TestMethod]
    public void BuildOptions_PathSet_MapsToOptions()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService())
        {
            Path = ["src/Program.cs", "README.md"],
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        CollectionAssert.AreEqual(
            new[] { "src/Program.cs", "README.md" },
            options.Paths);
    }

    [TestMethod]
    public void BuildPredicate_WhereNull_ReturnsNull()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService());

        var predicate = cmdlet.BuildPredicate();

        Assert.IsNull(predicate);
    }

    [TestMethod]
    public void BuildPredicate_WhereNotNull_ReturnsNonNullPredicate()
    {
        var cmdlet = new SelectGitCommitCmdlet(new StubGitCommitSearchService())
        {
            Where = ScriptBlock.Create("$true"),
        };

        var predicate = cmdlet.BuildPredicate();

        Assert.IsNotNull(predicate);
    }
}
