using System;
using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Searches git commit history using optional pickaxe content search and
/// an optional caller-supplied predicate.
/// </summary>
public interface IGitCommitSearchService
{
    /// <summary>
    /// Searches commit history according to <paramref name="options"/> and an
    /// optional <paramref name="predicate"/>.
    /// </summary>
    /// <param name="options">Search options including repository path, starting ref,
    /// path filter, content search, and result limit.</param>
    /// <param name="predicate">
    /// An optional predicate invoked for each candidate commit. The argument is a
    /// <c>LibGit2Sharp.Commit</c> instance from the isolated AssemblyLoadContext,
    /// passed as <see cref="object"/> to avoid an ALC-boundary type dependency.
    /// Return <see langword="true"/> to include the commit in results.
    /// When <see langword="null"/>, all candidates that pass the compiled filters
    /// in <paramref name="options"/> are included.
    /// </param>
    /// <returns>
    /// A lazily evaluated sequence of <see cref="GitCommitInfo"/> objects for each
    /// matching commit. Enumeration stops automatically when
    /// <see cref="GitCommitSearchOptions.MaxCount"/> matches have been yielded.
    /// </returns>
    IEnumerable<GitCommitInfo> Search(GitCommitSearchOptions options, Func<object, bool>? predicate = null);
}
