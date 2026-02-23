using System.Collections;
using System.Management.Automation;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Tests.Completers;

[TestClass]
public sealed class GitConfigValueCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestMethod]
    public void CompleteArgument_KnownName_ReturnsValues()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "core.autocrlf" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, parameters).ToList();

        Assert.HasCount(3, results);
        Assert.IsTrue(results.Any(r => r.CompletionText == "true"));
        Assert.IsTrue(results.Any(r => r.CompletionText == "false"));
        Assert.IsTrue(results.Any(r => r.CompletionText == "input"));
    }

    [TestMethod]
    public void CompleteArgument_PrefixFilter_ReturnsOnlyMatching()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "core.autocrlf" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "t", null!, parameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("true", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_CaseInsensitiveNameLookup_ReturnsValues()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "CORE.AUTOCRLF" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, parameters).ToList();

        Assert.HasCount(3, results);
    }

    [TestMethod]
    public void CompleteArgument_UnknownName_ReturnsEmpty()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "custom.unknown" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, parameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_NoNameBound_ReturnsEmpty()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_NullFakeBoundParameters_ReturnsEmpty()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, null!).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_EmptyNameBound_ReturnsEmpty()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, parameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_PushDefault_ReturnsAllModes()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "push.default" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, parameters).ToList();

        Assert.HasCount(5, results);
        Assert.IsTrue(results.Any(r => r.CompletionText == "simple"));
    }

    [TestMethod]
    public void CompleteArgument_DiffAlgorithm_ReturnsAlgorithms()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "diff.algorithm" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, parameters).ToList();

        Assert.HasCount(4, results);
        Assert.IsTrue(results.Any(r => r.CompletionText == "histogram"));
    }

    [TestMethod]
    public void CompleteArgument_AllResults_HaveParameterValueType()
    {
        var completer = new GitConfigValueCompleterAttribute.ConfigValueCompleter();
        var parameters = new Hashtable(BoundParameters) { ["Name"] = "color.ui" };

        var results = completer.CompleteArgument("Set-GitConfiguration", "Value", "", null!, parameters).ToList();

        Assert.IsNotEmpty(results);
        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }
}
