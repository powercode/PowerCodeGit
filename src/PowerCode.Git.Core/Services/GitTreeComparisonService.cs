using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Compares two tree-ish references by diffing their commit trees using
/// LibGit2Sharp inside the isolated assembly load context.
/// </summary>
public sealed class GitTreeComparisonService : IGitTreeComparisonService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitDiffEntry> Compare(GitTreeCompareOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        if (string.IsNullOrWhiteSpace(options.Base))
        {
            throw new ArgumentException("Base tree-ish reference is required.", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.Compare))
        {
            throw new ArgumentException("Compare tree-ish reference is required.", nameof(options));
        }

        using var repository = new Repository(options.RepositoryPath);

        var baseCommit = ResolveCommit(repository, options.Base, nameof(options));
        var compareCommit = ResolveCommit(repository, options.Compare, nameof(options));

        // Note: LibGit2Sharp CompareOptions does not expose an IgnoreWhitespace property.
        // The IgnoreWhitespace option is accepted in GitTreeCompareOptions for future support
        // and for use by callers that process the result set further.
        var compareOptions = new CompareOptions { Similarity = SimilarityOptions.Renames };

        using var changes = repository.Diff.Compare<Patch>(baseCommit.Tree, compareCommit.Tree, compareOptions);

        var entries = changes.AsEnumerable();

        if (options.Paths is { Length: > 0 })
        {
            var paths = options.Paths;
            entries = entries.Where(change =>
                paths.Any(p =>
                    string.Equals(change.Path, p, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(change.OldPath, p, StringComparison.OrdinalIgnoreCase)));
        }

        return entries.Select(MapDiffEntry).ToList();
    }

    /// <summary>
    /// Resolves a committish string (branch name, tag, SHA) to a <see cref="Commit"/>.
    /// </summary>
    private static Commit ResolveCommit(Repository repository, string committish, string paramName)
    {
        // Try direct lookup first (SHA or ref).
        var commit = repository.Lookup<Commit>(committish);

        if (commit is not null)
        {
            return commit;
        }

        // Try as a branch name.
        var branch = repository.Branches[committish];

        if (branch?.Tip is not null)
        {
            return branch.Tip;
        }

        // Try as a tag name.
        var tag = repository.Tags[committish];

        if (tag is not null)
        {
            var target = tag.PeeledTarget ?? tag.Target;
            commit = target as Commit ?? repository.Lookup<Commit>(target.Id);

            if (commit is not null)
            {
                return commit;
            }
        }

        throw new ArgumentException(
            $"The ref '{committish}' was not found or does not point to a commit.",
            paramName);
    }

    private static GitDiffEntry MapDiffEntry(PatchEntryChanges change)
    {
        var status = change.Status switch
        {
            ChangeKind.Added => GitFileStatus.Added,
            ChangeKind.Deleted => GitFileStatus.Deleted,
            ChangeKind.Modified => GitFileStatus.Modified,
            ChangeKind.Renamed => GitFileStatus.Renamed,
            ChangeKind.Conflicted => GitFileStatus.Conflicted,
            _ => GitFileStatus.Modified,
        };

        return new GitDiffEntry(
            change.OldPath,
            change.Path,
            status,
            change.LinesAdded,
            change.LinesDeleted,
            change.Patch);
    }
}
