using System;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Encapsulates caller-requested overrides for a single commit's metadata during a history rewrite.
/// </summary>
/// <remarks>
/// <para>
/// An instance of this class is returned by the <c>CommitFilter</c> delegate (wrapped from the
/// PowerShell <c>-CommitFilter</c> ScriptBlock). Each non-null property overrides the
/// corresponding field of the original commit. Properties that are <see langword="null"/>
/// preserve the original commit's values.
/// </para>
/// <para>
/// This type lives in <c>PowerCode.Git.Abstractions</c> so it can cross the Assembly Load
/// Context boundary between the cmdlet layer (default ALC) and the core layer (isolated ALC)
/// without requiring reflection at the boundary. All properties use BCL types only.
/// </para>
/// </remarks>
public sealed class GitCommitRewriteOverrides
{
    /// <summary>
    /// Gets the new author name, or <see langword="null"/> to keep the original.
    /// </summary>
    public string? AuthorName { get; init; }

    /// <summary>
    /// Gets the new author email address, or <see langword="null"/> to keep the original.
    /// </summary>
    public string? AuthorEmail { get; init; }

    /// <summary>
    /// Gets the new author timestamp, or <see langword="null"/> to keep the original.
    /// </summary>
    public DateTimeOffset? AuthorWhen { get; init; }

    /// <summary>
    /// Gets the new committer name, or <see langword="null"/> to keep the original.
    /// </summary>
    public string? CommitterName { get; init; }

    /// <summary>
    /// Gets the new committer email address, or <see langword="null"/> to keep the original.
    /// </summary>
    public string? CommitterEmail { get; init; }

    /// <summary>
    /// Gets the new committer timestamp, or <see langword="null"/> to keep the original.
    /// </summary>
    public DateTimeOffset? CommitterWhen { get; init; }

    /// <summary>
    /// Gets the new commit message, or <see langword="null"/> to keep the original.
    /// </summary>
    public string? Message { get; init; }
}
