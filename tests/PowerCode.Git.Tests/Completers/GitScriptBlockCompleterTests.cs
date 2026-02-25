using System.Collections;
using System.Management.Automation;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Tests.Completers;

[TestClass]
public sealed class GitScriptBlockCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestInitialize]
    public void ResetRegistry()
    {
        GitScriptBlockCompleterAttribute.ClearRegistry();
        GitScriptBlockCompleterAttribute.RegisterDefaults();
    }

    // ---------------------------------------------------------------
    //  Select-GitCommit -Where
    // ---------------------------------------------------------------

    [TestMethod]
    public void SelectGitCommit_Where_EmptyWord_ReturnsAllExamples()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "", null!, BoundParameters).ToList();

        Assert.HasCount(GitScriptBlockCompleterAttribute.SelectGitCommitWhereExamples.Count, results);
    }

    [TestMethod]
    public void SelectGitCommit_Where_FilterByAuthor_ReturnsAuthorExamples()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "Author", null!, BoundParameters).ToList();

        Assert.IsNotEmpty(results, "Expected at least one result matching 'Author'.");
        Assert.IsTrue(results.All(r => r.CompletionText.Contains("Author", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void SelectGitCommit_Where_FilterByMerge_ReturnsMergeExample()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "Merge", null!, BoundParameters).ToList();

        Assert.IsNotEmpty(results, "Expected at least one result matching 'Merge'.");
        Assert.IsTrue(results.All(r =>
            r.CompletionText.Contains("Parents", StringComparison.OrdinalIgnoreCase)
            || r.ListItemText.Contains("merge", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void SelectGitCommit_Where_FilterIsCaseInsensitive()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var upper = completer.CompleteArgument("Select-GitCommit", "Where", "AUTHOR", null!, BoundParameters).ToList();
        var lower = completer.CompleteArgument("Select-GitCommit", "Where", "author", null!, BoundParameters).ToList();

        Assert.HasCount(upper.Count, lower);
    }

    [TestMethod]
    public void SelectGitCommit_Where_NoMatch_ReturnsEmpty()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "NONEXISTENT_XYZ_123", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void SelectGitCommit_Where_BracePrefix_StrippedForFiltering()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "{ Author", null!, BoundParameters).ToList();

        Assert.IsNotEmpty(results, "Expected results when filter starts with '{ '.");
        Assert.IsTrue(results.All(r => r.CompletionText.Contains("Author", StringComparison.OrdinalIgnoreCase)));
    }

    // ---------------------------------------------------------------
    //  Invoke-GitRepository -Action
    // ---------------------------------------------------------------

    [TestMethod]
    public void InvokeGitRepository_Action_EmptyWord_ReturnsAllExamples()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Invoke-GitRepository", "Action", "", null!, BoundParameters).ToList();

        Assert.HasCount(GitScriptBlockCompleterAttribute.InvokeGitRepositoryActionExamples.Count, results);
    }

    [TestMethod]
    public void InvokeGitRepository_Action_FilterByHead_ReturnsHeadExamples()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Invoke-GitRepository", "Action", "Head", null!, BoundParameters).ToList();

        Assert.IsNotEmpty(results, "Expected at least one result matching 'Head'.");
        Assert.IsTrue(results.All(r => r.CompletionText.Contains("Head", StringComparison.OrdinalIgnoreCase)));
    }

    // ---------------------------------------------------------------
    //  Registry & dispatch behaviour
    // ---------------------------------------------------------------

    [TestMethod]
    public void UnregisteredCommandParameter_ReturnsEmpty()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Nonexistent-Cmdlet", "Whatever", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void RegisterExamples_OverridesExisting()
    {
        GitScriptBlockCompleterAttribute.RegisterExamples("Select-GitCommit", "Where",
        [
            new("{ 'custom' }", "Custom example", "A custom tooltip."),
        ]);
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("{ 'custom' }", results[0].CompletionText);
    }

    [TestMethod]
    public void RegistryLookup_IsCaseInsensitive()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var lower = completer.CompleteArgument("select-gitcommit", "where", "", null!, BoundParameters).ToList();
        var upper = completer.CompleteArgument("SELECT-GITCOMMIT", "WHERE", "", null!, BoundParameters).ToList();

        Assert.HasCount(upper.Count, lower);
        Assert.IsNotEmpty(lower);
    }

    // ---------------------------------------------------------------
    //  General completion properties
    // ---------------------------------------------------------------

    [TestMethod]
    public void AllResults_HaveParameterValueType()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    [TestMethod]
    public void AllResults_HaveNonEmptyTooltip()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var results = completer.CompleteArgument("Select-GitCommit", "Where", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => !string.IsNullOrWhiteSpace(r.ToolTip)));
    }

    [TestMethod]
    public void AllResults_CompletionTextIsValidScriptBlockLiteral()
    {
        var completer = new GitScriptBlockCompleterAttribute.ScriptBlockCompleter();

        var allExamples = completer.CompleteArgument("Select-GitCommit", "Where", "", null!, BoundParameters)
            .Concat(completer.CompleteArgument("Invoke-GitRepository", "Action", "", null!, BoundParameters))
            .ToList();

        foreach (var result in allExamples)
        {
            Assert.StartsWith("{", result.CompletionText, $"Expected '{{' prefix: {result.CompletionText}");
            Assert.EndsWith("}", result.CompletionText, $"Expected '}}' suffix: {result.CompletionText}");
        }
    }

    [TestMethod]
    public void Create_ReturnsScriptBlockCompleterInstance()
    {
        var attribute = new GitScriptBlockCompleterAttribute();

        var completer = attribute.Create();

        Assert.IsInstanceOfType<GitScriptBlockCompleterAttribute.ScriptBlockCompleter>(completer);
    }
}
