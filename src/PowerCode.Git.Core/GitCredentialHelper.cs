using System;
using System.Diagnostics;

namespace PowerCode.Git.Core;

/// <summary>
/// Queries the system's Git credential helper (e.g., Git Credential Manager)
/// to obtain credentials for a given remote URL via <c>git credential fill</c>.
/// </summary>
internal static class GitCredentialHelper
{
    private static readonly TimeSpan DefaultProcessTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets credentials from the configured Git credential helper for the specified URL.
    /// </summary>
    /// <param name="remoteUrl">The remote URL to look up credentials for.</param>
    /// <returns>
    /// A tuple containing the username and password if credentials were found;
    /// otherwise, <c>(null, null)</c>.
    /// </returns>
    public static (string? Username, string? Password) GetCredentials(string remoteUrl)
        => GetCredentials(remoteUrl, processFactory: null, timeout: null);

    /// <summary>
    /// Gets credentials from the configured Git credential helper for the specified URL.
    /// </summary>
    /// <param name="remoteUrl">The remote URL to look up credentials for.</param>
    /// <param name="processFactory">
    /// An optional factory that starts the credential-fill process from the supplied
    /// <see cref="ProcessStartInfo"/>. When <c>null</c>, <see cref="Process.Start(ProcessStartInfo)"/>
    /// is used. Tests inject a factory that returns a hanging process to exercise the kill path.
    /// </param>
    /// <param name="timeout">
    /// Maximum time to wait for the credential helper process to exit.
    /// When <c>null</c>, defaults to 30 seconds. Tests inject a short value to avoid real waiting.
    /// </param>
    internal static (string? Username, string? Password) GetCredentials(
        string remoteUrl,
        Func<ProcessStartInfo, Process?>? processFactory,
        TimeSpan? timeout)
    {
        try
        {
            if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri))
            {
                return (null, null);
            }

            var input = $"protocol={uri.Scheme}\nhost={uri.Host}\n\n";
            var effectiveTimeout = timeout ?? DefaultProcessTimeout;

            var startInfo = new ProcessStartInfo("git", "credential fill")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            // Prevent Git Credential Manager from showing interactive UI or
            // terminal prompts. This helper should only return already-cached
            // credentials silently.
            startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
            startInfo.Environment["GCM_INTERACTIVE"] = "never";
            // VS Code sets GIT_ASKPASS to its own helper that shows a dialog.
            // Clear it so git does not invoke an external askpass program.
            startInfo.Environment["GIT_ASKPASS"] = "";
            startInfo.Environment["SSH_ASKPASS"] = "";

            using var process = processFactory is not null
                ? processFactory(startInfo)
                : Process.Start(startInfo);

            if (process is null)
            {
                return (null, null);
            }

            process.StandardInput.Write(input);
            process.StandardInput.Close();

            // WaitForExit before ReadToEnd.
            // git credential fill output is tiny (protocol, host, username, password — < 200 bytes),
            // so reading from StandardOutput after the process has exited cannot deadlock on a full
            // OS pipe buffer. For general-purpose process execution this ordering would be risky.
            if (!process.WaitForExit((int)effectiveTimeout.TotalMilliseconds))
            {
                // Credential helper took too long — kill it and return no credentials
                // rather than blocking indefinitely.
                process.Kill();
                process.WaitForExit();
                return (null, null);
            }

            var output = process.StandardOutput.ReadToEnd();

            if (process.ExitCode != 0)
            {
                return (null, null);
            }

            return ParseCredentialOutput(output);
        }
        catch
        {
            // If git is not installed or credential helper fails, return no credentials.
            return (null, null);
        }
    }

    /// <summary>
    /// Parses the key=value output produced by <c>git credential fill</c> and extracts
    /// the <c>username</c> and <c>password</c> fields.
    /// </summary>
    /// <param name="output">The raw stdout text from the credential helper process.</param>
    /// <returns>The parsed username and password, either of which may be <c>null</c>.</returns>
    internal static (string? Username, string? Password) ParseCredentialOutput(string output)
    {
        string? username = null;
        string? password = null;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = line.IndexOf('=');

            if (separatorIndex < 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            switch (key)
            {
                case "username":
                    username = value;
                    break;
                case "password":
                    password = value;
                    break;
            }
        }

        return (username, password);
    }
}
