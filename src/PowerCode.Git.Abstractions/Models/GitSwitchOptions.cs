namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for switching the current branch of a git repository
/// (equivalent to <c>git switch</c> / <c>git checkout &lt;branch&gt;</c>).
/// </summary>
public sealed class GitSwitchOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the branch to switch to.
    /// Used with the default <c>Switch</c> parameter set and the <c>Create</c> set.
    /// </summary>
    public string? BranchName { get; init; }

    /// <summary>
    /// Gets a value indicating whether to create the branch before switching
    /// (equivalent to <c>git switch -c &lt;branch&gt;</c>).
    /// </summary>
    public bool Create { get; init; }

    /// <summary>
    /// Gets an optional committish (commit SHA, tag, or branch) to use as the starting
    /// point when creating a new branch (equivalent to <c>git switch -c &lt;branch&gt; &lt;start-point&gt;</c>).
    /// </summary>
    public string? StartPoint { get; init; }

    /// <summary>
    /// Gets a value indicating whether to detach HEAD at the given committish
    /// (equivalent to <c>git switch --detach &lt;committish&gt;</c>).
    /// </summary>
    public bool Detach { get; init; }

    /// <summary>
    /// Gets the committish to check out in detached HEAD mode.
    /// Used only when <see cref="Detach"/> is <see langword="true"/>.
    /// </summary>
    public string? Committish { get; init; }

    /// <summary>
    /// Gets a value indicating whether to force the checkout, discarding local changes
    /// (equivalent to <c>git switch --force</c> / <c>git checkout -f</c>).
    /// </summary>
    public bool Force { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>();
        if (BranchName is not null) parts.Add($"branch={BranchName}");
        if (Create) parts.Add("create");
        if (StartPoint is not null) parts.Add($"startPoint={StartPoint}");
        if (Detach) parts.Add("detach");
        if (Committish is not null) parts.Add($"committish={Committish}");
        if (Force) parts.Add("force");
        return parts.Count > 0
            ? $"GitSwitchOptions({string.Join(", ", parts)})"
            : "GitSwitchOptions()";
    }
}
