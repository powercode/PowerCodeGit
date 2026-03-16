using System;

namespace PowerCode.Git.Abstractions;

/// <summary>
/// Provides Optimal String Alignment (OSA) distance calculations — a variant
/// of Damerau-Levenshtein that penalises insertions, deletions, substitutions,
/// and transpositions of two adjacent characters.
/// </summary>
/// <remarks>
/// The OSA metric differs from the true Damerau-Levenshtein distance in that
/// it does not allow a transposition to be composed with another edit; this
/// makes it sufficient, and significantly simpler, for the heuristic use case
/// of deciding whether two diff lines represent an edit of the same content
/// rather than a complete replacement.
/// </remarks>
public static class DamerauLevenshteinDistance
{
    /// <summary>
    /// Computes the Optimal String Alignment distance between <paramref name="source"/>
    /// and <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// The result is the minimum number of single-character operations —
    /// insertion, deletion, substitution, and adjacent transposition — required
    /// to transform <paramref name="source"/> into <paramref name="target"/>.
    /// </remarks>
    /// <param name="source">The source text.</param>
    /// <param name="target">The target text.</param>
    /// <returns>
    /// A non-negative integer. Zero means the strings are identical.
    /// </returns>
    public static int Compute(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
    {
        if (source.IsEmpty)
        {
            return target.Length;
        }

        if (target.IsEmpty)
        {
            return source.Length;
        }

        var m = source.Length;
        var n = target.Length;

        // d[i, j] = OSA distance between source[0..i-1] and target[0..j-1].
        // We use a flat array with manual index arithmetic to avoid 2-D array overhead.
        var d = new int[(m + 1) * (n + 1)];

        // Index helper: row i, column j.
        int Idx(int i, int j) => i * (n + 1) + j;

        for (var i = 0; i <= m; i++)
        {
            d[Idx(i, 0)] = i;
        }

        for (var j = 0; j <= n; j++)
        {
            d[Idx(0, j)] = j;
        }

        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                d[Idx(i, j)] = Math.Min(
                    Math.Min(
                        d[Idx(i - 1, j)] + 1,       // deletion from source
                        d[Idx(i, j - 1)] + 1),       // insertion into source
                    d[Idx(i - 1, j - 1)] + cost);    // substitution (or match)

                // Adjacent transposition: a[i-1]==b[j-2] && a[i-2]==b[j-1]
                if (i > 1 && j > 1 &&
                    source[i - 1] == target[j - 2] &&
                    source[i - 2] == target[j - 1])
                {
                    d[Idx(i, j)] = Math.Min(d[Idx(i, j)], d[Idx(i - 2, j - 2)] + 1);
                }
            }
        }

        return d[Idx(m, n)];
    }

    /// <summary>
    /// Computes a normalized similarity score in the range [0.0, 1.0],
    /// where <c>1.0</c> means identical and <c>0.0</c> means maximally dissimilar.
    /// </summary>
    /// <remarks>
    /// The score is computed as
    /// <c>1.0 − distance / max(source.Length, target.Length)</c>.
    /// Both-empty inputs return <c>1.0</c> (trivially identical).
    /// </remarks>
    /// <param name="source">The source text.</param>
    /// <param name="target">The target text.</param>
    /// <returns>A similarity score in [0.0, 1.0].</returns>
    public static double ComputeNormalized(ReadOnlySpan<char> source, ReadOnlySpan<char> target)
    {
        if (source.IsEmpty && target.IsEmpty)
        {
            return 1.0;
        }

        var maxLen = Math.Max(source.Length, target.Length);

        return 1.0 - Compute(source, target) / (double)maxLen;
    }
}
