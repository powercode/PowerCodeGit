using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;

namespace PowerCode.Git.Abstractions.Services;

/// <summary>
/// Abstracts the invocation of the <c>git</c> executable so that callers can
/// be tested without spawning real processes.
/// </summary>
public interface IGitExecutable
{
    /// <summary>
    /// Runs <c>git</c> in the specified working directory with the given arguments.
    /// Throws <see cref="System.InvalidOperationException"/> on non-zero exit.
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

    /// <summary>
    /// Runs <c>git</c> and returns all output without throwing on non-zero exit codes.
    /// The caller is responsible for interpreting <see cref="GitProcessResult.ExitCode"/>.
    /// </summary>
    /// <param name="workingDirectory">
    /// The directory in which the git process is started (typically the repository root).
    /// </param>
    /// <param name="args">
    /// The command-line arguments passed to <c>git</c>.
    /// </param>
    /// <param name="standardInput">
    /// Optional content written to the process's standard input stream before it is closed.
    /// Pass <see langword="null"/> when no stdin is needed.
    /// </param>
    /// <returns>
    /// A <see cref="GitProcessResult"/> containing the exit code, stdout, and stderr.
    /// </returns>
    GitProcessResult RunWithResult(string workingDirectory, IReadOnlyList<string> args, string? standardInput = null);

    /// <summary>
    /// Runs <c>git</c> without redirecting stdin, stdout, or stderr, so that
    /// interactive commands (such as <c>git rebase -i</c>) can open the user's
    /// configured editor and display progress in the terminal.
    /// </summary>
    /// <param name="workingDirectory">
    /// The directory in which the git process is started (typically the repository root).
    /// </param>
    /// <param name="args">
    /// The command-line arguments passed to <c>git</c>.
    /// </param>
    /// <returns>The process exit code.</returns>
    int RunInteractive(string workingDirectory, IReadOnlyList<string> args);
}
