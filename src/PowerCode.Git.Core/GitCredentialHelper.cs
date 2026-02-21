using System;
using System.Diagnostics;

namespace PowerCode.Git.Core;

/// <summary>
/// Queries the system's Git credential helper (e.g., Git Credential Manager)
/// to obtain credentials for a given remote URL via <c>git credential fill</c>.
/// </summary>
internal static class GitCredentialHelper
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets credentials from the configured Git credential helper for the specified URL.
    /// </summary>
    /// <param name="remoteUrl">The remote URL to look up credentials for.</param>
    /// <returns>
    /// A tuple containing the username and password if credentials were found;
    /// otherwise, <c>(null, null)</c>.
    /// </returns>
    public static (string? Username, string? Password) GetCredentials(string remoteUrl)
    {
        try
        {
            if (!Uri.TryCreate(remoteUrl, UriKind.Absolute, out var uri))
            {
                return (null, null);
            }

            var input = $"protocol={uri.Scheme}\nhost={uri.Host}\n\n";

            var startInfo = new ProcessStartInfo("git", "credential fill")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return (null, null);
            }

            process.StandardInput.Write(input);
            process.StandardInput.Close();

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(ProcessTimeout);

            if (process.ExitCode != 0)
            {
                return (null, null);
            }

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
        catch
        {
            // If git is not installed or credential helper fails, return no credentials.
            return (null, null);
        }
    }
}
