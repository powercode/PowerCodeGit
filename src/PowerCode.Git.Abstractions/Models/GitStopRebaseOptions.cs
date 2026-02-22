namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for aborting a rebase operation (<c>git rebase --abort</c>).
/// </summary>
public class GitStopRebaseOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;
}
