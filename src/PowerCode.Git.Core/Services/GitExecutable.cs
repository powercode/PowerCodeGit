using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PowerCode.Git.Abstractions.Models;
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
        var result = RunWithResult(workingDirectory, args, standardInput);

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"git {args[0]} failed (exit code {result.ExitCode}): {result.StdErr.Trim()}");
        }
    }

    /// <inheritdoc/>
    public GitProcessResult RunWithResult(string workingDirectory, IReadOnlyList<string> args, string? standardInput = null)
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
            // Use UTF-8 without BOM preamble for stdin so that patch text
            // containing non-ASCII characters (e.g. BOM U+FEFF in file
            // content) is transmitted correctly to git apply.
            StandardInputEncoding = needsStdin ? new UTF8Encoding(false) : null,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git process.");

        if (needsStdin)
        {
            process.StandardInput.Write(standardInput);
            process.StandardInput.Close();
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit(ProcessTimeout);

        return new GitProcessResult(process.ExitCode, stdout, stderr);
    }

    /// <inheritdoc/>
    public int RunInteractive(string workingDirectory, IReadOnlyList<string> args)
    {
        var arguments = string.Join(' ', args.Select(EscapeArgument));

        // Do NOT redirect any streams so that the user's editor and terminal work normally.
        var startInfo = new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start git process.");

        process.WaitForExit();

        return process.ExitCode;
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
