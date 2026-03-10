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
    public IReadOnlyList<GitRemoteInfo> GetRemotes(GitRemoteListOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        if (options.Name is not null)
        {
            var remote = repository.Network.Remotes[options.Name];
            return remote is null
                ? []
                : [new GitRemoteInfo(remote.Name, remote.Url, remote.PushUrl ?? remote.Url)];
        }

        return repository.Network.Remotes
            .Select(remote => new GitRemoteInfo(remote.Name, remote.Url, remote.PushUrl ?? remote.Url))
            .ToList();
    }

    /// <inheritdoc/>
    public GitRemoteInfo AddRemote(GitRemoteAddOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Remote name is required.");
        RepositoryGuard.ValidateRequiredString(options.Url, nameof(options), "Remote URL is required.");

        using var repository = new Repository(options.RepositoryPath);

        // Add the remote with its fetch URL
        repository.Network.Remotes.Add(options.Name, options.Url);

        // Set a separate push URL if specified
        if (options.PushUrl is not null)
        {
            repository.Network.Remotes.Update(options.Name, r => r.PushUrl = options.PushUrl);
        }

        var created = repository.Network.Remotes[options.Name]!;
        return new GitRemoteInfo(created.Name, created.Url, created.PushUrl ?? created.Url);
    }

    /// <inheritdoc/>
    public void RemoveRemote(GitRemoteRemoveOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Remote name is required.");

        using var repository = new Repository(options.RepositoryPath);

        var existing = repository.Network.Remotes[options.Name];
        if (existing is null)
        {
            throw new ArgumentException($"Remote '{options.Name}' does not exist.", nameof(options));
        }

        repository.Network.Remotes.Remove(options.Name);
    }

    /// <inheritdoc/>
    public GitRemoteInfo RenameRemote(string repositoryPath, string oldName, string newName)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));
        RepositoryGuard.ValidateRequiredString(oldName, nameof(oldName), "Old remote name is required.");
        RepositoryGuard.ValidateRequiredString(newName, nameof(newName), "New remote name is required.");

        using var repository = new Repository(repositoryPath);

        var existing = repository.Network.Remotes[oldName];
        if (existing is null)
        {
            throw new ArgumentException($"Remote '{oldName}' does not exist.", nameof(oldName));
        }

        // Normalize Windows-style backslash paths to forward slashes before renaming.
        // git.exe on Windows stores local paths with unescaped backslashes in .git/config,
        // which is valid for git.exe but triggers "invalid escape" errors in libgit2's
        // stricter config parser during the rename operation. Remotes.Update() does not
        // share this problem and is used here as a pre-pass to sanitize the stored URL.
        bool urlHasBackslash = existing.Url?.Contains('\\') ?? false;
        bool pushUrlHasBackslash = existing.PushUrl?.Contains('\\') ?? false;
        if (urlHasBackslash || pushUrlHasBackslash)
        {
            repository.Network.Remotes.Update(oldName, updater =>
            {
                if (urlHasBackslash) updater.Url = existing.Url!.Replace('\\', '/');
                if (pushUrlHasBackslash) updater.PushUrl = existing.PushUrl!.Replace('\\', '/');
            });
        }

        // Ignore rename problems (stale non-default refspecs) — git remote rename warns about these
        // but does not fail. We follow the same behaviour.
        repository.Network.Remotes.Rename(oldName, newName, _ => { });

        var renamed = repository.Network.Remotes[newName]!;
        return new GitRemoteInfo(renamed.Name, renamed.Url, renamed.PushUrl ?? renamed.Url);
    }

    /// <inheritdoc/>
    public GitRemoteInfo UpdateRemoteUrl(GitRemoteUpdateOptions options)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Name, nameof(options), "Remote name is required.");

        if (options.Url is null && options.PushUrl is null)
        {
            throw new ArgumentException(
                "At least one of Url or PushUrl must be specified.", nameof(options));
        }

        using var repository = new Repository(options.RepositoryPath);

        var existing = repository.Network.Remotes[options.Name];
        if (existing is null)
        {
            throw new ArgumentException($"Remote '{options.Name}' does not exist.", nameof(options));
        }

        repository.Network.Remotes.Update(options.Name, r =>
        {
            if (options.Url is not null)
            {
                r.Url = options.Url;
            }

            if (options.PushUrl is not null)
            {
                r.PushUrl = options.PushUrl;
            }
        });

        var updated = repository.Network.Remotes[options.Name]!;
        return new GitRemoteInfo(updated.Name, updated.Url, updated.PushUrl ?? updated.Url);
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
                branch.TrackingDetails?.BehindBy,
                worktreePath: null);
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
                behindBy: null,
                worktreePath: null);
        }

        if (options.All)
        {
            // Collect all local branch refspecs and push in a single network call,
            // matching `git push --all` behaviour (one round-trip, one credential prompt).
            var branchRefSpecs = repository.Branches
                .Where(b => !b.IsRemote)
                .Select(b => options.Force
                    ? $"+{b.CanonicalName}:{b.CanonicalName}"
                    : b.CanonicalName)
                .ToList();

            if (branchRefSpecs.Count > 0)
            {
                repository.Network.Push(remote, branchRefSpecs, pushOptions);
            }
        }
        else if (options.Tags)
        {
            // Collect all tag refspecs and push in a single network call,
            // matching `git push --tags` behaviour (one round-trip, one credential prompt).
            // Previously this looped per-tag, triggering a separate credential callback
            // for each tag and hanging when LibGit2Sharp retried on auth failure.
            var tagRefSpecs = repository.Tags
                .Select(t => t.CanonicalName)
                .ToList();

            if (tagRefSpecs.Count > 0)
            {
                repository.Network.Push(remote, tagRefSpecs, pushOptions);
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
            updatedBranch.TrackingDetails?.BehindBy,
            worktreePath: null);
    }

    /// <inheritdoc/>
    public void Fetch(GitFetchOptions options, Action<int, string>? onProgress = null)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        using var repository = new Repository(options.RepositoryPath);

        var fetchOptions = BuildFetchOptions(
            options.CredentialUsername, options.CredentialPassword,
            GetRemoteUrl(options.RepositoryPath, options.RemoteName),
            options.Prune, options.Tags, onProgress);

        var remote = repository.Network.Remotes[options.RemoteName]
            ?? throw new ArgumentException($"Remote '{options.RemoteName}' was not found.", nameof(options));

        var refSpecs = remote.FetchRefSpecs.Select(r => r.Specification);
        Commands.Fetch(repository, remote.Name, refSpecs, fetchOptions, null);
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
            FetchOptions = BuildFetchOptions(
                options.CredentialUsername, options.CredentialPassword,
                GetRemoteUrl(options.RepositoryPath, options.RemoteName),
                options.Prune, options.Tags, onProgress),
        };

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
    /// Builds a <see cref="FetchOptions"/> instance shared by both <see cref="Fetch"/> and <see cref="Pull"/>.
    /// </summary>
    private static FetchOptions BuildFetchOptions(
        string? credentialUsername,
        string? credentialPassword,
        string remoteUrl,
        bool prune,
        bool? tags,
        Action<int, string>? onProgress)
    {
        var fetchOptions = new FetchOptions
        {
            Prune = prune,
            CredentialsProvider = CreateCredentialsProvider(credentialUsername, credentialPassword, remoteUrl),
        };

        if (tags.HasValue)
        {
            fetchOptions.TagFetchMode = tags.Value ? TagFetchMode.All : TagFetchMode.None;
        }

        if (onProgress is not null)
        {
            fetchOptions.OnTransferProgress = progress =>
            {
                if (progress.TotalObjects > 0)
                {
                    var percent = (int)((double)progress.ReceivedObjects / progress.TotalObjects * 100);
                    onProgress(percent, $"Receiving objects: {progress.ReceivedObjects}/{progress.TotalObjects}");
                }

                return true;
            };
        }

        return fetchOptions;
    }

    /// <summary>
    /// Creates a credentials handler that uses explicit credentials if provided,
    /// or falls back to the system's Git credential helper via <c>git credential fill</c>.
    /// </summary>
    /// <remarks>
    /// LibGit2Sharp retries the credentials callback on every authentication failure, which
    /// can cause an infinite loop when no credentials are available. The <c>alreadyCalled</c>
    /// guard breaks this loop by returning <see cref="DefaultCredentials"/> on the second
    /// invocation, allowing LibGit2Sharp to surface an authentication error to the caller.
    /// </remarks>
    private static LibGit2Sharp.Handlers.CredentialsHandler CreateCredentialsProvider(
        string? credentialUsername, string? credentialPassword, string remoteUrl)
    {
        // Captured in the closure — one flag per push/fetch operation.
        var alreadyCalled = false;

        return (_, _, supportedTypes) =>
        {
            // On the second invocation LibGit2Sharp is retrying after an auth failure.
            // Return DefaultCredentials to abort the retry loop and propagate the error.
            if (alreadyCalled)
            {
                return new DefaultCredentials();
            }

            alreadyCalled = true;

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
