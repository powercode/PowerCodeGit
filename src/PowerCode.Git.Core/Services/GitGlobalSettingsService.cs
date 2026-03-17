using LibGit2Sharp;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Exposes process-wide libgit2 global settings via LibGit2Sharp.
/// </summary>
public sealed class GitGlobalSettingsService : IGitGlobalSettingsService
{
    /// <inheritdoc/>
    public void SetOwnerValidation(bool enabled)
    {
        GlobalSettings.SetOwnerValidation(enabled);
    }
}
