namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for resuming a paused rebase operation.
/// </summary>
public sealed class GitRebaseContinueOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to skip the current conflicting commit
    /// instead of applying it (<c>git rebase --skip</c>). When <see langword="false"/>
    /// (the default), the rebase is resumed normally (<c>git rebase --continue</c>).
    /// </summary>
    public bool Skip { get; init; }

    /// <summary>
    /// Returns a human-readable representation of the options.
    /// </summary>
    public override string ToString()
    {
        var action = Skip ? "--skip" : "--continue";
        return $"git rebase {action} (repo: {RepositoryPath})";
    }
}
