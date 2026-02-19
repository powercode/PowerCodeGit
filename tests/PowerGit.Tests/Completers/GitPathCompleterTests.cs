using System.Collections;
using System.Management.Automation;
using PowerGit.Abstractions.Services;
using PowerGit.Completers;

namespace PowerGit.Tests.Completers;

[TestClass]
public sealed class GitPathCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestMethod]
    public void CompleteArgument_EmptyWord_ReturnsAllPaths()
    {
        var service = new StubGitPathService(["src/Program.cs", "src/Utils.cs", "README.md"]);
        var completer = new GitPathCompleterAttribute.PathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "", null!, BoundParameters).ToList();

        Assert.HasCount(3, results);
    }

    [TestMethod]
    public void CompleteArgument_PrefixFilter_ReturnsOnlyMatching()
    {
        var service = new StubGitPathService(["src/Program.cs", "src/Utils.cs", "README.md"]);
        var completer = new GitPathCompleterAttribute.PathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "src/", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(r => r.CompletionText.StartsWith("src/")));
    }

    [TestMethod]
    public void CompleteArgument_CaseInsensitiveMatch_ReturnsMatching()
    {
        var service = new StubGitPathService(["README.md", "src/File.cs"]);
        var completer = new GitPathCompleterAttribute.PathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "readme", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("README.md", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_NoMatch_ReturnsEmpty()
    {
        var service = new StubGitPathService(["src/Program.cs"]);
        var completer = new GitPathCompleterAttribute.PathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "xyz", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_ResultsAreAlphabeticallySorted()
    {
        var service = new StubGitPathService(["src/Zebra.cs", "src/Alpha.cs", "src/Middle.cs"]);
        var completer = new GitPathCompleterAttribute.PathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "src/", null!, BoundParameters).ToList();

        Assert.HasCount(3, results);
        Assert.AreEqual("src/Alpha.cs", results[0].CompletionText);
        Assert.AreEqual("src/Middle.cs", results[1].CompletionText);
        Assert.AreEqual("src/Zebra.cs", results[2].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitPathService();
        var completer = new GitPathCompleterAttribute.PathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_AllResults_HaveParameterValueType()
    {
        var service = new StubGitPathService(["src/Program.cs", "README.md"]);
        var completer = new GitPathCompleterAttribute.PathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    private sealed class StubGitPathService(IReadOnlyList<string> paths) : IGitPathService
    {
        public IReadOnlyList<string> GetTrackedPaths(string repositoryPath) => paths;
    }

    private sealed class ThrowingGitPathService : IGitPathService
    {
        public IReadOnlyList<string> GetTrackedPaths(string repositoryPath) =>
            throw new InvalidOperationException("Not a git repository");
    }
}
