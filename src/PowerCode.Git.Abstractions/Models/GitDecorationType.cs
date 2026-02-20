namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Classifies the kind of reference that decorates a commit.
/// </summary>
public enum GitDecorationType
{
    /// <summary>The HEAD pointer.</summary>
    Head,

    /// <summary>A local branch reference (refs/heads/*).</summary>
    LocalBranch,

    /// <summary>A remote-tracking branch reference (refs/remotes/*).</summary>
    RemoteBranch,

    /// <summary>A tag reference (refs/tags/*).</summary>
    Tag,
}
