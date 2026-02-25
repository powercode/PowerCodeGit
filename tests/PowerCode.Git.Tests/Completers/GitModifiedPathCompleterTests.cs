using System.Collections;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Tests.Completers;

[TestClass]
public sealed class GitModifiedPathCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestMethod]
    public void WithoutStagedParameter_ReturnsUnstagedModifiedFiles()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("deleted.cs", GitFileStatus.Deleted, GitStagingState.Unstaged),
            new GitStatusEntry("untracked.cs", GitFileStatus.Untracked, GitStagingState.Unstaged),
            new GitStatusEntry("staged.cs", GitFileStatus.Added, GitStagingState.Staged),
        ]);
        var completer = new GitModifiedPathCompleterAttribute.ModifiedPathCompleter(service, "Staged");

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
        Assert.AreEqual("deleted.cs", results[0].CompletionText);
        Assert.AreEqual("modified.cs", results[1].CompletionText);
    }

    [TestMethod]
    public void WithStagedParameter_ReturnsStagedFiles()
    {
        var boundParams = new Hashtable { ["RepoPath"] = "C:\\repo", ["Staged"] = true };
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("staged-added.cs", GitFileStatus.Added, GitStagingState.Staged),
            new GitStatusEntry("staged-modified.cs", GitFileStatus.Modified, GitStagingState.Staged),
        ]);
        var completer = new GitModifiedPathCompleterAttribute.ModifiedPathCompleter(service, "Staged");

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, boundParams).ToList();

        Assert.HasCount(2, results);
        Assert.AreEqual("staged-added.cs", results[0].CompletionText);
        Assert.AreEqual("staged-modified.cs", results[1].CompletionText);
    }

    [TestMethod]
    public void ExcludesUntrackedFromUnstagedResults()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("untracked.cs", GitFileStatus.Untracked, GitStagingState.Unstaged),
        ]);
        var completer = new GitModifiedPathCompleterAttribute.ModifiedPathCompleter(service, "Staged");

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("modified.cs", results[0].CompletionText);
    }

    [TestMethod]
    public void IncludesRenamedInUnstagedResults()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("renamed.cs", GitFileStatus.Renamed, GitStagingState.Unstaged),
        ]);
        var completer = new GitModifiedPathCompleterAttribute.ModifiedPathCompleter(service, "Staged");

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("renamed.cs", results[0].CompletionText);
    }

    [TestMethod]
    public void FiltersAndSortsByWordToComplete()
    {
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("src/Zebra.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("src/Alpha.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("README.md", GitFileStatus.Modified, GitStagingState.Unstaged),
        ]);
        var completer = new GitModifiedPathCompleterAttribute.ModifiedPathCompleter(service, "Staged");

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "src/", null!, BoundParameters).ToList();

        Assert.HasCount(2, results);
        Assert.AreEqual("src/Alpha.cs", results[0].CompletionText);
        Assert.AreEqual("src/Zebra.cs", results[1].CompletionText);
    }

    [TestMethod]
    public void ServiceThrows_ReturnsEmpty()
    {
        var service = new ThrowingGitWorkingTreeService();
        var completer = new GitModifiedPathCompleterAttribute.ModifiedPathCompleter(service, "Staged");

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void EmptyStagedParameterName_AlwaysReturnsUnstaged()
    {
        var boundParams = new Hashtable { ["RepoPath"] = "C:\\repo", ["Staged"] = true };
        var service = new StubGitWorkingTreeService([
            new GitStatusEntry("modified.cs", GitFileStatus.Modified, GitStagingState.Unstaged),
            new GitStatusEntry("staged.cs", GitFileStatus.Added, GitStagingState.Staged),
        ]);
        var completer = new GitModifiedPathCompleterAttribute.ModifiedPathCompleter(service, "");

        var results = completer.CompleteArgument("Restore-GitItem", "Path", "", null!, boundParams).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("modified.cs", results[0].CompletionText);
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
