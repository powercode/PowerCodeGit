namespace PowerCode.Git.Formatting;

/// <summary>
/// ANSI/VT100 escape code constants for terminal coloring.
/// PowerShell 7.4+ automatically strips these when <c>$PSStyle.OutputRendering</c>
/// is <c>PlainText</c> or when output is redirected to a file.
/// </summary>
/// <example>
/// <code>
/// var colored = AnsiCodes.Colorize("hello", AnsiCodes.Green);
/// // produces "\x1b[32mhello\x1b[0m"
/// </code>
/// </example>
public static class AnsiCodes
{
    /// <summary>Reset all attributes.</summary>
    public const string Reset = "\x1b[0m";

    /// <summary>Red foreground.</summary>
    public const string Red = "\x1b[31m";

    /// <summary>Green foreground.</summary>
    public const string Green = "\x1b[32m";

    /// <summary>Yellow foreground.</summary>
    public const string Yellow = "\x1b[33m";

    /// <summary>Blue foreground.</summary>
    public const string Blue = "\x1b[34m";

    /// <summary>Magenta foreground.</summary>
    public const string Magenta = "\x1b[35m";

    /// <summary>Cyan foreground.</summary>
    public const string Cyan = "\x1b[36m";

    /// <summary>Bold + red foreground.</summary>
    public const string BoldRed = "\x1b[1;31m";

    /// <summary>Bold + green foreground.</summary>
    public const string BoldGreen = "\x1b[1;32m";

    /// <summary>Bold + yellow foreground.</summary>
    public const string BoldYellow = "\x1b[1;33m";

    /// <summary>Bold + cyan foreground.</summary>
    public const string BoldCyan = "\x1b[1;36m";

    /// <summary>Bold (increased intensity) text.</summary>
    public const string Bold = "\x1b[1m";

    /// <summary>Dim (faint) foreground.</summary>
    public const string Dim = "\x1b[2m";

    /// <summary>
    /// Wraps <paramref name="text"/> with the given ANSI <paramref name="colorCode"/>
    /// and appends a <see cref="Reset"/> sequence.
    /// </summary>
    /// <param name="text">The text to colorize.</param>
    /// <param name="colorCode">An ANSI escape sequence (e.g. <see cref="Green"/>).</param>
    /// <returns>The text wrapped in ANSI escape codes.</returns>
    public static string Colorize(string text, string colorCode) =>
        $"{colorCode}{text}{Reset}";
}
