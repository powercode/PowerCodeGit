using System.Collections.Generic;
using System.Management.Automation;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class SearchGitCommitCmdletTests
{
    [TestMethod]
    public void BuildOptions_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService());

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual(@"C:\repo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_RepoPathSpecified_UsesRepoPath()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService())
        {
            RepoPath = @"D:\myrepo",
        };

        var options = cmdlet.BuildOptions(@"C:\ignored");

        Assert.AreEqual(@"D:\myrepo", options.RepositoryPath);
    }

    [TestMethod]
    public void BuildOptions_ContentSearchSet_MapsToOptions()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService())
        {
            ContentSearch = "TODO",
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual("TODO", options.ContentSearch);
    }

    [TestMethod]
    public void BuildOptions_ContentSearchIsRegexSet_SetsFlag()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService())
        {
            ContentSearch = "TODO|FIXME",
            ContentSearchIsRegex = new SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.IsTrue(options.ContentSearchIsRegex);
    }

    [TestMethod]
    public void BuildOptions_FirstBound_SetsMaxCount()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService())
        {
            First = 10,
            BoundParameterOverrides = new HashSet<string> { nameof(SearchGitCommitCmdlet.First) },
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual(10, options.MaxCount);
    }

    [TestMethod]
    public void BuildOptions_FirstNotBound_MaxCountIsNull()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService());

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.IsNull(options.MaxCount);
    }

    [TestMethod]
    public void BuildOptions_FromSet_MapsToOptions()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService())
        {
            From = "main",
        };

        var options = cmdlet.BuildOptions(@"C:\repo");

        Assert.AreEqual("main", options.From);
    }

    [TestMethod]
    public void BuildOptions_PathSet_MapsToOptions()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService())
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
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService());

        var predicate = cmdlet.BuildPredicate();

        Assert.IsNull(predicate);
    }

    [TestMethod]
    public void BuildPredicate_WhereNotNull_ReturnsNonNullPredicate()
    {
        var cmdlet = new SearchGitCommitCmdlet(new StubGitCommitSearchService())
        {
            Where = ScriptBlock.Create("$true"),
        };

        var predicate = cmdlet.BuildPredicate();

        Assert.IsNotNull(predicate);
    }
}
