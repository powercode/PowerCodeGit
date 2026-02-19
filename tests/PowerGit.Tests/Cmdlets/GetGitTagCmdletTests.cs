using PowerGit.Cmdlets;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Tests.Cmdlets;

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

    private sealed class StubGitTagService : IGitTagService
    {
        public IReadOnlyList<GitTagInfo> GetTags(string repositoryPath)
        {
            return Array.Empty<GitTagInfo>();
        }
    }
}
