using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace PowerCode.Git.Completers;

/// <summary>
/// Provides context-sensitive argument completions for git configuration values.
/// </summary>
/// <remarks>
/// Reads the already-bound <c>Name</c> parameter from <c>FakeBoundParameters</c>
/// and offers known valid values for that configuration key. For example, when
/// <c>Name</c> is <c>core.autocrlf</c> the completer offers <c>true</c>,
/// <c>false</c>, and <c>input</c>. When no known values exist for the given
/// key, the completer returns an empty list.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitConfigValueCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        return new ConfigValueCompleter();
    }

    /// <summary>
    /// Maps well-known configuration keys to their set of valid values.
    /// Keys are compared case-insensitively.
    /// </summary>
    internal static readonly Dictionary<string, string[]> KnownValues = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── core ──
        ["core.autocrlf"] = ["true", "false", "input"],
        ["core.eol"] = ["lf", "crlf", "native"],
        ["core.filemode"] = ["true", "false"],
        ["core.ignorecase"] = ["true", "false"],
        ["core.bare"] = ["true", "false"],
        ["core.logallrefupdates"] = ["true", "false", "always"],
        ["core.symlinks"] = ["true", "false"],
        ["core.longpaths"] = ["true", "false"],
        ["core.untrackedcache"] = ["true", "false"],
        ["core.quotepath"] = ["true", "false"],
        ["core.safecrlf"] = ["true", "false", "warn"],

        // ── init ──
        ["init.defaultBranch"] = ["main", "master", "develop", "trunk"],

        // ── push ──
        ["push.default"] = ["nothing", "current", "upstream", "simple", "matching"],
        ["push.autoSetupRemote"] = ["true", "false"],
        ["push.followTags"] = ["true", "false"],

        // ── pull ──
        ["pull.rebase"] = ["true", "false", "interactive", "merges"],
        ["pull.ff"] = ["true", "false", "only"],

        // ── fetch ──
        ["fetch.prune"] = ["true", "false"],
        ["fetch.pruneTags"] = ["true", "false"],

        // ── merge ──
        ["merge.ff"] = ["true", "false", "only"],
        ["merge.conflictstyle"] = ["merge", "diff3", "zdiff3"],

        // ── rebase ──
        ["rebase.autosquash"] = ["true", "false"],
        ["rebase.autostash"] = ["true", "false"],
        ["rebase.updateRefs"] = ["true", "false"],

        // ── diff ──
        ["diff.algorithm"] = ["patience", "minimal", "histogram", "myers"],
        ["diff.colorMoved"] = ["no", "default", "plain", "blocks", "zebra", "dimmed-zebra"],
        ["diff.renames"] = ["true", "false", "copies"],

        // ── color ──
        ["color.ui"] = ["auto", "always", "never"],
        ["color.diff"] = ["auto", "always", "never"],
        ["color.status"] = ["auto", "always", "never"],
        ["color.branch"] = ["auto", "always", "never"],

        // ── credential ──
        ["credential.helper"] = ["store", "cache", "manager"],

        // ── http ──
        ["http.sslVerify"] = ["true", "false"],

        // ── gc ──
        ["gc.autoDetach"] = ["true", "false"],

        // ── advice ──
        ["advice.pushNonFastForward"] = ["true", "false"],
        ["advice.statusHints"] = ["true", "false"],
        ["advice.detachedHead"] = ["true", "false"],
        ["advice.addIgnoredFile"] = ["true", "false"],

        // ── branch ──
        ["branch.autoSetupMerge"] = ["true", "false", "always"],
        ["branch.autoSetupRebase"] = ["never", "local", "remote", "always"],

        // ── log ──
        ["log.decorate"] = ["true", "false", "short", "full", "auto"],
        ["log.abbrevCommit"] = ["true", "false"],

        // ── rerere ──
        ["rerere.enabled"] = ["true", "false"],
        ["rerere.autoupdate"] = ["true", "false"],

        // ── commit ──
        ["commit.gpgsign"] = ["true", "false"],
        ["commit.verbose"] = ["true", "false"],

        // ── status ──
        ["status.showUntrackedFiles"] = ["no", "normal", "all"],
        ["status.submoduleSummary"] = ["true", "false"],

        // ── submodule ──
        ["submodule.recurse"] = ["true", "false"],

        // ── transfer ──
        ["transfer.fsckObjects"] = ["true", "false"],

        // ── receive ──
        ["receive.denyCurrentBranch"] = ["refuse", "warn", "ignore", "updateInstead"],
    };

    internal sealed class ConfigValueCompleter : IArgumentCompleter
    {
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            try
            {
                if (fakeBoundParameters is null ||
                    !fakeBoundParameters.Contains("Name") ||
                    fakeBoundParameters["Name"] is not string name ||
                    string.IsNullOrWhiteSpace(name))
                {
                    return [];
                }

                if (!KnownValues.TryGetValue(name, out var values))
                {
                    return [];
                }

                return values
                    .Where(v => v.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .Select(v => new CompletionResult(
                        v,
                        v,
                        CompletionResultType.ParameterValue,
                        v));
            }
            catch
            {
                return [];
            }
        }
    }
}
