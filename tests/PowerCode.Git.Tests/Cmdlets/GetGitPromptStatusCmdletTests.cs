using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Cmdlets;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitPromptStatusCmdletTests
{
    // ── ResolveRepositoryPath ─────────────────────────────────────────────────

    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    // ── BuildOptions — parameter defaults ────────────────────────────────────

    [TestMethod]
    public void BuildOptions_NoParametersSet_AllFlagsAreFalse()
    {
        ModuleConfiguration.Current.Reset();
        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService());

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsFalse(options.HideUpstream);
        Assert.IsFalse(options.HideCounts);
        Assert.IsFalse(options.HideStash);
        Assert.IsFalse(options.NoColor);
    }

    [TestMethod]
    public void BuildOptions_HideUpstreamSet_HideUpstreamIsTrue()
    {
        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService())
        {
            HideUpstream = new SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.HideUpstream);
    }

    [TestMethod]
    public void BuildOptions_HideCountsSet_HideCountsIsTrue()
    {
        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService())
        {
            HideCounts = new SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.HideCounts);
    }

    [TestMethod]
    public void BuildOptions_HideStashSet_HideStashIsTrue()
    {
        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService())
        {
            HideStash = new SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.HideStash);
    }

    [TestMethod]
    public void BuildOptions_NoColorSet_NoColorIsTrue()
    {
        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService())
        {
            NoColor = new SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.NoColor);
    }

    // ── BuildOptions — module configuration defaults ──────────────────────────

    [TestMethod]
    public void BuildOptions_ModuleConfigHideUpstreamTrue_HideUpstreamIsTrue()
    {
        ModuleConfiguration.Current.Reset();
        ModuleConfiguration.Current.PromptHideUpstream = true;
        try
        {
            var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService());

            var options = cmdlet.BuildOptions("C:\\repo");

            Assert.IsTrue(options.HideUpstream);
        }
        finally
        {
            ModuleConfiguration.Current.Reset();
        }
    }

    [TestMethod]
    public void BuildOptions_ModuleConfigNoColorTrue_NoColorIsTrue()
    {
        ModuleConfiguration.Current.Reset();
        ModuleConfiguration.Current.PromptNoColor = true;
        try
        {
            var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService());

            var options = cmdlet.BuildOptions("C:\\repo");

            Assert.IsTrue(options.NoColor);
        }
        finally
        {
            ModuleConfiguration.Current.Reset();
        }
    }

    // ── BuildOptions — Options parameter set ──────────────────────────────────

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_OptionsPropertyStoresValue()
    {
        var predefined = new GitPromptStatusOptions
        {
            RepositoryPath = "D:\\special-repo",
            HideUpstream = true,
            HideCounts = true,
            HideStash = false,
            NoColor = true,
        };

        var cmdlet = new GetGitPromptStatusCmdlet(new StubGitWorkingTreeService(), new StubGitRemoteService())
        {
            Options = predefined,
        };

        // When the ParameterSetName is "Options", BuildOptions returns Options directly.
        // Outside the PS engine the ParameterSetName is not "Options", so we verify the
        // property itself forwards its value as expected by the runtime path.
        Assert.AreSame(predefined, cmdlet.Options);
    }
}
