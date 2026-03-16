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
/// <param name="Similarity">
/// For <see cref="GitDiffLineKind.Modified"/> lines, the normalized
/// Damerau-Levenshtein similarity score [0.0, 1.0] between the removed line
/// and the added line (1.0 = identical, 0.0 = entirely different).
/// <see langword="null"/> for <see cref="GitDiffLineKind.Added"/> and
/// <see cref="GitDiffLineKind.Removed"/> lines.
/// </param>
/// <param name="MaxLineLength">
/// For <see cref="GitDiffLineKind.Modified"/> lines, the maximum character
/// length of the two paired lines (<c>max(oldLen, newLen)</c>). Used together
/// with <paramref name="Similarity"/> to apply a length-adaptive threshold
/// when classifying a hunk (see <see cref="GitDiffHunk.ChangeKind"/>).
/// <see langword="null"/> for <see cref="GitDiffLineKind.Added"/> and
/// <see cref="GitDiffLineKind.Removed"/> lines.
/// </param>
public sealed record GitDiffLine(
    int? OldLineNumber,
    int? NewLineNumber,
    GitDiffLineKind Kind,
    string Content,
    double? Similarity = null,
    int? MaxLineLength = null);
