using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Lists and creates git tags using LibGit2Sharp.
/// </summary>
public sealed class GitTagService : IGitTagService
{
    /// <summary>
    /// Gets all tags in the repository. Forwards to the options-based overload.
    /// </summary>
    public IReadOnlyList<GitTagInfo> GetTags(string repositoryPath)
        => GetTags(new GitTagListOptions { RepositoryPath = repositoryPath });

    /// <inheritdoc/>
    public IReadOnlyList<GitTagInfo> GetTags(GitTagListOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        IEnumerable<Tag> tags = repository.Tags;

        if (options.Pattern is not null)
        {
            var regexPattern = "^" + Regex.Escape(options.Pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
            tags = tags.Where(t => regex.IsMatch(t.FriendlyName));
        }

        if (options.Exclude is not null)
        {
            var excludePattern = "^" + Regex.Escape(options.Exclude).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            var excludeRegex = new Regex(excludePattern, RegexOptions.IgnoreCase);
            tags = tags.Where(t => !excludeRegex.IsMatch(t.FriendlyName));
        }

        if (options.ContainsCommit is not null)
        {
            var targetCommit = repository.Lookup<Commit>(options.ContainsCommit);
            if (targetCommit is not null)
            {
                tags = tags.Where(t =>
                {
                    var peeled = t.PeeledTarget as Commit ?? t.Target as Commit;
                    if (peeled is null) return false;

                    if (peeled.Sha == targetCommit.Sha) return true;
                    var mergeBase = repository.ObjectDatabase.FindMergeBase(peeled, targetCommit);
                    return mergeBase?.Sha == targetCommit.Sha;
                });
            }
        }

        var result = tags.Select(MapTag).ToList();

        if (options.SortBy is not null)
        {
            var sortKey = options.SortBy.ToLowerInvariant();
            if (sortKey is "version" or "v:refname")
            {
                result = result
                    .OrderBy(t => ParseVersion(t.Name), new VersionComparer())
                    .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                result = result
                    .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public GitTagInfo CreateTag(GitTagCreateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Tag Name is required.");

        using var repository = new Repository(options.RepositoryPath);

        // Resolve the target object. Defaults to the current HEAD commit when not specified.
        GitObject target;
        if (options.Target is not null)
        {
            target = repository.Lookup<GitObject>(options.Target)
                ?? throw new ArgumentException(
                    $"Target '{options.Target}' was not found in the repository.", nameof(options));
        }
        else
        {
            target = repository.Head.Tip
                ?? throw new InvalidOperationException(
                    "The repository has no commits. Cannot create a tag without a target.");
        }

        Tag tag;
        if (!string.IsNullOrWhiteSpace(options.Message))
        {
            // Annotated tag: capture the tagger identity from the repository configuration.
            var tagger = repository.Config.BuildSignature(DateTimeOffset.Now);
            tag = repository.Tags.Add(options.Name, target, tagger, options.Message, allowOverwrite: options.Force);
        }
        else
        {
            // Lightweight tag: points directly to the target object.
            tag = repository.Tags.Add(options.Name, target, allowOverwrite: options.Force);
        }

        return MapTag(tag);
    }

    /// <inheritdoc/>
    public void DeleteTag(GitTagDeleteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Tag name (options.Name) is required.");

        using var repository = new Repository(options.RepositoryPath);

        var tag = repository.Tags[options.Name]
            ?? throw new ArgumentException($"The tag '{options.Name}' does not exist.", nameof(options));

        repository.Tags.Remove(tag);
    }

    private static string? ParseVersion(string tagName)
    {
        // Strip common prefixes like v, V
        var raw = tagName.TrimStart('v', 'V');
        return raw;
    }

    private sealed class VersionComparer : System.Collections.Generic.IComparer<string?>
    {
        public int Compare(string? x, string? y)
        {
            if (x is null && y is null) return 0;
            if (x is null) return -1;
            if (y is null) return 1;

            var xParts = x.Split('.').Select(p => int.TryParse(p, out var n) ? n : -1).ToArray();
            var yParts = y.Split('.').Select(p => int.TryParse(p, out var n) ? n : -1).ToArray();
            var len = Math.Max(xParts.Length, yParts.Length);

            for (var i = 0; i < len; i++)
            {
                var xVal = i < xParts.Length ? xParts[i] : 0;
                var yVal = i < yParts.Length ? yParts[i] : 0;
                var cmp = xVal.CompareTo(yVal);
                if (cmp != 0) return cmp;
            }

            return 0;
        }
    }

    private static GitTagInfo MapTag(Tag tag)
    {
        var targetSha = (tag.PeeledTarget ?? tag.Target).Sha;

        if (tag.IsAnnotated)
        {
            var annotation = tag.Annotation;
            return new GitTagInfo(
                tag.FriendlyName,
                targetSha,
                isAnnotated: true,
                annotation.Tagger?.Name,
                annotation.Tagger?.Email,
                annotation.Tagger?.When,
                annotation.Message?.TrimEnd());
        }

        return new GitTagInfo(
            tag.FriendlyName,
            targetSha,
            isAnnotated: false,
            taggerName: null,
            taggerEmail: null,
            tagDate: null,
            message: null);
    }
}
