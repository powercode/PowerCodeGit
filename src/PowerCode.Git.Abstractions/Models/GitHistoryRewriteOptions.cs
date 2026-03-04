using System;
using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options that control a <c>Edit-GitHistory</c> history-rewrite operation.
/// </summary>
/// <remarks>
/// Passed from the cmdlet layer to <c>IGitHistoryRewriteService.Rewrite</c>.
/// All properties use BCL types so the object crosses the Assembly Load Context
/// boundary cleanly. Filter delegates are passed separately because they reference
/// <see cref="GitCommitRewriteOverrides"/>, another shared Abstractions type.
/// </remarks>
public sealed class GitHistoryRewriteOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the canonical names of the refs whose reachable commits should be rewritten.
    /// When <see langword="null"/> or empty, all local branch heads are targeted.
    /// </summary>
    /// <example>
    /// <code>new[] { "refs/heads/main", "refs/heads/feature/work" }</code>
    /// </example>
    public IReadOnlyList<string>? Refs { get; init; }

    /// <summary>
    /// Gets the namespace under which the original (pre-rewrite) refs are backed up.
    /// </summary>
    /// <remarks>
    /// LibGit2Sharp creates one backup ref per rewritten branch: for a branch
    /// <c>refs/heads/main</c> the backup is <c>refs/original/refs/heads/main</c>.
    /// Defaults to <c>"refs/original/"</c>.
    /// </remarks>
    public string BackupRefsNamespace { get; init; } = "refs/original/";

    /// <summary>
    /// Gets a value indicating whether commits whose tree is empty after tree filtering
    /// should be removed from history.
    /// </summary>
    /// <remarks>
    /// Passed directly to <c>RewriteHistoryOptions.PruneEmptyCommits</c> and has no effect
    /// when a <c>CommitTreeRewriter</c> is not in use.
    /// </remarks>
    public bool PruneEmptyCommits { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation should simulate the rewrite without
    /// modifying the repository. When <see langword="true"/>, the service analyses each commit
    /// and returns a <see cref="GitRewrittenCommitInfo"/> per commit that <em>would</em> be
    /// changed, but no refs or objects are written.
    /// </summary>
    public bool DryRun { get; init; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"GitHistoryRewriteOptions(repositoryPath={RepositoryPath}, dryRun={DryRun}, pruneEmpty={PruneEmptyCommits})";
}
