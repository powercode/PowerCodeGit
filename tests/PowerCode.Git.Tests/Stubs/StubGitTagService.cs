using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitTagService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitTagService : IGitTagService
{
    public IReadOnlyList<GitTagInfo> GetTags(GitTagListOptions options) =>
        Array.Empty<GitTagInfo>();

    public GitTagInfo CreateTag(GitTagCreateOptions options) =>
        new(options.Name, "abc1234abc1234abc1234abc1234abc1234abc1234", isAnnotated: options.Message is not null, null, null, null, options.Message);

    public void DeleteTag(GitTagDeleteOptions options)
    {
        // No-op stub — records nothing by default.
    }
}
