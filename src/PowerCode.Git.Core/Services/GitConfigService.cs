using System;
using System.Collections.Generic;
using System.Linq;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Reads and writes git configuration values via the <c>git config</c> CLI.
/// </summary>
public sealed class GitConfigService : IGitConfigService
{
    private readonly IGitExecutable gitExecutable;

    /// <summary>
    /// Initializes a new instance using the default <see cref="GitExecutable"/>.
    /// </summary>
    public GitConfigService() : this(new GitExecutable()) { }

    /// <summary>
    /// Initializes a new instance with the specified <see cref="IGitExecutable"/>
    /// for testability.
    /// </summary>
    /// <param name="gitExecutable">The git process runner to use.</param>
    internal GitConfigService(IGitExecutable gitExecutable)
    {
        this.gitExecutable = gitExecutable;
    }

    /// <inheritdoc/>
    public IReadOnlyList<GitConfigEntry> GetConfigEntries(GitConfigGetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var args = new List<string> { "config", "--list" };

        if (options.ShowScope)
        {
            args.Add("--show-scope");
        }

        AppendScopeFlag(args, options.Scope);

        var result = gitExecutable.RunWithResult(options.RepositoryPath, args);

        if (!result.IsSuccess)
        {
            return [];
        }

        return ParseEntries(result.StdOut, options.ShowScope);
    }

    /// <inheritdoc/>
    public GitConfigEntry? GetConfigValue(GitConfigGetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new ArgumentException("Name is required when getting a single config value.", nameof(options));
        }

        var args = new List<string> { "config" };

        if (options.ShowScope)
        {
            args.Add("--show-scope");
        }

        AppendScopeFlag(args, options.Scope);
        args.Add("--get");
        args.Add(options.Name);

        var result = gitExecutable.RunWithResult(options.RepositoryPath, args);

        if (!result.IsSuccess)
        {
            return null;
        }

        var value = result.StdOut.TrimEnd('\n', '\r');

        if (options.ShowScope)
        {
            var entries = ParseEntries($"{value}\n", showScope: true);
            return entries.Count > 0 ? entries[0] : null;
        }

        return new GitConfigEntry
        {
            Name = options.Name,
            Value = value,
            Scope = options.Scope,
        };
    }

    /// <inheritdoc/>
    public void SetConfigValue(GitConfigSetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new ArgumentException("Name is required.", nameof(options));
        }

        var args = new List<string> { "config" };
        AppendScopeFlag(args, options.Scope);
        args.Add(options.Name);
        args.Add(options.Value);

        gitExecutable.Run(options.RepositoryPath, args);
    }

    private static void AppendScopeFlag(List<string> args, GitConfigScope? scope)
    {
        if (scope.HasValue)
        {
            args.Add(scope.Value switch
            {
                GitConfigScope.Local => "--local",
                GitConfigScope.Global => "--global",
                GitConfigScope.System => "--system",
                GitConfigScope.Worktree => "--worktree",
                _ => throw new ArgumentOutOfRangeException(nameof(scope), scope.Value, "Unknown config scope."),
            });
        }
    }

    private static IReadOnlyList<GitConfigEntry> ParseEntries(string output, bool showScope)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return lines
            .Select(line => ParseLine(line.TrimEnd('\r'), showScope))
            .Where(e => e is not null)
            .ToList()!;
    }

    private static GitConfigEntry? ParseLine(string line, bool showScope)
    {
        GitConfigScope? scope = null;
        var remainder = line;

        if (showScope)
        {
            // Format: "scope\tkey=value" (e.g., "local\tuser.name=John")
            var tabIndex = line.IndexOf('\t');
            if (tabIndex < 0)
            {
                return null;
            }

            scope = line[..tabIndex] switch
            {
                "local" => GitConfigScope.Local,
                "global" => GitConfigScope.Global,
                "system" => GitConfigScope.System,
                "worktree" => GitConfigScope.Worktree,
                _ => null,
            };

            remainder = line[(tabIndex + 1)..];
        }

        // Format: "key=value"
        var eqIndex = remainder.IndexOf('=');
        if (eqIndex < 0)
        {
            return new GitConfigEntry { Name = remainder, Value = null, Scope = scope };
        }

        return new GitConfigEntry
        {
            Name = remainder[..eqIndex],
            Value = remainder[(eqIndex + 1)..],
            Scope = scope,
        };
    }
}
