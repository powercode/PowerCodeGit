using System;
using System.Collections.Generic;
using System.Threading;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Tests.Stubs;

/// <summary>
/// A no-op stub for <see cref="IGitCommitSearchService"/> suitable for use in cmdlet unit tests.
/// </summary>
internal sealed class StubGitCommitSearchService : IGitCommitSearchService
{
    public IEnumerable<GitCommitInfo> Search(
        GitCommitSearchOptions options,
        Func<object, bool>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return Array.Empty<GitCommitInfo>();
    }
}
