using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitWorkingTreeService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitWorkingTreeService : IGitWorkingTreeService
{
    public GitStatusResult GetStatus(GitStatusOptions options) =>
        new(options.RepositoryPath, "main", Array.Empty<GitStatusEntry>(), 0, 0, 0);

    public IReadOnlyList<GitDiffEntry> GetDiff(GitDiffOptions options) =>
        Array.Empty<GitDiffEntry>();

    public void Stage(GitStageOptions options)
    {
    }

    public void Unstage(string repositoryPath, IReadOnlyList<string>? paths = null)
    {
    }

    public void Reset(GitResetOptions options)
    {
    }
}
