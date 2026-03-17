namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Exposes process-wide libgit2 settings that are not scoped to a single repository.
/// </summary>
public interface IGitGlobalSettingsService
{
    /// <summary>
    /// Enables or disables the libgit2 repository-ownership validation.
    /// When disabled, the "repository not owned by current user" error
    /// (introduced in Git 2.35.2 / CVE-2022-24765) is suppressed for
    /// the lifetime of the process.
    /// </summary>
    /// <param name="enabled">
    /// <c>true</c> to enforce ownership validation (default);
    /// <c>false</c> to suppress it.
    /// </param>
    void SetOwnerValidation(bool enabled);
}
