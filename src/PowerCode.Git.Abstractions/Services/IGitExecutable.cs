using System.Collections.Generic;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Abstracts the invocation of the <c>git</c> executable so that callers can
/// be tested without spawning real processes.
/// </summary>
public interface IGitExecutable
{
    /// <summary>
    /// Runs <c>git</c> in the specified working directory with the given arguments.
    /// </summary>
    /// <param name="workingDirectory">
    /// The directory in which the git process is started (typically the repository root).
    /// </param>
    /// <param name="args">
    /// The command-line arguments passed to <c>git</c> (e.g. <c>["restore", "--staged", "."]</c>).
    /// </param>
    /// <param name="standardInput">
    /// Optional content written to the process's standard input stream before it is
    /// closed. Pass <see langword="null"/> when no stdin is needed.
    /// </param>
    /// <exception cref="System.InvalidOperationException">
    /// The git process exited with a non-zero exit code.
    /// </exception>
    void Run(string workingDirectory, IReadOnlyList<string> args, string? standardInput = null);
}
