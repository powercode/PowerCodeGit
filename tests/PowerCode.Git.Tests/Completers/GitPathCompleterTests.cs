using System.Collections;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Tests.Completers;

[TestClass]
public sealed class GitPathCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestMethod]
    public void CompleteArgument_EmptyWord_ReturnsAllPaths()
    {
        var service = new StubGitPathService(["src/Program.cs", "src/Utils.cs", "README.md"]);
        var completer = new GitPathCompleterAttribute.TrackedPathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "", null!, BoundParameters).ToList();

        Assert.HasCount(3, results);
    }

    [TestMethod]
    public void CompleteArgument_PrefixFilter_ReturnsOnlyMatching()
    {
        var service = new StubGitPathService(["src/Program.cs", "src/Utils.cs", "README.md"]);
        var completer = new GitPathCompleterAttribute.TrackedPathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "src/", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
        Assert.IsTrue(results.All(r => r.CompletionText.StartsWith("src/")));
    }

    [TestMethod]
    public void CompleteArgument_CaseInsensitiveMatch_ReturnsMatching()
    {
        var service = new StubGitPathService(["README.md", "src/File.cs"]);
        var completer = new GitPathCompleterAttribute.TrackedPathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "readme", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("README.md", results[0].CompletionText);
    }

    [TestMethod]
    public void CompleteArgument_NoMatch_ReturnsEmpty()
    {
        var service = new StubGitPathService(["src/Program.cs"]);
        var completer = new GitPathCompleterAttribute.TrackedPathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "xyz", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_ResultsAreAlphabeticallySorted()
    {
        var service = new StubGitPathService(["src/Zebra.cs", "src/Alpha.cs", "src/Middle.cs"]);
        var completer = new GitPathCompleterAttribute.TrackedPathCompleter(service);

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
        var completer = new GitPathCompleterAttribute.TrackedPathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_AllResults_HaveParameterValueType()
    {
        var service = new StubGitPathService(["src/Program.cs", "README.md"]);
        var completer = new GitPathCompleterAttribute.TrackedPathCompleter(service);

        var results = completer.CompleteArgument("Get-GitDiff", "FilePath", "", null!, BoundParameters).ToList();

        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    [TestMethod]
    public void StatusCompleter_IncludeModified_ReturnsOnlyModifiedFiles()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("untracked.cs", GitFileStatus.Untracked, GitStagingState.Unstaged),
            new GitStatusEntry("staged.cs", GitFileStatus.Added, GitStagingState.Staged),
        ]);
        var completer = new GitPathCompleterAttribute.StatusPathCompleter(service, includeModified: true, includeUntracked: false);

        var results = completer.CompleteArgument("Add-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("modified.cs", results[0].CompletionText);
    }

    [TestMethod]
    public void StatusCompleter_IncludeUntracked_ReturnsOnlyUntrackedFiles()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("untracked.cs", GitFileStatus.Untracked, GitStagingState.Unstaged),
        ]);
        var completer = new GitPathCompleterAttribute.StatusPathCompleter(service, includeModified: false, includeUntracked: true);

        var results = completer.CompleteArgument("Add-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("untracked.cs", results[0].CompletionText);
    }

    [TestMethod]
    public void StatusCompleter_IncludeBoth_ReturnsModifiedAndUntracked()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("untracked.cs", GitFileStatus.Untracked, GitStagingState.Unstaged),
            new GitStatusEntry("staged.cs", GitFileStatus.Added, GitStagingState.Staged),
        ]);
        var completer = new GitPathCompleterAttribute.StatusPathCompleter(service, includeModified: true, includeUntracked: true);

        var results = completer.CompleteArgument("Add-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void StatusCompleter_ExcludesStagedEntries()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("staged-modified.cs", GitFileStatus.Modified, GitStagingState.Staged),
            new GitStatusEntry("unstaged-modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
        ]);
        var completer = new GitPathCompleterAttribute.StatusPathCompleter(service, includeModified: true, includeUntracked: true);

        var results = completer.CompleteArgument("Add-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("unstaged-modified.cs", results[0].CompletionText);
    }

    [TestMethod]
    public void StatusCompleter_ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitWorkingTreeService();
        var completer = new GitPathCompleterAttribute.StatusPathCompleter(service, includeModified: true, includeUntracked: true);

        var results = completer.CompleteArgument("Add-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void StatusCompleter_FiltersAndSortsByWordToComplete()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("src/Zebra.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("src/Alpha.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("README.md", GitFileStatus.Modified, GitStagingState.Unstaged),
        ]);
        var completer = new GitPathCompleterAttribute.StatusPathCompleter(service, includeModified: true, includeUntracked: false);

        var results = completer.CompleteArgument("Add-GitItem", "Path", "src/", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
        Assert.AreEqual("src/Alpha.cs", results[0].CompletionText);
        Assert.AreEqual("src/Zebra.cs", results[1].CompletionText);
    }

    [TestMethod]
    public void StagedCompleter_ReturnsOnlyStagedFiles()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("staged-added.cs", GitFileStatus.Added, GitStagingState.Staged),
            new GitStatusEntry("staged-modified.cs", GitFileStatus.Modified, GitStagingState.Staged),
            new GitStatusEntry("unstaged-modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("untracked.cs", GitFileStatus.Untracked, GitStagingState.Unstaged),
        ]);
        var completer = new GitPathCompleterAttribute.StagedPathCompleter(service);

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
        Assert.AreEqual("staged-added.cs", results[0].CompletionText);
        Assert.AreEqual("staged-modified.cs", results[1].CompletionText);
    }

    [TestMethod]
    public void StagedCompleter_FiltersAndSortsByWordToComplete()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("src/Zebra.cs", GitFileStatus.Modified, GitStagingState.Staged),
            new GitStatusEntry("src/Alpha.cs", GitFileStatus.Added, GitStagingState.Staged),
            new GitStatusEntry("README.md", GitFileStatus.Modified, GitStagingState.Staged),
        ]);
        var completer = new GitPathCompleterAttribute.StagedPathCompleter(service);

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "src/", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
        Assert.AreEqual("src/Alpha.cs", results[0].CompletionText);
        Assert.AreEqual("src/Zebra.cs", results[1].CompletionText);
    }

    [TestMethod]
    public void StagedCompleter_ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitWorkingTreeService();
        var completer = new GitPathCompleterAttribute.StagedPathCompleter(service);

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
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

    private sealed class StubGitWorkingTreeService(IReadOnlyList<GitStatusEntry> entries) : IGitWorkingTreeService
    {
        public GitStatusResult GetStatus(GitStatusOptions options)
            => new("C:\\repo", "main", entries, 0, 0, 0);

        public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options) => [];
        public void Stage(GitStageOptions options) { }
        public void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null) { }
        public void Reset(GitResetOptions options) { }
        public void StageHunks(GitStageHunkOptions options) { }
        public void Restore(GitRestoreOptions options) { }
        public void RestoreHunks(GitRestoreHunkOptions options) { }
        public GitWorkingTreePromptInfo GetPromptInfo(string repositoryPath) =>
            new("main", false, null, null, null, 0, 0, 0, 0);
    }

    private sealed class ThrowingGitWorkingTreeService : IGitWorkingTreeService
    {
        public GitStatusResult GetStatus(GitStatusOptions options)
            => throw new InvalidOperationException("Not a git repository");

        public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options) => [];
        public void Stage(GitStageOptions options) { }
        public void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null) { }
        public void Reset(GitResetOptions options) { }
        public void StageHunks(GitStageHunkOptions options) { }
        public void Restore(GitRestoreOptions options) { }
        public void RestoreHunks(GitRestoreHunkOptions options) { }
        public GitWorkingTreePromptInfo GetPromptInfo(string repositoryPath) =>
            new("main", false, null, null, null, 0, 0, 0, 0);
    }
}
