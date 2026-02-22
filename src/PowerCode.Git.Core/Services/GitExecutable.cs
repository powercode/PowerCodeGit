using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Core.Services;

/// <summary>
/// Runs the <c>git</c> executable as a child process.
/// </summary>
public sealed class GitExecutable : IGitExecutable
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);

    /// <inheritdoc/>
    public void Run(string workingDirectory, IReadOnlyList<string> args, string? standardInput = null)
    {
        var arguments = string.Join(' ', args.Select(EscapeArgument));
        var needsStdin = standardInput is not null;

        var startInfo = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = needsStdin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git process.");

        if (needsStdin)
        {
            process.StandardInput.Write(standardInput);
            process.StandardInput.Close();
        }

        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(ProcessTimeout);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {args[0]} failed (exit code {process.ExitCode}): {stderr.Trim()}");
        }
    }

    /// <summary>
    /// Wraps an argument in double quotes if it contains spaces or special characters.
    /// </summary>
    private static string EscapeArgument(string arg)
    {
        // Already quoted or a simple switch (--flag, -f, --source=value).
        if (arg.StartsWith('-') || !arg.Contains(' '))
        {
            return arg;
        }

        return $"\"{arg.Replace("\"", "\\\"")}\"";
    }
}
