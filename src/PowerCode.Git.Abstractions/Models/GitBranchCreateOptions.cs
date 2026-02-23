namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Options for creating a new branch in a git repository.
/// </summary>
public sealed class GitBranchCreateOptions
{
    /// <summary>
    /// Gets the path to the git repository.
    /// </summary>
    public required string RepositoryPath { get; init; }

    /// <summary>
    /// Gets the name of the new branch.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the committish to branch from. When <see langword="null"/>, HEAD is used.
    /// Equivalent to the <c>[&lt;start-point&gt;]</c> argument of <c>git branch</c>.
    /// </summary>
    public string? StartPoint { get; init; }

    /// <summary>
    /// Gets a value indicating whether to set up the new branch to track the upstream
    /// remote branch (equivalent to <c>git branch --track</c>).
    /// </summary>
    public bool Track { get; init; }

    /// <summary>
    /// Gets a value indicating whether to forcibly reset an existing branch to the
    /// new start point, overwriting any existing branch with the same name
    /// (equivalent to <c>git branch -f</c>).
    /// </summary>
    public bool Force { get; init; }

    /// <summary>
    /// Gets the optional branch description. When set, the value is stored as the
    /// <c>branch.&lt;name&gt;.description</c> configuration entry in the local repository.
    /// </summary>
    public string? Description { get; init; }

    /// <inheritdoc/>
    public override string ToString()
    {
        var parts = new System.Collections.Generic.List<string>
        {
            $"Name={Name}",
        };
        if (StartPoint is not null) parts.Add($"StartPoint={StartPoint}");
        if (Track) parts.Add("track");
        if (Force) parts.Add("force");
        if (Description is not null) parts.Add($"Description={Description}");
        return $"GitBranchCreateOptions({string.Join(", ", parts)})";
    }
}
