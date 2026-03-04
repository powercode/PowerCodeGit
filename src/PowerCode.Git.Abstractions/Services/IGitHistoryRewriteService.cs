using System;
using System.Collections.Generic;
using System.Threading;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Rewrites git repository history using caller-supplied filters, analogous to
/// <c>git filter-branch</c> or <c>git filter-repo</c> but with strongly-typed,
/// PowerShell-aware delegates.
/// </summary>
/// <remarks>
/// <para>
/// All delegate parameters use <see cref="object"/> for arguments that carry
/// LibGit2Sharp types (e.g. <c>Commit</c>, <c>TreeEntry</c>). This allows the
/// delegates to cross the Assembly Load Context boundary: the Core implementation
/// (isolated ALC) passes real LibGit2Sharp objects; the cmdlet layer (default ALC)
/// wraps PowerShell ScriptBlocks that access properties via PowerShell's ETS and
/// reflection.
/// </para>
/// <para>
/// Return types use only BCL or <c>PowerCode.Git.Abstractions</c> types so they
/// can flow back from the cmdlet layer to the Core layer without ALC conflicts.
/// </para>
/// </remarks>
public interface IGitHistoryRewriteService
{
    /// <summary>
    /// Rewrites history according to the supplied options and filter delegates.
    /// </summary>
    /// <param name="options">
    /// Configuration for the rewrite: repository path, target refs, backup namespace,
    /// pruning, and dry-run flag.
    /// </param>
    /// <param name="commitFilter">
    /// Optional delegate invoked for each commit. The argument is a
    /// <c>LibGit2Sharp.Commit</c> passed as <see cref="object"/>.
    /// Return <see langword="null"/> to keep the commit metadata unchanged, or a
    /// <see cref="GitCommitRewriteOverrides"/> with non-null fields to override the
    /// corresponding metadata.
    /// </param>
    /// <param name="treeFilter">
    /// Optional delegate invoked for each blob entry in each commit's tree. The argument
    /// is a <c>TreeEntryContext</c> (internal Core type) passed as <see cref="object"/>;
    /// it exposes <c>Path</c>, <c>Mode</c>, and <c>ObjectId</c> properties accessible
    /// via PowerShell ETS. Return <see langword="true"/> to keep the entry,
    /// <see langword="false"/> to remove it from the rewritten tree.
    /// </param>
    /// <param name="parentsRewriter">
    /// Optional delegate invoked for each commit. The argument is a
    /// <c>LibGit2Sharp.Commit</c> passed as <see cref="object"/>.
    /// Return <see langword="null"/> to keep the original parents, or a sequence of
    /// SHA strings to replace them. Core resolves each SHA to a <c>Commit</c> object.
    /// </param>
    /// <param name="tagNameRewriter">
    /// Optional delegate invoked for each tag that points to a rewritten commit.
    /// Arguments are <c>(oldTagName, isAnnotated, oldTargetIdentifier)</c>.
    /// Return the new tag name, or <see langword="null"/> to keep the original name.
    /// </param>
    /// <param name="cancellationToken">
    /// Token that cancels the operation between commit boundaries. When cancelled,
    /// the method throws <see cref="System.OperationCanceledException"/> before
    /// any further commits are processed. The repository is left in a consistent
    /// state because cancellation is only checked before (not during) each commit
    /// rewrite callback.
    /// </param>
    /// <returns>
    /// One <see cref="GitRewrittenCommitInfo"/> per commit that was (or in dry-run mode,
    /// would be) rewritten. Commits with no modifications are omitted from the list.
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown when no filter is provided, or <see cref="GitHistoryRewriteOptions.RepositoryPath"/>
    /// does not reference a valid git repository.
    /// </exception>
    IReadOnlyList<GitRewrittenCommitInfo> Rewrite(
        GitHistoryRewriteOptions options,
        Func<object, GitCommitRewriteOverrides?>? commitFilter = null,
        Func<object, bool>? treeFilter = null,
        Func<object, IEnumerable<string>?>? parentsRewriter = null,
        Func<string, bool, string, string?>? tagNameRewriter = null,
        CancellationToken cancellationToken = default);
}
