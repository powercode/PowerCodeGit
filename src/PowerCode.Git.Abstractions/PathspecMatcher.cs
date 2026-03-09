using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PowerCode.Git.Abstractions;

/// <summary>
/// Provides git-style pathspec glob matching for repository-relative file paths.
/// <para>
/// Supported patterns:
/// <list type="bullet">
///   <item><description>Exact path: <c>src/foo.cs</c></description></item>
///   <item><description>Directory prefix: <c>src/</c> (matches all files under <c>src/</c>)</description></item>
///   <item><description>Single-star glob: <c>*.cs</c> (matches within one directory segment)</description></item>
///   <item><description>Double-star glob: <c>**/*.cs</c> (matches across directory boundaries)</description></item>
///   <item><description>Question mark: <c>?.cs</c> (matches exactly one non-separator character)</description></item>
/// </list>
/// </para>
/// </summary>
/// <example>
/// <code>
/// string[] patterns = ["**/*.cs"];
/// bool match = PathspecMatcher.IsMatch("src/Models/Foo.cs", patterns); // true
/// </code>
/// </example>
public static class PathspecMatcher
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="filePath"/> matches at least
    /// one of the given pathspec <paramref name="patterns"/>.
    /// </summary>
    /// <param name="filePath">A repository-relative file path (forward-slash separated).</param>
    /// <param name="patterns">One or more pathspec glob patterns.</param>
    public static bool IsMatch(string filePath, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (IsMatch(filePath, pattern))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="filePath"/> matches
    /// the given pathspec <paramref name="pattern"/>.
    /// </summary>
    /// <param name="filePath">A repository-relative file path (forward-slash separated).</param>
    /// <param name="pattern">A pathspec glob pattern.</param>
    public static bool IsMatch(string filePath, string pattern)
    {
        var regex = ConvertToRegex(pattern);
        return regex.IsMatch(NormalizePath(filePath));
    }

    /// <summary>
    /// Creates a sequence of compiled <see cref="Regex"/> objects from pathspec patterns,
    /// suitable for repeated matching across many file paths.
    /// </summary>
    /// <param name="patterns">One or more pathspec glob patterns.</param>
    /// <returns>A list of compiled regexes, one per pattern.</returns>
    public static IReadOnlyList<Regex> CompilePatterns(string[] patterns)
    {
        var result = new Regex[patterns.Length];

        for (var i = 0; i < patterns.Length; i++)
        {
            result[i] = ConvertToRegex(patterns[i]);
        }

        return result;
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="filePath"/> matches at least
    /// one of the pre-compiled <paramref name="regexes"/>.
    /// </summary>
    /// <param name="filePath">A repository-relative file path (forward-slash separated).</param>
    /// <param name="regexes">Pre-compiled pathspec regexes from <see cref="CompilePatterns"/>.</param>
    public static bool IsMatch(string filePath, IReadOnlyList<Regex> regexes)
    {
        var normalized = NormalizePath(filePath);

        foreach (var regex in regexes)
        {
            if (regex.IsMatch(normalized))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Converts a single git-style pathspec pattern to a compiled <see cref="Regex"/>.
    /// </summary>
    internal static Regex ConvertToRegex(string pattern)
    {
        var normalized = NormalizePath(pattern);

        // Directory prefix: "src/" matches everything under src/
        if (normalized.EndsWith('/'))
        {
            var escaped = Regex.Escape(normalized);
            return new Regex($"^{escaped}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        // Build regex by scanning the pattern character by character.
        var regexPattern = BuildRegexPattern(normalized);
        return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private static string BuildRegexPattern(string pattern)
    {
        var result = new System.Text.StringBuilder(pattern.Length * 2);
        var span = pattern.AsSpan();

        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];

            if (c == '*')
            {
                if (i + 1 < span.Length && span[i + 1] == '*')
                {
                    // "**" — match across directory boundaries
                    i++; // consume second '*'

                    if (i + 1 < span.Length && span[i + 1] == '/')
                    {
                        // "**/" — match zero or more directory segments
                        i++; // consume '/'
                        result.Append("(.+/)?");
                    }
                    else
                    {
                        // "**" at end or not followed by '/' — match anything
                        result.Append(".*");
                    }
                }
                else
                {
                    // Single "*" — match anything except '/'
                    result.Append("[^/]*");
                }
            }
            else if (c == '?')
            {
                // "?" — match exactly one character that is not '/'
                result.Append("[^/]");
            }
            else
            {
                // Escape regex-special characters
                result.Append(Regex.Escape(c.ToString()));
            }
        }

        return result.ToString();
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/');
}
