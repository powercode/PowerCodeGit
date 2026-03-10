using System;
using System.Diagnostics;

namespace PowerCode.Git.Core.Tests;

[TestClass]
public sealed class GitCredentialHelperTests
{
    [TestMethod]
    public void GetCredentials_InvalidUrl_ReturnsNull()
    {
        var (username, password) = GitCredentialHelper.GetCredentials("not-a-url");

        Assert.IsNull(username);
        Assert.IsNull(password);
    }

    [TestMethod]
    public void GetCredentials_EmptyUrl_ReturnsNull()
    {
        var (username, password) = GitCredentialHelper.GetCredentials(string.Empty);

        Assert.IsNull(username);
        Assert.IsNull(password);
    }

    // ---------------------------------------------------------------------------
    // ParseCredentialOutput — unit tests for the output-parsing logic.
    // These avoid spawning any process and run in < 1 ms each.
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void ParseCredentialOutput_FullOutput_ReturnsUsernameAndPassword()
    {
        const string output = "protocol=https\nhost=example.com\nusername=alice\npassword=s3cr3t\n";

        var (username, password) = GitCredentialHelper.ParseCredentialOutput(output);

        Assert.AreEqual("alice", username);
        Assert.AreEqual("s3cr3t", password);
    }

    [TestMethod]
    public void ParseCredentialOutput_OnlyUsername_ReturnsUsernameAndNullPassword()
    {
        const string output = "username=bob\n";

        var (username, password) = GitCredentialHelper.ParseCredentialOutput(output);

        Assert.AreEqual("bob", username);
        Assert.IsNull(password);
    }

    [TestMethod]
    public void ParseCredentialOutput_EmptyOutput_ReturnsNull()
    {
        var (username, password) = GitCredentialHelper.ParseCredentialOutput(string.Empty);

        Assert.IsNull(username);
        Assert.IsNull(password);
    }

    [TestMethod]
    public void ParseCredentialOutput_ValuesWithEqualsSign_PreservesFullValue()
    {
        // Base64-encoded passwords contain '=' — only the first '=' is the separator.
        const string output = "username=user\npassword=abc=def==\n";

        var (username, password) = GitCredentialHelper.ParseCredentialOutput(output);

        Assert.AreEqual("abc=def==", password);
    }

    [TestMethod]
    public void ParseCredentialOutput_WindowsLineEndings_StripsCarriageReturn()
    {
        const string output = "username=carol\r\npassword=pass\r\n";

        var (username, password) = GitCredentialHelper.ParseCredentialOutput(output);

        Assert.AreEqual("carol", username);
        Assert.AreEqual("pass", password);
    }

    // ---------------------------------------------------------------------------
    // Kill path — inject a hanging process with a very short timeout so the test
    // exercises process.Kill() without real waiting.
    // ---------------------------------------------------------------------------

    [TestMethod]
    public void GetCredentials_ProcessExceedsTimeout_KillsProcessAndReturnsNull()
    {
        static Process? HangingProcessFactory(ProcessStartInfo _)
        {
            var hangInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "ping" : "sleep",
                Arguments = OperatingSystem.IsWindows() ? "-n 100 127.0.0.1" : "100",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            return Process.Start(hangInfo);
        }

        var (username, password) = GitCredentialHelper.GetCredentials(
            "https://example.com/repo.git",
            processFactory: HangingProcessFactory,
            timeout: TimeSpan.FromMilliseconds(50));

        Assert.IsNull(username);
        Assert.IsNull(password);
    }
}
