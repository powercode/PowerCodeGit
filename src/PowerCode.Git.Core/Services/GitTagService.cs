using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Lists git tags using LibGit2Sharp.
/// </summary>
public sealed class GitTagService : IGitTagService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitTagInfo> GetTags(string repositoryPath)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

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
