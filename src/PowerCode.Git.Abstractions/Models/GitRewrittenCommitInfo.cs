using System;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Describes a single commit that was, or would be, rewritten by <c>Edit-GitHistory</c>.
/// </summary>
/// <remarks>
/// Emitted to the PowerShell pipeline once per rewritten commit so callers can inspect
/// what changed, audit the rewrite, or pipe into further filtering with <c>Where-Object</c>.
/// Properties reflect the <em>original</em> commit's values; the <c>*Modified</c> flags
/// indicate which aspects were changed.
/// </remarks>
public sealed class GitRewrittenCommitInfo
{
    /// <summary>
    /// Gets the SHA hash of the original (pre-rewrite) commit.
    /// </summary>
    public required string OriginalSha { get; init; }

    /// <summary>
    /// Gets the original author's display name.
    /// </summary>
    public required string AuthorName { get; init; }

    /// <summary>
    /// Gets the original author's email address.
    /// </summary>
    public required string AuthorEmail { get; init; }

    /// <summary>
    /// Gets the original author timestamp.
    /// </summary>
    public required DateTimeOffset AuthorWhen { get; init; }

    /// <summary>
    /// Gets the first line of the original commit message (the subject).
    /// </summary>
    public required string MessageShort { get; init; }

    /// <summary>
    /// Gets a value indicating whether the author or committer metadata was changed.
    /// </summary>
    public bool HeaderModified { get; init; }

    /// <summary>
    /// Gets a value indicating whether the commit message was changed.
    /// </summary>
    public bool MessageModified { get; init; }

    /// <summary>
    /// Gets a value indicating whether the commit tree (file contents) was changed.
    /// </summary>
    public bool TreeModified { get; init; }

    /// <summary>
    /// Gets a value indicating whether the commit's parent links were changed.
    /// </summary>
    public bool ParentsModified { get; init; }
}
