using System;
using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitConfigService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitConfigService : IGitConfigService
{
    public IReadOnlyList<GitConfigEntry> GetConfigEntries(GitConfigGetOptions options) =>
        Array.Empty<GitConfigEntry>();

    public GitConfigEntry? GetConfigValue(GitConfigGetOptions options) =>
        new() { Name = options.Name!, Value = "stub-value" };

    public void SetConfigValue(GitConfigSetOptions options)
    {
    }
}
