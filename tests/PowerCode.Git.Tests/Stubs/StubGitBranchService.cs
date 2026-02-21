using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitBranchService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitBranchService : IGitBranchService
{
    public IReadOnlyList<GitBranchInfo> GetBranches(GitBranchListOptions options) =>
        Array.Empty<GitBranchInfo>();

    public GitBranchInfo SwitchBranch(GitSwitchOptions options) =>
        new(options.BranchName ?? "HEAD", true, false, "abc1234", null, null, null);

    public GitBranchInfo CreateBranch(GitBranchCreateOptions options) =>
        new(options.Name, true, false, "abc1234", null, null, null);

    public void DeleteBranch(GitBranchDeleteOptions options)
    {
    }
}
