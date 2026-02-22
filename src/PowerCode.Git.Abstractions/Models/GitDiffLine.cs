namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Represents a single changed line within a <see cref="GitDiffHunk"/>,
/// paired with its old and new line numbers and the kind of change.
/// </summary>
/// <param name="OldLineNumber">
/// The 1-based line number in the old file, or <see langword="null"/> for
/// standalone <see cref="GitDiffLineKind.Added"/> lines.
/// </param>
/// <param name="NewLineNumber">
/// The 1-based line number in the new file, or <see langword="null"/> for
/// standalone <see cref="GitDiffLineKind.Removed"/> lines.
/// </param>
/// <param name="Kind">The kind of change for this line.</param>
/// <param name="Content">
/// The line text without the leading <c>+</c>, <c>-</c> or space sigil.
/// </param>
public sealed record GitDiffLine(
    int? OldLineNumber,
    int? NewLineNumber,
    GitDiffLineKind Kind,
    string Content);
