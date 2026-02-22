namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for starting a rebase operation (<c>git rebase</c>).
/// </summary>
public sealed class GitRebaseOptions
{
    /// <summary>
    /// Gets or sets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets or sets the upstream branch or commit to rebase onto.
    /// Equivalent to the <c>&lt;upstream&gt;</c> argument in <c>git rebase &lt;upstream&gt;</c>.
    /// </summary>
    public required string Upstream { get; init; }

    /// <summary>
    /// Gets or sets an optional target ref when using <c>--onto</c>.
    /// When set, the rebase is equivalent to <c>git rebase --onto &lt;Onto&gt; &lt;Upstream&gt;</c>.
    /// </summary>
    public string? Onto { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to open an interactive rebase session
    /// (<c>git rebase -i</c>). When <see langword="true"/> the user's configured editor
    /// is opened; stdin, stdout and stderr are not redirected.
    /// </summary>
    public bool Interactive { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically stash uncommitted
    /// changes before the rebase and restore them afterwards
    /// (<c>git rebase --autostash</c>).
    /// </summary>
    public bool AutoStash { get; init; }

    /// <summary>
    /// Returns a human-readable representation of the options.
    /// </summary>
    public override string ToString()
    {
        var onto = Onto is not null ? $" --onto {Onto}" : string.Empty;
        var interactive = Interactive ? " -i" : string.Empty;
        var autoStash = AutoStash ? " --autostash" : string.Empty;
        return $"git rebase{onto}{interactive}{autoStash} {Upstream} (repo: {RepositoryPath})";
    }
}
