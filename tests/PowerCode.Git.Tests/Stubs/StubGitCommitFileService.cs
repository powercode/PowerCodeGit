using System;
using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitCommitFileService"/> suitable for use across cmdlet unit tests.
/// </summary>
internal sealed class StubGitCommitFileService : IGitCommitFileService
{
    public IReadOnlyList<GitDiffEntry> GetCommitFiles(GitCommitFileOptions options) =>
        Array.Empty<GitDiffEntry>();
}
