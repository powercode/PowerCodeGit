using System.Collections;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Tests.Completers;

[TestClass]
public sealed class GitConfigNameCompleterTests
{
    private static readonly Hashtable BoundParameters = new() { ["RepoPath"] = "C:\\repo" };

    [TestMethod]
    public void CompleteArgument_EmptyWord_ReturnsStaticAndDynamicKeys()
    {
        var service = new StubGitConfigService(
        [
            new() { Name = "user.name", Value = "Jane" },
            new() { Name = "custom.key", Value = "val" },
        ]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "", null!, BoundParameters).ToList();

        // All static keys + one extra dynamic key ("custom.key"; "user.name" is already static)
        Assert.IsGreaterThan(GitConfigNameCompleterAttribute.KnownKeys.Count, results.Count);
        Assert.IsTrue(results.Exists(r => r.CompletionText == "custom.key"));
    }

    [TestMethod]
    public void CompleteArgument_PrefixFilter_ReturnsOnlyMatching()
    {
        var service = new StubGitConfigService([]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "core.auto", null!, BoundParameters).ToList();

        Assert.IsNotEmpty(results);
        Assert.IsTrue(results.All(r => r.CompletionText.StartsWith("core.auto", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void CompleteArgument_CaseInsensitiveMatch_ReturnsMatching()
    {
        var service = new StubGitConfigService([]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "USER.N", null!, BoundParameters).ToList();

        Assert.IsTrue(results.Exists(r => r.CompletionText == "user.name"));
    }

    [TestMethod]
    public void CompleteArgument_NoMatch_ReturnsEmpty()
    {
        var service = new StubGitConfigService([]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "zzz.nonexistent", null!, BoundParameters).ToList();

        Assert.IsEmpty(results);
    }

    [TestMethod]
    public void CompleteArgument_StaticKey_TooltipHasDescription()
    {
        var service = new StubGitConfigService([]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "user.name", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("Your full name attached to every commit and tag", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_DynamicKey_TooltipShowsCurrentValue()
    {
        var service = new StubGitConfigService(
        [
            new() { Name = "custom.setting", Value = "myvalue" },
        ]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "custom.setting", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        Assert.AreEqual("custom.setting=myvalue", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_StaticKeyOverridesDynamicTooltip()
    {
        var service = new StubGitConfigService(
        [
            new() { Name = "user.name", Value = "Jane" },
        ]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "user.name", null!, BoundParameters).ToList();

        Assert.HasCount(1, results);
        // Static description takes precedence, not "user.name=Jane"
        Assert.AreEqual("Your full name attached to every commit and tag", results[0].ToolTip);
    }

    [TestMethod]
    public void CompleteArgument_ServiceThrows_ReturnsStaticKeysOnly()
    {
        var service = new ThrowingGitConfigService();
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "user.", null!, BoundParameters).ToList();

        Assert.IsNotEmpty(results);
        Assert.IsTrue(results.All(r => r.CompletionText.StartsWith("user.", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public void CompleteArgument_AllResults_HaveParameterValueType()
    {
        var service = new StubGitConfigService([]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "core.", null!, BoundParameters).ToList();

        Assert.IsNotEmpty(results);
        Assert.IsTrue(results.All(r => r.ResultType == CompletionResultType.ParameterValue));
    }

    [TestMethod]
    public void CompleteArgument_ResultsAreSortedAlphabetically()
    {
        var service = new StubGitConfigService([]);
        var completer = new GitConfigNameCompleterAttribute.ConfigNameCompleter(service);

        var results = completer.CompleteArgument("Set-GitConfiguration", "Name", "core.", null!, BoundParameters).ToList();

        var sorted = results.OrderBy(r => r.CompletionText, StringComparer.OrdinalIgnoreCase).ToList();
        CollectionAssert.AreEqual(sorted.Select(r => r.CompletionText).ToList(), results.Select(r => r.CompletionText).ToList());
    }

    private sealed class StubGitConfigService(IReadOnlyList<GitConfigEntry> entries) : IGitConfigService
    {
        public IReadOnlyList<GitConfigEntry> GetConfigEntries(GitConfigGetOptions options) => entries;

        public GitConfigEntry? GetConfigValue(GitConfigGetOptions options) =>
            throw new NotImplementedException();

        public void SetConfigValue(GitConfigSetOptions options) =>
            throw new NotImplementedException();

        public void UnsetConfigValue(GitConfigUnsetOptions options) =>
            throw new NotImplementedException();
    }

    private sealed class ThrowingGitConfigService : IGitConfigService
    {
        public IReadOnlyList<GitConfigEntry> GetConfigEntries(GitConfigGetOptions options) =>
            throw new InvalidOperationException("Not a git repository");

        public GitConfigEntry? GetConfigValue(GitConfigGetOptions options) =>
            throw new InvalidOperationException("Not a git repository");

        public void SetConfigValue(GitConfigSetOptions options) =>
            throw new InvalidOperationException("Not a git repository");

        public void UnsetConfigValue(GitConfigUnsetOptions options) =>
            throw new InvalidOperationException("Not a git repository");
    }
}
