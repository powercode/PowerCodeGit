using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Core.Tests.Models;

[TestClass]
public sealed class GitBranchInfoTests
{
    private static GitBranchInfo MakeBranch(string name, bool isRemote) =>
        new(name, isHead: false, isRemote: isRemote, tipSha: new string('0', 40),
            trackedBranchName: null, aheadBy: null, behindBy: null);

    [TestMethod]
    public void LocalName_LocalBranch_EqualsName()
    {
        var branch = MakeBranch("main", isRemote: false);

        Assert.AreEqual("main", branch.LocalName);
    }

    [TestMethod]
    public void LocalName_LocalBranchWithSlashes_EqualsName()
    {
        var branch = MakeBranch("feature/foo/bar", isRemote: false);

        Assert.AreEqual("feature/foo/bar", branch.LocalName);
    }

    [TestMethod]
    public void LocalName_RemoteBranch_StripsRemotePrefix()
    {
        var branch = MakeBranch("origin/main", isRemote: true);

        Assert.AreEqual("main", branch.LocalName);
    }

    [TestMethod]
    public void LocalName_RemoteBranchWithSlashes_StripsOnlyRemotePrefix()
    {
        var branch = MakeBranch("origin/feature/deep/path", isRemote: true);

        Assert.AreEqual("feature/deep/path", branch.LocalName);
    }

    [TestMethod]
    public void LocalName_RemoteBranchNonOrigin_StripsRemotePrefix()
    {
        var branch = MakeBranch("upstream/release/v2", isRemote: true);

        Assert.AreEqual("release/v2", branch.LocalName);
    }
}
