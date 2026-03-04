using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Implements <see cref="IGitHistoryRewriteService"/> using the LibGit2Sharp
/// <c>Repository.Refs.RewriteHistory</c> API.
/// </summary>
public sealed class GitHistoryRewriteService : IGitHistoryRewriteService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitRewrittenCommitInfo> Rewrite(
        GitHistoryRewriteOptions options,
        Func<object, GitCommitRewriteOverrides?>? commitFilter = null,
        Func<object, bool>? treeFilter = null,
        Func<object, IEnumerable<string>?>? parentsRewriter = null,
        Func<string, bool, string, string?>? tagNameRewriter = null,
        CancellationToken cancellationToken = default)
    {
        // Require at least one filter — a no-op rewrite is a user error.
        if (commitFilter is null && treeFilter is null && parentsRewriter is null && tagNameRewriter is null)
        {
            throw new ArgumentException(
                "At least one filter (CommitFilter, TreeFilter, ParentsRewriter, or TagNameRewriter) must be provided.",
                nameof(commitFilter));
        }

        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        cancellationToken.ThrowIfCancellationRequested();

        using var repository = new Repository(options.RepositoryPath);

        var commitsToRewrite = GatherCommits(repository, options, cancellationToken);

        return options.DryRun
            ? SimulateDryRun(commitsToRewrite, commitFilter, treeFilter, parentsRewriter, cancellationToken)
            : ExecuteRewrite(repository, commitsToRewrite, options, commitFilter, treeFilter, parentsRewriter, tagNameRewriter, cancellationToken);
    }

    // ─── Core execution ────────────────────────────────────────────────────────

    private static IReadOnlyList<GitRewrittenCommitInfo> ExecuteRewrite(
        Repository repository,
        IReadOnlyList<Commit> commitsToRewrite,
        GitHistoryRewriteOptions options,
        Func<object, GitCommitRewriteOverrides?>? commitFilter,
        Func<object, bool>? treeFilter,
        Func<object, IEnumerable<string>?>? parentsRewriter,
        Func<string, bool, string, string?>? tagNameRewriter,
        CancellationToken cancellationToken)
    {
        // Simulate first so we can skip the destructive rewrite when nothing would change.
        // LibGit2Sharp's RewriteHistory rewrites every commit (new SHAs) even when callbacks
        // return identical values, so calling it unnecessarily would silently corrupt history.
        var preview = SimulateDryRun(commitsToRewrite, commitFilter, treeFilter, parentsRewriter, cancellationToken);
        if (preview.Count == 0)
        {
            return [];
        }

        var tracking = commitsToRewrite.ToDictionary(c => c.Sha, CreateTracking);

        var rewriteOptions = BuildRewriteHistoryOptions(
            repository, options, tracking,
            commitFilter, treeFilter, parentsRewriter, tagNameRewriter,
            cancellationToken);

        repository.Refs.RewriteHistory(rewriteOptions, commitsToRewrite);

        return CollectModifiedCommits(tracking);
    }

    private static RewriteHistoryOptions BuildRewriteHistoryOptions(
        Repository repository,
        GitHistoryRewriteOptions options,
        Dictionary<string, CommitTracking> tracking,
        Func<object, GitCommitRewriteOverrides?>? commitFilter,
        Func<object, bool>? treeFilter,
        Func<object, IEnumerable<string>?>? parentsRewriter,
        Func<string, bool, string, string?>? tagNameRewriter,
        CancellationToken cancellationToken)
    {
        var rewriteOptions = new RewriteHistoryOptions
        {
            BackupRefsNamespace = options.BackupRefsNamespace,
            PruneEmptyCommits = options.PruneEmptyCommits,
        };

        if (commitFilter is not null)
            rewriteOptions.CommitHeaderRewriter = BuildCommitHeaderRewriter(commitFilter, tracking, cancellationToken);

        if (treeFilter is not null)
            rewriteOptions.CommitTreeRewriter = BuildCommitTreeRewriter(treeFilter, tracking, cancellationToken);

        if (parentsRewriter is not null)
            rewriteOptions.CommitParentsRewriter = BuildCommitParentsRewriter(repository, parentsRewriter, tracking, cancellationToken);

        if (tagNameRewriter is not null)
        {
            // LibGit2Sharp requires a non-null return; fall back to the original name when
            // the caller's delegate returns null (meaning "keep unchanged").
            rewriteOptions.TagNameRewriter = (oldName, isAnnotated, oldTarget) =>
                tagNameRewriter(oldName, isAnnotated, oldTarget) ?? oldName;
        }

        return rewriteOptions;
    }

    private static Func<Commit, CommitRewriteInfo> BuildCommitHeaderRewriter(
        Func<object, GitCommitRewriteOverrides?> commitFilter,
        Dictionary<string, CommitTracking> tracking,
        CancellationToken cancellationToken)
    {
        return commit =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var overrides = commitFilter(commit);
            if (overrides is null)
                return CommitRewriteInfo.From(commit);

            var newAuthor = BuildSignature(commit.Author, overrides.AuthorName, overrides.AuthorEmail, overrides.AuthorWhen);
            var newCommitter = BuildSignature(commit.Committer, overrides.CommitterName, overrides.CommitterEmail, overrides.CommitterWhen);
            var newMessage = overrides.Message ?? commit.Message;

            if (tracking.TryGetValue(commit.Sha, out var info))
            {
                info.HeaderModified = !SignatureEquals(newAuthor, commit.Author) || !SignatureEquals(newCommitter, commit.Committer);
                info.MessageModified = newMessage != commit.Message;
            }

            return new CommitRewriteInfo { Author = newAuthor, Committer = newCommitter, Message = newMessage };
        };
    }

    private static Func<Commit, TreeDefinition> BuildCommitTreeRewriter(
        Func<object, bool> treeFilter,
        Dictionary<string, CommitTracking> tracking,
        CancellationToken cancellationToken)
    {
        return commit =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var treeDef = TreeDefinition.From(commit);
            var anyRemoved = RemoveFilteredEntries(treeDef, commit.Tree, treeFilter, "");

            if (anyRemoved && tracking.TryGetValue(commit.Sha, out var info))
                info.TreeModified = true;

            return treeDef;
        };
    }

    private static Func<Commit, IEnumerable<Commit>> BuildCommitParentsRewriter(
        Repository repository,
        Func<object, IEnumerable<string>?> parentsRewriter,
        Dictionary<string, CommitTracking> tracking,
        CancellationToken cancellationToken)
    {
        return commit =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var newParentShas = parentsRewriter(commit);
            if (newParentShas is null)
                return commit.Parents;

            var newParents = newParentShas
                .Select(sha => repository.Lookup<Commit>(sha)
                    ?? throw new ArgumentException($"Parent SHA '{sha}' was not found in the repository."))
                .ToList();

            var originalShas = commit.Parents.Select(p => p.Sha).OrderBy(s => s).ToList();
            var newShas = newParents.Select(p => p.Sha).OrderBy(s => s).ToList();

            if (!originalShas.SequenceEqual(newShas) && tracking.TryGetValue(commit.Sha, out var info))
                info.ParentsModified = true;

            return newParents;
        };
    }

    private static IReadOnlyList<GitRewrittenCommitInfo> CollectModifiedCommits(Dictionary<string, CommitTracking> tracking) =>
        tracking.Values
            .Where(t => t.HeaderModified || t.MessageModified || t.TreeModified || t.ParentsModified)
            .Select(t => t.Build())
            .ToList()
            .AsReadOnly();

    // ─── Dry-run simulation ─────────────────────────────────────────────────────

    private static IReadOnlyList<GitRewrittenCommitInfo> SimulateDryRun(
        IReadOnlyList<Commit> commitsToRewrite,
        Func<object, GitCommitRewriteOverrides?>? commitFilter,
        Func<object, bool>? treeFilter,
        Func<object, IEnumerable<string>?>? parentsRewriter,
        CancellationToken cancellationToken)
    {
        var results = new List<GitRewrittenCommitInfo>();

        foreach (var commit in commitsToRewrite)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = CreateTracking(commit);

            SimulateCommitFilter(commit, commitFilter, info);
            SimulateTreeFilter(commit, treeFilter, info);
            SimulateParentsRewriter(commit, parentsRewriter, info);

            if (info.HeaderModified || info.MessageModified || info.TreeModified || info.ParentsModified)
                results.Add(info.Build());
        }

        return results.AsReadOnly();
    }

    private static void SimulateCommitFilter(
        Commit commit,
        Func<object, GitCommitRewriteOverrides?>? commitFilter,
        CommitTracking info)
    {
        if (commitFilter is null) return;

        var overrides = commitFilter(commit);
        if (overrides is null) return;

        var newAuthor = BuildSignature(commit.Author, overrides.AuthorName, overrides.AuthorEmail, overrides.AuthorWhen);
        var newCommitter = BuildSignature(commit.Committer, overrides.CommitterName, overrides.CommitterEmail, overrides.CommitterWhen);

        info.HeaderModified = !SignatureEquals(newAuthor, commit.Author) || !SignatureEquals(newCommitter, commit.Committer);
        info.MessageModified = overrides.Message is not null && overrides.Message != commit.Message;
    }

    private static void SimulateTreeFilter(
        Commit commit,
        Func<object, bool>? treeFilter,
        CommitTracking info)
    {
        if (treeFilter is null) return;
        info.TreeModified = WouldRemoveAnyEntry(commit.Tree, treeFilter, "");
    }

    private static void SimulateParentsRewriter(
        Commit commit,
        Func<object, IEnumerable<string>?>? parentsRewriter,
        CommitTracking info)
    {
        if (parentsRewriter is null) return;

        var newParentShas = parentsRewriter(commit);
        if (newParentShas is null) return;

        var originalShas = commit.Parents.Select(p => p.Sha).OrderBy(s => s);
        var newShas = newParentShas.OrderBy(s => s);
        info.ParentsModified = !originalShas.SequenceEqual(newShas);
    }

    // ─── Tree walking helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Recursively walks <paramref name="tree"/> and removes any blob entry for which
    /// <paramref name="predicate"/> returns <see langword="false"/> from <paramref name="treeDef"/>.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if at least one entry was removed; otherwise <see langword="false"/>.
    /// </returns>
    private static bool RemoveFilteredEntries(
        TreeDefinition treeDef,
        Tree tree,
        Func<object, bool> predicate,
        string prefix)
    {
        var anyRemoved = false;

        foreach (var entry in tree)
        {
            var fullPath = prefix.Length == 0 ? entry.Name : $"{prefix}/{entry.Name}";

            switch (entry.TargetType)
            {
                case TreeEntryTargetType.Blob:
                {
                    // Wrap the entry in a context object so the ScriptBlock can access
                    // Path alongside the standard TreeEntry properties. Since this object
                    // is passed as `object` across the ALC boundary, PowerShell ETS reads
                    // its properties via reflection.
                    var context = new TreeEntryContext(fullPath, entry.Mode.ToString(), entry.Target.Id.Sha);
                    if (!predicate(context))
                    {
                        treeDef.Remove(fullPath);
                        anyRemoved = true;
                    }

                    break;
                }

                case TreeEntryTargetType.Tree:
                {
                    var subtree = (Tree)entry.Target;
                    anyRemoved |= RemoveFilteredEntries(treeDef, subtree, predicate, fullPath);
                    break;
                }

                // TreeLink (submodule) entries are passed through unchanged.
            }
        }

        return anyRemoved;
    }

    /// <summary>
    /// Walks <paramref name="tree"/> recursively and returns <see langword="true"/> if
    /// <paramref name="predicate"/> returns <see langword="false"/> for any blob entry.
    /// Short-circuits on the first match.
    /// </summary>
    private static bool WouldRemoveAnyEntry(Tree tree, Func<object, bool> predicate, string prefix)
    {
        foreach (var entry in tree)
        {
            var fullPath = prefix.Length == 0 ? entry.Name : $"{prefix}/{entry.Name}";

            switch (entry.TargetType)
            {
                case TreeEntryTargetType.Blob:
                {
                    var context = new TreeEntryContext(fullPath, entry.Mode.ToString(), entry.Target.Id.Sha);
                    if (!predicate(context))
                    {
                        return true;
                    }

                    break;
                }

                case TreeEntryTargetType.Tree:
                {
                    var subtree = (Tree)entry.Target;
                    if (WouldRemoveAnyEntry(subtree, predicate, fullPath))
                    {
                        return true;
                    }

                    break;
                }
            }
        }

        return false;
    }

    // ─── Commit gathering ───────────────────────────────────────────────────────

    private static IReadOnlyList<Commit> GatherCommits(
        Repository repository,
        GitHistoryRewriteOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return options.Refs is { Count: > 0 }
            ? GatherCommitsByRefs(repository, options.Refs)
            : GatherCommitsByLocalBranches(repository);
    }

    private static IReadOnlyList<Commit> GatherCommitsByRefs(Repository repository, IReadOnlyList<string> refs)
    {
        var tipCommits = refs
            .Select(refName => ResolveRef(repository, refName))
            .Where(c => c is not null)
            .Select(c => c!)
            .Distinct()
            .ToList();

        return QueryCommitsTopological(repository, tipCommits);
    }

    private static IReadOnlyList<Commit> GatherCommitsByLocalBranches(Repository repository)
    {
        var branchTips = repository.Branches
            .Where(b => !b.IsRemote)
            .Select(b => b.Tip)
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList();

        return branchTips.Count == 0 ? [] : QueryCommitsTopological(repository, branchTips);
    }

    private static IReadOnlyList<Commit> QueryCommitsTopological(Repository repository, IEnumerable<Commit> tips)
    {
        var filter = new CommitFilter
        {
            SortBy = CommitSortStrategies.Topological | CommitSortStrategies.Reverse,
            IncludeReachableFrom = tips,
        };
        return repository.Commits.QueryBy(filter).ToList();
    }

    private static Commit? ResolveRef(Repository repository, string refName)
    {
        // Try as a direct ref name first, then as a branch, tag, or committish.
        var gitRef = repository.Refs[refName];
        if (gitRef is not null)
        {
            return repository.Lookup<Commit>(gitRef.TargetIdentifier);
        }

        var branch = repository.Branches[refName];
        if (branch is not null)
        {
            return branch.Tip;
        }

        if (repository.Tags[refName] is { } tag)
        {
            return (tag.PeeledTarget ?? tag.Target) as Commit
                ?? repository.Lookup<Commit>((tag.PeeledTarget ?? tag.Target).Id);
        }

        return repository.Lookup<Commit>(refName);
    }

    // ─── Signature helpers ──────────────────────────────────────────────────────

    private static Signature BuildSignature(
        Signature original,
        string? name,
        string? email,
        DateTimeOffset? when)
    {
        return new Signature(
            name ?? original.Name,
            email ?? original.Email,
            when ?? original.When);
    }

    private static bool SignatureEquals(Signature a, Signature b) =>
        a.Name == b.Name && a.Email == b.Email && a.When == b.When;

    // ─── Tracking ───────────────────────────────────────────────────────────────

    private static CommitTracking CreateTracking(Commit commit)
    {
        var messageShort = commit.MessageShort
            ?? (commit.Message?.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty);

        return new CommitTracking
        {
            OriginalSha = commit.Sha,
            AuthorName = commit.Author.Name,
            AuthorEmail = commit.Author.Email,
            AuthorWhen = commit.Author.When,
            MessageShort = messageShort,
        };
    }

    /// <summary>
    /// Mutable tracking object for one commit during a rewrite. Collects modification
    /// flags set by the rewriter delegates before the final immutable DTO is built.
    /// </summary>
    private sealed class CommitTracking
    {
        public required string OriginalSha { get; init; }
        public required string AuthorName { get; init; }
        public required string AuthorEmail { get; init; }
        public required DateTimeOffset AuthorWhen { get; init; }
        public required string MessageShort { get; init; }
        public bool HeaderModified { get; set; }
        public bool MessageModified { get; set; }
        public bool TreeModified { get; set; }
        public bool ParentsModified { get; set; }

        public GitRewrittenCommitInfo Build() => new()
        {
            OriginalSha = OriginalSha,
            AuthorName = AuthorName,
            AuthorEmail = AuthorEmail,
            AuthorWhen = AuthorWhen,
            MessageShort = MessageShort,
            HeaderModified = HeaderModified,
            MessageModified = MessageModified,
            TreeModified = TreeModified,
            ParentsModified = ParentsModified,
        };
    }
}

/// <summary>
/// Passed to the caller's <c>TreeFilter</c> predicate for each blob entry encountered
/// during a tree walk. Exposes the full repository-relative path alongside the entry's
/// mode and object id so the PowerShell ScriptBlock can make an informed keep/remove decision.
/// </summary>
/// <remarks>
/// This type is defined in <c>PowerCode.Git.Core</c> (isolated ALC) and passed as
/// <see cref="object"/> across the ALC boundary. PowerShell's ETS accesses its properties
/// via reflection, so <c>$args[0].Path</c>, <c>$args[0].Mode</c>, and
/// <c>$args[0].ObjectId</c> work as expected inside a ScriptBlock.
/// </remarks>
public sealed class TreeEntryContext
{
    /// <summary>
    /// Initializes a new <see cref="TreeEntryContext"/>.
    /// </summary>
    /// <param name="path">Full repository-relative path (e.g. <c>src/Program.cs</c>).</param>
    /// <param name="mode">Entry mode string (e.g. <c>NonExecutableFile</c>).</param>
    /// <param name="objectId">The blob's SHA hash.</param>
    public TreeEntryContext(string path, string mode, string objectId)
    {
        Path = path;
        Mode = mode;
        ObjectId = objectId;
    }

    /// <summary>Gets the full repository-relative path of this tree entry.</summary>
    public string Path { get; }

    /// <summary>Gets the git mode string for this entry (e.g. <c>"NonExecutableFile"</c>).</summary>
    public string Mode { get; }

    /// <summary>Gets the SHA hash of the underlying blob object.</summary>
    public string ObjectId { get; }

    /// <inheritdoc/>
    public override string ToString() => Path;
}
