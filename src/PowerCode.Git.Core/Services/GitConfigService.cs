using System;
using System.Collections.Generic;
using System.Linq;
using LibGit2Sharp;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Reads and writes git configuration values via LibGit2Sharp's <see cref="Configuration"/>
/// API (<c>repository.Config</c>), eliminating the overhead of spawning a <c>git</c> process.
/// </summary>
public sealed class GitConfigService : IGitConfigService
{
    /// <inheritdoc/>
    public IReadOnlyList<GitConfigEntry> GetConfigEntries(GitConfigGetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));

        using var repo = new Repository(options.RepositoryPath);

        // LibGit2Sharp enumerates all config entries across every level (local, global,
        // xdg, system, programdata). Filter to the requested level when a scope is given,
        // and only propagate the Scope field when the caller opts in via ShowScope.
        var configLevel = options.Scope.HasValue ? MapScope(options.Scope.Value) : (ConfigurationLevel?)null;

        return repo.Config
            .Where(e => configLevel is null || e.Level == configLevel.Value)
            .Select(MapEntry)
            .ToList();
    }

    /// <inheritdoc/>
    public GitConfigEntry? GetConfigValue(GitConfigGetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new ArgumentException("Name is required when getting a single config value.", nameof(options));
        }

        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));

        using var repo = new Repository(options.RepositoryPath);

        // Get<T>(key, level) returns null when the level's config file does not exist
        // or the key is absent at that level — no exception handling needed.
        ConfigurationEntry<string>? entry = options.Scope.HasValue
            ? repo.Config.Get<string>(options.Name, MapScope(options.Scope.Value))
            : repo.Config.Get<string>(options.Name);

        return entry is null ? null : MapEntry(entry);
    }

    /// <inheritdoc/>
    public void SetConfigValue(GitConfigSetOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.Name))
        {
            throw new ArgumentException("Name is required.", nameof(options));
        }

        RepositoryGuard.ValidateRepositoryPath(options.RepositoryPath, nameof(options));

        using var repo = new Repository(options.RepositoryPath);

        if (options.Scope.HasValue)
        {
            repo.Config.Set(options.Name, options.Value, MapScope(options.Scope.Value));
        }
        else
        {
            // Defaults to ConfigurationLevel.Local — consistent with git CLI behaviour.
            repo.Config.Set(options.Name, options.Value);
        }
    }

    /// <summary>
    /// Maps a <see cref="GitConfigScope"/> to the equivalent LibGit2Sharp <see cref="ConfigurationLevel"/>.
    /// </summary>
    private static ConfigurationLevel MapScope(GitConfigScope scope) =>
        scope switch
        {
            GitConfigScope.Local => ConfigurationLevel.Local,
            GitConfigScope.Global => ConfigurationLevel.Global,
            GitConfigScope.System => ConfigurationLevel.System,
            GitConfigScope.Worktree => ConfigurationLevel.Worktree,
            _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown config scope."),
        };

    /// <summary>
    /// Maps a LibGit2Sharp <see cref="ConfigurationLevel"/> back to a <see cref="GitConfigScope"/>.
    /// Returns <see langword="null"/> for levels with no <see cref="GitConfigScope"/> equivalent
    /// (e.g. <see cref="ConfigurationLevel.Xdg"/>, <see cref="ConfigurationLevel.ProgramData"/>).
    /// </summary>
    private static GitConfigScope? MapLevel(ConfigurationLevel level) =>
        level switch
        {
            ConfigurationLevel.Local => GitConfigScope.Local,
            ConfigurationLevel.Global => GitConfigScope.Global,
            ConfigurationLevel.System => GitConfigScope.System,
            ConfigurationLevel.Worktree => GitConfigScope.Worktree,
            _ => null,
        };

    /// <summary>
    /// Converts a LibGit2Sharp <see cref="ConfigurationEntry{T}"/> to a <see cref="GitConfigEntry"/>.
    /// </summary>
    /// <param name="entry">The source entry.</param>        
    private static GitConfigEntry MapEntry(ConfigurationEntry<string> entry) =>
        new()
        {
            Name = entry.Key,
            Value = entry.Value,
            Scope = MapLevel(entry.Level),
        };
}
