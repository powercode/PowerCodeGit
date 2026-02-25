using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitTagCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void BuildOptions_DefaultSet_RepositoryPathMapped()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsNull(options.Pattern);
        Assert.IsNull(options.Exclude);
        Assert.IsNull(options.SortBy);
        Assert.IsNull(options.ContainsCommit);
    }

    [TestMethod]
    public void BuildOptions_IncludeSet_PatternMapped()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService())
        {
            Include = "v1.*",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("v1.*", options.Pattern);
    }

    [TestMethod]
    public void BuildOptions_ExcludeSet_ExcludeMapped()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService())
        {
            Exclude = "v1.*",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("v1.*", options.Exclude);
    }

    [TestMethod]
    public void BuildOptions_SortBySet_SortByMapped()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService())
        {
            SortBy = "version",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("version", options.SortBy);
    }

    [TestMethod]
    public void BuildOptions_ContainsCommitSet_ContainsCommitMapped()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService())
        {
            ContainsCommit = "abc1234",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("abc1234", options.ContainsCommit);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefined = new GitTagListOptions { RepositoryPath = "D:\\repo", Pattern = "release*" };
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService())
        {
            Options = predefined,
        };

        var options = cmdlet.BuildOptions("C:\\ignored");

        Assert.AreSame(predefined, options);
    }

    private sealed class StubGitTagService : IGitTagService
    {
        public IReadOnlyList<GitTagInfo> GetTags(GitTagListOptions options)
        {
            return Array.Empty<GitTagInfo>();
        }

        public GitTagInfo CreateTag(GitTagCreateOptions options) =>
            new(options.Name, "abc1234abc1234abc1234abc1234abc1234abc1234", isAnnotated: false, null, null, null, null);
    }
}
