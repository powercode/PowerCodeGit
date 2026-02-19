using PowerGit.Cmdlets;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Tests.Cmdlets;

[TestClass]
public sealed class GetGitTagCmdletTests
{
    [TestMethod]
    public void ResolvePath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService());

        var resolvedPath = cmdlet.ResolvePath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolvePath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitTagCmdlet(new StubGitTagService())
        {
            Path = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolvePath("C:\\ignored");

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
