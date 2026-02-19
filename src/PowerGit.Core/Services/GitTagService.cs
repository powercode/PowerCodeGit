using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Core.Services;

/// <summary>
/// Lists git tags using LibGit2Sharp.
/// </summary>
public sealed class GitTagService : IGitTagService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitTagInfo> GetTags(string repositoryPath)
    {
        if (string.IsNullOrWhiteSpace(repositoryPath))
        {
            throw new ArgumentException("RepositoryPath is required.", nameof(repositoryPath));
        }

        if (!Repository.IsValid(repositoryPath))
        {
            throw new ArgumentException("RepositoryPath does not reference a valid git repository.", nameof(repositoryPath));
        }

        using var repository = new Repository(repositoryPath);

        return repository.Tags
            .Select(MapTag)
            .ToList();
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
