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
    /// Gets or sets a value indicating whether to automatically apply <c>fixup!</c> and
    /// <c>squash!</c> commits during an interactive rebase
    /// (<c>git rebase -i --autosquash</c>).
    /// </summary>
    public bool AutoSquash { get; init; }

    /// <summary>
    /// Gets or sets an optional shell command to execute after each rebased commit
    /// during an interactive rebase (<c>git rebase -i --exec &lt;cmd&gt;</c>).
    /// When set, a corresponding <c>exec</c> line is inserted after every <c>pick</c>
    /// line in the todo list.
    /// </summary>
    public string? Exec { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to recreate merge commits during the
    /// rebase rather than linearising the history
    /// (<c>git rebase --rebase-merges</c>).
    /// </summary>
    public bool RebaseMerges { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether to automatically update any branches
    /// that point to commits being rebased — useful when working with stacked branches
    /// (<c>git rebase --update-refs</c>).
    /// </summary>
    public bool UpdateRefs { get; init; }

    /// <summary>
    /// Returns a human-readable representation of the options.
    /// </summary>
    public override string ToString()
    {
        var onto = Onto is not null ? $" --onto {Onto}" : string.Empty;
        var interactive = Interactive ? " -i" : string.Empty;
        var autoStash = AutoStash ? " --autostash" : string.Empty;
        var autoSquash = AutoSquash ? " --autosquash" : string.Empty;
        var exec = Exec is not null ? $" --exec {Exec}" : string.Empty;
        var rebaseMerges = RebaseMerges ? " --rebase-merges" : string.Empty;
        var updateRefs = UpdateRefs ? " --update-refs" : string.Empty;
        return $"git rebase{onto}{interactive}{autoSquash}{exec}{rebaseMerges}{updateRefs}{autoStash} {Upstream} (repo: {RepositoryPath})";
    }
}
