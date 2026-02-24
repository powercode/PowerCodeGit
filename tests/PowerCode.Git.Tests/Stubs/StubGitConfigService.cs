using System;
using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A configurable stub for <see cref="IGitConfigService"/> suitable for use across cmdlet unit tests.
/// </summary>
/// <remarks>
/// Pass a list of <see cref="GitConfigEntry"/> values to control what
/// <see cref="GetConfigEntries"/> returns. When omitted, the stub returns an empty list.
/// </remarks>
internal sealed class StubGitConfigService(IReadOnlyList<GitConfigEntry>? entries = null) : IGitConfigService
{
    public IReadOnlyList<GitConfigEntry> GetConfigEntries(GitConfigGetOptions options) =>
        entries ?? Array.Empty<GitConfigEntry>();

    public GitConfigEntry? GetConfigValue(GitConfigGetOptions options) =>
        new() { Name = options.Name!, Value = "stub-value" };

    public void SetConfigValue(GitConfigSetOptions options)
    {
    }

    public void UnsetConfigValue(GitConfigUnsetOptions options)
    {
    }
}
