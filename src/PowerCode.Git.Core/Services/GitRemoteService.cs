using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Manages git remote operations including listing remotes, cloning,
/// pushing, and pulling using LibGit2Sharp.
/// </summary>
public sealed class GitRemoteService : IGitRemoteService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitRemoteInfo> GetRemotes(string repositoryPath)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

        using var repository = new Repository(repositoryPath);

        return repository.Network.Remotes
            .Select(remote => new GitRemoteInfo(remote.Name, remote.Url, remote.PushUrl ?? remote.Url))
            .ToList();
    }

    /// <inheritdoc/>
    public string Clone(GitCloneOptions options, Action<int, string>? onProgress = null)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Url, nameof(options), "Clone URL is required.");

        var localPath = options.LocalPath ?? DeriveLocalPath(options.Url);

        var cloneOptions = new CloneOptions
        {
            IsBare = options.Bare,
        };

        if (options.BranchName is not null)
        {
            cloneOptions.BranchName = options.BranchName;
        }

        if (options.RecurseSubmodules)
        {
            cloneOptions.RecurseSubmodules = true;
        }

        cloneOptions.FetchOptions.CredentialsProvider = CreateCredentialsProvider(
            options.CredentialUsername, options.CredentialPassword, options.Url);

        if (onProgress is not null)
        {
            cloneOptions.FetchOptions.OnTransferProgress = progress =>
            {
                if (progress.TotalObjects > 0)
                {
                    var percent = (int)((double)progress.ReceivedObjects / progress.TotalObjects * 100);
                    onProgress(percent, $"Receiving objects: {progress.ReceivedObjects}/{progress.TotalObjects}");
                }

                return true;
            };
        }

        var resultPath = Repository.Clone(options.Url, localPath, cloneOptions);
        return Path.GetFullPath(resultPath);
    }

    /// <inheritdoc/>
    public GitBranchInfo Push(GitPushOptions options, Action<int, string>? onProgress = null)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var branch = options.BranchName is not null
            ? repository.Branches[options.BranchName]
                ?? throw new ArgumentException($"Branch '{options.BranchName}' does not exist.", nameof(options))
            : repository.Head;

        var remote = repository.Network.Remotes[options.RemoteName]
            ?? throw new ArgumentException($"Remote '{options.RemoteName}' does not exist.", nameof(options));

        var pushOptions = new LibGit2Sharp.PushOptions();

        pushOptions.CredentialsProvider = CreateCredentialsProvider(
            options.CredentialUsername, options.CredentialPassword, remote.Url);

        if (onProgress is not null)
        {
            pushOptions.OnPushTransferProgress = (current, total, bytes) =>
            {
                var percent = total > 0 ? (int)((double)current / total * 100) : 0;
                onProgress(percent, $"Pushing objects: {current}/{total}");
                return true;
            };
        }

        if (options.DryRun)
        {
            // Dry run is not natively supported by LibGit2Sharp; skip actual push
            return new GitBranchInfo(
                branch.FriendlyName,
                branch.IsCurrentRepositoryHead,
                branch.IsRemote,
                branch.Tip?.Sha ?? string.Empty,
                branch.TrackedBranch?.FriendlyName,
                branch.TrackingDetails?.AheadBy,
                branch.TrackingDetails?.BehindBy);
        }

        if (options.Delete)
        {
            var branchName = branch.FriendlyName;
            repository.Network.Push(remote, $":refs/heads/{branchName}", pushOptions);

            return new GitBranchInfo(
                branchName,
                isHead: false,
                isRemote: false,
                tipSha: branch.Tip?.Sha ?? string.Empty,
                trackedBranchName: null,
                aheadBy: null,
                behindBy: null);
        }

        if (options.All)
        {
            foreach (var localBranch in repository.Branches.Where(b => !b.IsRemote))
            {
                var refSpec = options.Force
                    ? $"+{localBranch.CanonicalName}:{localBranch.CanonicalName}"
                    : localBranch.CanonicalName;
                repository.Network.Push(remote, refSpec, pushOptions);
            }
        }
        else if (options.Tags)
        {
            foreach (var tag in repository.Tags)
            {
                repository.Network.Push(remote, tag.CanonicalName, pushOptions);
            }
        }
        else
        {
            var refSpec = options.Force || options.ForceWithLease
                ? $"+{branch.CanonicalName}:{branch.CanonicalName}"
                : branch.CanonicalName;
            repository.Network.Push(remote, refSpec, pushOptions);
        }

        if (options.SetUpstream)
        {
            repository.Branches.Update(
                branch,
                b => b.TrackedBranch = $"refs/remotes/{options.RemoteName}/{branch.FriendlyName}");
        }

        // Re-read the branch to get updated tracking info
        var updatedBranch = repository.Branches[branch.FriendlyName]!;
        return new GitBranchInfo(
            updatedBranch.FriendlyName,
            updatedBranch.IsCurrentRepositoryHead,
            updatedBranch.IsRemote,
            updatedBranch.Tip?.Sha ?? string.Empty,
            updatedBranch.TrackedBranch?.FriendlyName,
            updatedBranch.TrackingDetails?.AheadBy,
            updatedBranch.TrackingDetails?.BehindBy);
    }

    /// <inheritdoc/>
    public GitCommitInfo Pull(GitPullOptions options, Action<int, string>? onProgress = null)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var config = repository.Config;
        var name = config.Get<string>("user.name")?.Value
            ?? throw new InvalidOperationException("Git user.name is not configured.");
        var email = config.Get<string>("user.email")?.Value
            ?? throw new InvalidOperationException("Git user.email is not configured.");
        var signature = new Signature(name, email, DateTimeOffset.Now);

        var pullOptions = new PullOptions
        {
            MergeOptions = new MergeOptions
            {
                FastForwardStrategy = options.MergeStrategy switch
                {
                    GitMergeStrategy.FastForward => FastForwardStrategy.FastForwardOnly,
                    GitMergeStrategy.Merge => FastForwardStrategy.Default,
                    _ => FastForwardStrategy.Default,
                },
            },
            FetchOptions = new FetchOptions(),
        };

        if (options.Prune)
        {
            pullOptions.FetchOptions.Prune = true;
        }

        if (options.Tags.HasValue)
        {
            pullOptions.FetchOptions.TagFetchMode = options.Tags.Value ? TagFetchMode.All : TagFetchMode.None;
        }

        pullOptions.FetchOptions.CredentialsProvider = CreateCredentialsProvider(
            options.CredentialUsername, options.CredentialPassword,
            GetRemoteUrl(options.RepositoryPath, options.RemoteName));

        if (onProgress is not null)
        {
            pullOptions.FetchOptions.OnTransferProgress = progress =>
            {
                if (progress.TotalObjects > 0)
                {
                    var percent = (int)((double)progress.ReceivedObjects / progress.TotalObjects * 100);
                    onProgress(percent, $"Receiving objects: {progress.ReceivedObjects}/{progress.TotalObjects}");
                }

                return true;
            };
        }

        // AutoStash: save local changes before pulling, reapply after
        Stash? autoStash = null;
        if (options.AutoStash && repository.RetrieveStatus().IsDirty)
        {
            autoStash = repository.Stashes.Add(signature, "AutoStash before pull");
        }

        var mergeResult = Commands.Pull(repository, signature, pullOptions);

        if (autoStash is not null)
        {
            repository.Stashes.Pop(0, new StashApplyOptions());
        }

        var headCommit = repository.Head.Tip;
        var parentShas = headCommit.Parents.Select(p => p.Sha).ToList();

        return new GitCommitInfo(
            headCommit.Sha,
            headCommit.Author.Name,
            headCommit.Author.Email,
            headCommit.Author.When,
            headCommit.Committer.Name,
            headCommit.Committer.Email,
            headCommit.Committer.When,
            headCommit.MessageShort,
            headCommit.Message,
            parentShas);
    }

    /// <summary>
    /// Creates a credentials handler that uses explicit credentials if provided,
    /// or falls back to the system's Git credential helper via <c>git credential fill</c>.
    /// </summary>
    private static LibGit2Sharp.Handlers.CredentialsHandler CreateCredentialsProvider(
        string? credentialUsername, string? credentialPassword, string remoteUrl)
    {
        return (_, _, supportedTypes) =>
        {
            if (!supportedTypes.HasFlag(SupportedCredentialTypes.UsernamePassword))
            {
                return new DefaultCredentials();
            }

            var username = credentialUsername;
            var password = credentialPassword;

            if (username is null)
            {
                (username, password) = GitCredentialHelper.GetCredentials(remoteUrl);
            }

            if (username is not null)
            {
                return new UsernamePasswordCredentials
                {
                    Username = username,
                    Password = password ?? string.Empty,
                };
            }

            return new DefaultCredentials();
        };
    }

    /// <summary>
    /// Resolves the URL of a remote from the repository configuration.
    /// </summary>
    private static string GetRemoteUrl(string repositoryPath, string remoteName)
    {
        using var repository = new Repository(repositoryPath);
        var remote = repository.Network.Remotes[remoteName];
        return remote?.Url ?? string.Empty;
    }

    private static string DeriveLocalPath(string url)
    {
        // Extract repository name from URL, stripping .git suffix
        var lastSegment = url.TrimEnd('/').Split('/')[^1];

        if (lastSegment.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
        {
            lastSegment = lastSegment[..^4];
        }

        return Path.GetFullPath(lastSegment);
    }
}
