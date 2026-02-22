namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for aborting a rebase operation (<c>git rebase --abort</c>).
/// </summary>
public sealed class GitStopRebaseOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public string RepositoryPath { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override string ToString()
    {
        return OptionsFormatter.Format(nameof(GitStopRebaseOptions), (nameof(RepositoryPath), RepositoryPath));
    }
}
