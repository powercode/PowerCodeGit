namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Identifies the hosting provider of a git remote, based on the remote URL.
/// </summary>
public enum GitUpstreamProvider
{
    /// <summary>
    /// The provider could not be determined from the remote URL.
    /// </summary>
    Unknown,

    /// <summary>
    /// The remote is hosted on GitHub (<c>github.com</c>).
    /// </summary>
    GitHub,

    /// <summary>
    /// The remote is hosted on GitLab (<c>gitlab.com</c> or a self-hosted GitLab instance).
    /// </summary>
    GitLab,

    /// <summary>
    /// The remote is hosted on Bitbucket (<c>bitbucket.org</c> or a self-hosted Bitbucket instance).
    /// </summary>
    Bitbucket,

    /// <summary>
    /// The remote is hosted on Azure DevOps (<c>dev.azure.com</c> or a <c>visualstudio.com</c> URL).
    /// </summary>
    AzureDevOps,
}
