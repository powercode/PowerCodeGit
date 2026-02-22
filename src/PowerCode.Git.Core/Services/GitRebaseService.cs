using System;
using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Implements git rebase operations by invoking the <c>git</c> executable.
/// LibGit2Sharp does not expose a full rebase API, so all operations
/// are delegated to the CLI.
/// </summary>
public sealed class GitRebaseService : IGitRebaseService
{
    private readonly IGitExecutable gitExecutable;

    /// <summary>
    /// Initializes a new instance using the default <see cref="GitExecutable"/>.
    /// </summary>
    public GitRebaseService() : this(new GitExecutable()) { }

    /// <summary>
    /// Initializes a new instance with the specified <see cref="IGitExecutable"/>
    /// for testability.
    /// </summary>
    /// <param name="gitExecutable">The git process runner to use.</param>
    internal GitRebaseService(IGitExecutable gitExecutable)
    {
        this.gitExecutable = gitExecutable ?? throw new ArgumentNullException(nameof(gitExecutable));
    }

    /// <inheritdoc/>
    public GitRebaseResult Start(GitRebaseOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));
        RepositoryGuard.ValidateRequiredString(options.Upstream, nameof(options.Upstream));

        if (options.Interactive)
        {
            return StartInteractive(options);
        }

        var args = BuildStartArgs(options);
        var result = gitExecutable.RunWithResult(options.RepositoryPath, args);

        return InterpretResult(result);
    }

    /// <inheritdoc/>
    public GitRebaseResult Continue(GitRebaseContinueOptions options)
    {
        RepositoryGuard.ValidateOptions(options, o => o.RepositoryPath, nameof(options));

        var action = options.Skip ? "--skip" : "--continue";
        var result = gitExecutable.RunWithResult(options.RepositoryPath, ["rebase", action]);

        return InterpretResult(result);
    }

    /// <inheritdoc/>
    public void Abort(string repositoryPath)
    {
        RepositoryGuard.ValidateRepositoryPath(repositoryPath, nameof(repositoryPath));

        // git rebase --abort should always succeed; use Run so any unexpected failure throws.
        gitExecutable.Run(repositoryPath, ["rebase", "--abort"]);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds the argument list for <c>git rebase</c> from <paramref name="options"/>.
    /// </summary>
    private static IReadOnlyList<string> BuildStartArgs(GitRebaseOptions options)
    {
        var args = new List<string> { "rebase" };

        if (options.AutoStash)
        {
            args.Add("--autostash");
        }

        if (options.Onto is not null)
        {
            args.Add("--onto");
            args.Add(options.Onto);
        }

        args.Add(options.Upstream);

        return args;
    }

    /// <summary>
    /// Runs an interactive rebase. Stdin/stdout/stderr are not redirected so the
    /// user's editor can open normally.
    /// </summary>
    private GitRebaseResult StartInteractive(GitRebaseOptions options)
    {
        var args = new List<string> { "rebase", "-i" };

        if (options.AutoStash)
        {
            args.Add("--autostash");
        }

        if (options.Onto is not null)
        {
            args.Add("--onto");
            args.Add(options.Onto);
        }

        args.Add(options.Upstream);

        var exitCode = gitExecutable.RunInteractive(options.RepositoryPath, args);

        return exitCode switch
        {
            0 => new GitRebaseResult(success: true, hasConflicts: false, output: string.Empty),
            1 => new GitRebaseResult(success: false, hasConflicts: true, output: string.Empty),
            _ => throw new InvalidOperationException(
                $"git rebase -i failed with exit code {exitCode}.")
        };
    }

    /// <summary>
    /// Converts a <see cref="GitProcessResult"/> into a <see cref="GitRebaseResult"/>.
    /// Exit code 0 = success, exit code 1 = conflicts, anything else = fatal error.
    /// </summary>
    private static GitRebaseResult InterpretResult(GitProcessResult result)
    {
        return result.ExitCode switch
        {
            0 => new GitRebaseResult(success: true, hasConflicts: false, output: result.StdOut),
            1 => new GitRebaseResult(success: false, hasConflicts: true, output: result.StdOut),
            _ => throw new InvalidOperationException(
                $"git rebase failed (exit code {result.ExitCode}): {result.StdErr.Trim()}")
        };
    }
}
