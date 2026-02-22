namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Holds the raw output of a <c>git</c> process invocation.
/// </summary>
/// <param name="ExitCode">The process exit code.</param>
/// <param name="StdOut">Everything written to standard output.</param>
/// <param name="StdErr">Everything written to standard error.</param>
public sealed record GitProcessResult(int ExitCode, string StdOut, string StdErr)
{
    /// <summary>
    /// Gets a value indicating whether the process exited successfully (exit code 0).
    /// </summary>
    public bool IsSuccess => ExitCode == 0;
}
