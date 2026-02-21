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

    [TestMethod]
    public void GetCredentials_ValidHttpsUrl_ReturnsCredentialsOrNull()
    {
        // This test verifies the helper does not throw for a valid URL.
        // The actual result depends on whether a credential helper is configured.
        var (username, _) = GitCredentialHelper.GetCredentials("https://example.com/repo.git");

        // We only assert no exception was thrown; the credential helper
        // may or may not have credentials for this host.
        Assert.IsTrue(username is null || username.Length > 0);
    }
}
