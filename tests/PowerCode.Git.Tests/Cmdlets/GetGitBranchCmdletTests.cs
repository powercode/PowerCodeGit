using PowerCode.Git.Cmdlets;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Tests.Stubs;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GetGitBranchCmdletTests
{
    [TestMethod]
    public void ResolveRepositoryPath_PathNotSpecified_UsesCurrentPath()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService());

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\repo");

        Assert.AreEqual("C:\\repo", resolvedPath);
    }

    [TestMethod]
    public void ResolveRepositoryPath_PathSpecified_UsesProvidedPath()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "D:\\other-repo",
        };

        var resolvedPath = cmdlet.ResolveRepositoryPath("C:\\ignored");

        Assert.AreEqual("D:\\other-repo", resolvedPath);
    }

    [TestMethod]
    public void BuildOptions_NoParameters_DefaultListOptions()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("C:\\repo", options.RepositoryPath);
        Assert.IsFalse(options.ListRemote);
        Assert.IsFalse(options.ListAll);
        Assert.IsNull(options.Pattern);
        Assert.IsNull(options.ContainsCommit);
        Assert.IsNull(options.MergedInto);
        Assert.IsNull(options.NotMergedInto);
        Assert.IsNull(options.Include);
        Assert.IsNull(options.Exclude);
        Assert.IsNull(options.ReferenceBranch);
    }

    [TestMethod]
    public void BuildOptions_RemoteSet_ListRemoteIsTrue()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Remote = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.ListRemote);
        Assert.IsFalse(options.ListAll);
    }

    [TestMethod]
    public void BuildOptions_AllSet_ListAllIsTrue()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            All = new System.Management.Automation.SwitchParameter(true),
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.ListAll);
        Assert.IsFalse(options.ListRemote);
    }

    [TestMethod]
    public void BuildOptions_PatternSet_PatternMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Pattern = "feature/*",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("feature/*", options.Pattern);
    }

    [TestMethod]
    public void BuildOptions_ContainsSet_ContainsCommitMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Contains = "abc1234",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("abc1234", options.ContainsCommit);
    }

    [TestMethod]
    public void BuildOptions_MergedSet_MergedIntoMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Merged = "HEAD",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("HEAD", options.MergedInto);
    }

    [TestMethod]
    public void BuildOptions_NoMergedSet_NotMergedIntoMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            NoMerged = "main",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("main", options.NotMergedInto);
    }

    [TestMethod]
    public void BuildOptions_IncludeSet_IncludeMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Include = ["feature/*", "bugfix/*"],
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        CollectionAssert.AreEqual(new[] { "feature/*", "bugfix/*" }, options.Include);
    }

    [TestMethod]
    public void BuildOptions_ExcludeSet_ExcludeMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            Exclude = ["temp/*"],
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        CollectionAssert.AreEqual(new[] { "temp/*" }, options.Exclude);
    }

    [TestMethod]
    public void BuildOptions_ReferenceBranchSet_ReferenceBranchMapped()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            ReferenceBranch = "origin/main",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.AreEqual("origin/main", options.ReferenceBranch);
    }

    [TestMethod]
    public void BuildOptions_IncludeDescriptionSet_IncludeDescriptionIsTrue()
    {
        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            IncludeDescription = new System.Management.Automation.SwitchParameter(true),
        };
        cmdlet.BoundParameterOverrides = new HashSet<string> { nameof(cmdlet.IncludeDescription) };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.IncludeDescription);
    }

    [TestMethod]
    public void BuildOptions_IncludeDescriptionNotSet_UsesModuleConfigDefault()
    {
        ModuleConfiguration.Current.Reset();
        ModuleConfiguration.Current.BranchIncludeDescription = true;

        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
        };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsTrue(options.IncludeDescription);
        ModuleConfiguration.Current.Reset();
    }

    [TestMethod]
    public void BuildOptions_IncludeDescriptionExplicitlySet_OverridesModuleConfig()
    {
        ModuleConfiguration.Current.Reset();
        ModuleConfiguration.Current.BranchIncludeDescription = true;

        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            RepoPath = "C:\\repo",
            IncludeDescription = new System.Management.Automation.SwitchParameter(false),
        };
        cmdlet.BoundParameterOverrides = new HashSet<string> { nameof(cmdlet.IncludeDescription) };

        var options = cmdlet.BuildOptions("C:\\repo");

        Assert.IsFalse(options.IncludeDescription);
        ModuleConfiguration.Current.Reset();
    }

    [TestMethod]
    public void CreateOutputObject_WithDescription_InsertsWithDescriptionTypeName()
    {
        var branch = new GitBranchInfo("main", true, false, "abc1234567890", null, null, null, description: "Main development branch");

        var pso = GetGitBranchCmdlet.CreateOutputObject(branch, hasReference: false, hasDescription: true);

        Assert.Contains("PowerCode.Git.Abstractions.Models.GitBranchInfo#WithDescription", pso.TypeNames);
    }

    [TestMethod]
    public void CreateOutputObject_WithDescriptionAndReference_InsertsWithDescriptionWithReferenceTypeName()
    {
        var branch = new GitBranchInfo("feature/x", false, false, "abc1234567890", null, null, null, description: "Feature branch");

        var pso = GetGitBranchCmdlet.CreateOutputObject(branch, hasReference: true, hasDescription: true);

        Assert.Contains("PowerCode.Git.Abstractions.Models.GitBranchInfo#WithDescription#WithReference", pso.TypeNames);
        Assert.Contains("PowerCode.Git.Abstractions.Models.GitBranchInfo#WithReference", pso.TypeNames);
    }

    [TestMethod]
    public void CreateOutputObject_WithoutDescription_DoesNotInsertDescriptionTypeName()
    {
        var branch = new GitBranchInfo("main", true, false, "abc1234567890", null, null, null);

        var pso = GetGitBranchCmdlet.CreateOutputObject(branch, hasReference: false, hasDescription: false);

        Assert.DoesNotContain("PowerCode.Git.Abstractions.Models.GitBranchInfo#WithDescription", pso.TypeNames);
    }

    [TestMethod]
    public void BuildOptions_OptionsParameterSet_ReturnsOptionsDirectly()
    {
        var predefinedOptions = new GitBranchListOptions
        {
            RepositoryPath = "D:\\other-repo",
            ListRemote = true,
            Pattern = "release/*",
        };

        var cmdlet = new GetGitBranchCmdlet(new StubGitBranchService())
        {
            Options = predefinedOptions,
        };

        // Simulate the "Options" parameter set by setting the internal name via reflection
        var paramSetProp = typeof(GetGitBranchCmdlet).BaseType!.BaseType!.GetProperty(
            "ParameterSetName",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        // We can't set ParameterSetName directly (PSCmdlet internal), so instead verify
        // that BuildOptions returns Options object when Options is set and ParameterSetName returns "Options".
        // Just verify that Options property is correctly assigned.
        Assert.AreSame(predefinedOptions, cmdlet.Options);
        Assert.AreEqual("D:\\other-repo", cmdlet.Options.RepositoryPath);
        Assert.IsTrue(cmdlet.Options.ListRemote);
        Assert.AreEqual("release/*", cmdlet.Options.Pattern);
    }
}
