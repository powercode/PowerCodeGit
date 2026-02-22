using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitRebaseService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitRebaseService : IGitRebaseService
{
    public GitRebaseResult Start(GitRebaseOptions options) =>
        new(success: true, hasConflicts: false, output: string.Empty);

    public GitRebaseResult Continue(GitRebaseContinueOptions options) =>
        new(success: true, hasConflicts: false, output: string.Empty);

    public void Abort(string repositoryPath)
    {
    }
}
