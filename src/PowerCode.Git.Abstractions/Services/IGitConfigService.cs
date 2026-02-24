using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Defines operations for reading and writing git configuration values.
/// </summary>
public interface IGitConfigService
{
    /// <summary>
    /// Lists configuration entries matching the supplied options.
    /// Equivalent to <c>git config --list</c> with optional scope filtering.
    /// </summary>
    /// <param name="options">The query options.</param>
    /// <returns>A list of configuration entries.</returns>
    IReadOnlyList<GitConfigEntry> GetConfigEntries(GitConfigGetOptions options);

    /// <summary>
    /// Lists all configuration entries visible from the repository at
    /// <paramref name="repositoryPath"/>.
    /// </summary>
    /// <param name="repositoryPath">The path to the git repository.</param>
    /// <returns>A list of configuration entries.</returns>
    IReadOnlyList<GitConfigEntry> GetConfigEntries(string repositoryPath)
        => GetConfigEntries(new GitConfigGetOptions { RepositoryPath = repositoryPath });

    /// <summary>
    /// Gets the value of a single configuration key.
    /// Returns <see langword="null"/> when the key is not set.
    /// </summary>
    /// <param name="options">
    /// The query options. <see cref="GitConfigGetOptions.Name"/> must be set.
    /// </param>
    /// <returns>The matching entry, or <see langword="null"/> if not found.</returns>
    GitConfigEntry? GetConfigValue(GitConfigGetOptions options);

    /// <summary>
    /// Sets a configuration value. Equivalent to <c>git config &lt;name&gt; &lt;value&gt;</c>.
    /// </summary>
    /// <param name="options">The set options specifying key, value, and optional scope.</param>
    void SetConfigValue(GitConfigSetOptions options);

    /// <summary>
    /// Removes a configuration key. Equivalent to <c>git config --unset &lt;name&gt;</c>.
    /// </summary>
    /// <param name="options">The unset options specifying the key and optional scope.</param>
    void UnsetConfigValue(GitConfigUnsetOptions options);
}
