using System.Collections.Generic;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Tests;

/// <summary>
/// A recording stub for <see cref="IGitExecutable"/> that captures calls
/// instead of spawning real git processes.
/// </summary>
internal sealed class StubGitExecutable : IGitExecutable
{
    /// <summary>
    /// Represents a single recorded invocation of <see cref="IGitExecutable.Run"/>.
    /// </summary>
    /// <param name="WorkingDirectory">The working directory passed to the call.</param>
    /// <param name="Args">The argument list passed to the call.</param>
    /// <param name="StandardInput">The standard input content, if any.</param>
    internal record Invocation(string WorkingDirectory, IReadOnlyList<string> Args, string? StandardInput);

    /// <summary>
    /// Gets the list of recorded invocations in order.
    /// </summary>
    public List<Invocation> Invocations { get; } = [];

    /// <summary>
    /// When set, the next call to <see cref="Run"/> throws this exception
    /// instead of recording the invocation.
    /// </summary>
    public System.Exception? ExceptionToThrow { get; set; }

    /// <summary>
    /// The result returned by <see cref="RunWithResult"/>. Defaults to a
    /// successful (exit code 0) result with empty output.
    /// </summary>
    public GitProcessResult ResultToReturn { get; set; } = new(0, string.Empty, string.Empty);

    /// <inheritdoc/>
    public void Run(string workingDirectory, IReadOnlyList<string> args, string? standardInput = null)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        Invocations.Add(new Invocation(workingDirectory, args, standardInput));
    }

    /// <inheritdoc/>
    public GitProcessResult RunWithResult(string workingDirectory, IReadOnlyList<string> args, string? standardInput = null)
    {
        Invocations.Add(new Invocation(workingDirectory, args, standardInput));
        return ResultToReturn;
    }

    /// <inheritdoc/>
    public int RunInteractive(string workingDirectory, IReadOnlyList<string> args)
    {
        Invocations.Add(new Invocation(workingDirectory, args, null));
        return 0;
    }
}
