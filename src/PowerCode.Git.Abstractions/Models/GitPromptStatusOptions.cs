namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Controls the behaviour of the <c>Get-GitPromptStatus</c> cmdlet.
/// </summary>
public sealed class GitPromptStatusOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the upstream provider icon
    /// and ahead/behind counts should be omitted from the prompt string.
    /// </summary>
    public bool HideUpstream { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether staged, modified, and untracked
    /// file counts should be omitted from the prompt string.
    /// </summary>
    public bool HideCounts { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the stash count indicator
    /// should be omitted from the prompt string.
    /// </summary>
    public bool HideStash { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ANSI color escape sequences
    /// should be stripped from the prompt string.
    /// </summary>
    public bool NoColor { get; set; }
}
