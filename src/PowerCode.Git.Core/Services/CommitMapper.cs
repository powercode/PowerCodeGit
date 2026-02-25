using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Provides shared helpers for mapping LibGit2Sharp commit objects to
/// <see cref="GitCommitInfo"/> DTOs and building ref-decoration lookup maps.
/// </summary>
internal static class CommitMapper
{
    /// <summary>
    /// Builds a lookup from commit SHA to the list of ref-name decorations
    /// (HEAD, local branches, remote branches, tags) pointing at that commit.
    /// </summary>
    internal static Dictionary<string, List<GitDecoration>> BuildDecorationMap(Repository repository)
    {
        var map = new Dictionary<string, List<GitDecoration>>(StringComparer.OrdinalIgnoreCase);

        // HEAD pointer
        if (repository.Head?.Tip is not null)
        {
            var headSha = repository.Head.Tip.Sha;
            GetOrCreate(map, headSha).Add(new GitDecoration("HEAD", GitDecorationType.Head));
        }

        // Branches (local and remote)
        foreach (var branch in repository.Branches)
        {
            if (branch.Tip is null)
            {
                continue;
            }

            var type = branch.IsRemote ? GitDecorationType.RemoteBranch : GitDecorationType.LocalBranch;
            GetOrCreate(map, branch.Tip.Sha).Add(new GitDecoration(branch.FriendlyName, type));
        }

        // Tags
        foreach (var tag in repository.Tags)
        {
            var targetSha = (tag.PeeledTarget ?? tag.Target).Sha;
            GetOrCreate(map, targetSha).Add(new GitDecoration($"tag: {tag.FriendlyName}", GitDecorationType.Tag));
        }

        return map;
    }

    /// <summary>
    /// Maps a single LibGit2Sharp <see cref="Commit"/> to a <see cref="GitCommitInfo"/> DTO.
    /// </summary>
    /// <param name="commit">The commit to map.</param>
    /// <param name="decorationMap">
    /// Optional decoration lookup built by <see cref="BuildDecorationMap"/>. When
    /// <see langword="null"/>, the returned <see cref="GitCommitInfo"/> has an empty
    /// decorations list.
    /// </param>
    internal static GitCommitInfo MapCommit(
        Commit commit,
        Dictionary<string, List<GitDecoration>>? decorationMap = null)
    {
        var parentShas = commit.Parents.Select(parent => parent.Sha).ToList();
        List<GitDecoration>? decorations = null;
        decorationMap?.TryGetValue(commit.Sha, out decorations);

        return new GitCommitInfo(
            commit.Sha,
            commit.Author.Name,
            commit.Author.Email,
            commit.Author.When,
            commit.Committer.Name,
            commit.Committer.Email,
            commit.Committer.When,
            commit.MessageShort,
            commit.Message,
            parentShas,
            decorations);
    }

    /// <summary>
    /// Returns <see langword="true"/> if the given commit's diff against its first
    /// parent touches at least one of the specified repository-relative paths.
    /// </summary>
    internal static bool CommitTouchesAnyPath(Repository repository, Commit commit, string[] paths)
    {
        var parentTree = commit.Parents.FirstOrDefault()?.Tree;

        using var changes = repository.Diff.Compare<TreeChanges>(parentTree, commit.Tree);

        return changes.Any(change =>
            paths.Any(p =>
                string.Equals(change.Path, p, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(change.OldPath, p, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<GitDecoration> GetOrCreate(Dictionary<string, List<GitDecoration>> map, string sha)
    {
        if (!map.TryGetValue(sha, out var list))
        {
            list = [];
            map[sha] = list;
        }

        return list;
    }
}
