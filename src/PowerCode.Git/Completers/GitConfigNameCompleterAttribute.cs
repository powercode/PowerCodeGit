using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Completers;

/// <summary>
/// Provides argument completions for git configuration key names.
/// </summary>
/// <remarks>
/// Merges a curated dictionary of well-known git configuration keys (with
/// human-readable descriptions shown as tooltips) with keys dynamically
/// discovered from the current repository via <c>git config --list</c>.
/// Static entries take precedence for tooltip descriptions.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class GitConfigNameCompleterAttribute : ArgumentCompleterFactoryAttribute
{
    /// <inheritdoc/>
    public override IArgumentCompleter Create()
    {
        return new ConfigNameCompleter(ServiceFactory.CreateGitConfigService());
    }

    /// <summary>
    /// Well-known git configuration keys mapped to their descriptions.
    /// </summary>
    internal static readonly Dictionary<string, string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        // ── user ──
        ["user.name"] = "Your full name for commit authorship",
        ["user.email"] = "Your email address for commit authorship",
        ["user.signingkey"] = "GPG key ID used for signing commits and tags",

        // ── core ──
        ["core.autocrlf"] = "Convert CRLF line endings on checkout/commit (true, false, input)",
        ["core.eol"] = "Line ending style for text files (lf, crlf, native)",
        ["core.editor"] = "Text editor used by git for commit messages and interactive rebase",
        ["core.pager"] = "Pager program used for terminal output (e.g. less, delta)",
        ["core.excludesfile"] = "Path to a global .gitignore file",
        ["core.attributesfile"] = "Path to a global .gitattributes file",
        ["core.whitespace"] = "Comma-separated list of whitespace error types to detect",
        ["core.filemode"] = "Track file permission changes (true, false)",
        ["core.ignorecase"] = "Ignore case differences in file names (true, false)",
        ["core.bare"] = "Whether this repository is bare (true, false)",
        ["core.logallrefupdates"] = "Log all ref updates in the reflog (true, false, always)",
        ["core.symlinks"] = "Support symbolic links (true, false)",
        ["core.longpaths"] = "Allow paths longer than 260 characters on Windows (true, false)",
        ["core.fsmonitor"] = "Enable file-system monitor for performance (true, false, hook path)",
        ["core.untrackedcache"] = "Cache untracked files for faster status (true, false)",
        ["core.compression"] = "Compression level for objects (0-9, -1 for default)",
        ["core.quotepath"] = "Quote special characters in file paths (true, false)",
        ["core.safecrlf"] = "Warn or error on irreversible CRLF conversions (true, false, warn)",
        ["core.hooksPath"] = "Custom directory for git hooks",

        // ── init ──
        ["init.defaultBranch"] = "Default branch name for new repositories",

        // ── push ──
        ["push.default"] = "Default push behaviour (nothing, current, upstream, simple, matching)",
        ["push.autoSetupRemote"] = "Automatically set up remote tracking on first push (true, false)",
        ["push.followTags"] = "Push annotated tags alongside commits (true, false)",

        // ── pull ──
        ["pull.rebase"] = "Rebase instead of merge on pull (true, false, interactive, merges)",
        ["pull.ff"] = "Fast-forward behaviour for pull (true, false, only)",

        // ── fetch ──
        ["fetch.prune"] = "Remove stale remote-tracking branches on fetch (true, false)",
        ["fetch.pruneTags"] = "Remove stale remote tags on fetch (true, false)",

        // ── merge ──
        ["merge.ff"] = "Fast-forward behaviour for merge (true, false, only)",
        ["merge.conflictstyle"] = "Conflict marker style (merge, diff3, zdiff3)",
        ["merge.tool"] = "Default merge tool",

        // ── rebase ──
        ["rebase.autosquash"] = "Automatically reorder fixup!/squash! commits (true, false)",
        ["rebase.autostash"] = "Stash before rebase and apply after (true, false)",
        ["rebase.updateRefs"] = "Update stacked branch refs during rebase (true, false)",

        // ── diff ──
        ["diff.algorithm"] = "Diff algorithm (patience, minimal, histogram, myers)",
        ["diff.tool"] = "Default diff tool",
        ["diff.colorMoved"] = "Highlight moved lines in diffs (no, default, plain, blocks, zebra, dimmed-zebra)",
        ["diff.colorMovedWS"] = "Whitespace handling for moved-line detection",
        ["diff.renames"] = "Detect renames in diffs (true, false, copies)",

        // ── color ──
        ["color.ui"] = "Enable coloured terminal output (auto, always, never)",
        ["color.diff"] = "Enable coloured diff output (auto, always, never)",
        ["color.status"] = "Enable coloured status output (auto, always, never)",
        ["color.branch"] = "Enable coloured branch output (auto, always, never)",

        // ── credential ──
        ["credential.helper"] = "Credential storage back-end (store, cache, manager)",

        // ── http ──
        ["http.proxy"] = "HTTP/HTTPS proxy URL",
        ["http.sslVerify"] = "Verify SSL certificates (true, false)",
        ["http.postBuffer"] = "Maximum POST buffer size in bytes",

        // ── gc ──
        ["gc.auto"] = "Threshold for automatic garbage collection (0 to disable)",
        ["gc.autoDetach"] = "Run gc in background (true, false)",

        // ── advice ──
        ["advice.pushNonFastForward"] = "Show advice on non-fast-forward push (true, false)",
        ["advice.statusHints"] = "Show hints in status output (true, false)",
        ["advice.detachedHead"] = "Show advice when entering detached HEAD state (true, false)",
        ["advice.addIgnoredFile"] = "Show advice when adding an ignored file (true, false)",

        // ── branch ──
        ["branch.autoSetupMerge"] = "Auto-configure tracking on branch creation (true, false, always)",
        ["branch.autoSetupRebase"] = "Auto-rebase on pull for new branches (never, local, remote, always)",
        ["branch.sort"] = "Default sort order for branch listing",

        // ── tag ──
        ["tag.sort"] = "Default sort order for tag listing",

        // ── log ──
        ["log.date"] = "Default date format for log output (relative, local, iso, short, etc.)",
        ["log.decorate"] = "Show ref names in log output (true, false, short, full, auto)",
        ["log.abbrevCommit"] = "Abbreviate commit hashes in log output (true, false)",

        // ── remote ──
        ["remote.origin.url"] = "URL of the 'origin' remote",
        ["remote.origin.fetch"] = "Refspec for fetching from 'origin'",

        // ── rerere ──
        ["rerere.enabled"] = "Record and reuse merge conflict resolutions (true, false)",
        ["rerere.autoupdate"] = "Auto-stage rerere-resolved files (true, false)",

        // ── commit ──
        ["commit.gpgsign"] = "Sign commits with GPG by default (true, false)",
        ["commit.template"] = "Path to a default commit message template file",
        ["commit.verbose"] = "Show diff in commit message editor (true, false)",

        // ── status ──
        ["status.showUntrackedFiles"] = "Show untracked files in status (no, normal, all)",
        ["status.submoduleSummary"] = "Show submodule summary in status (true, false)",

        // ── submodule ──
        ["submodule.recurse"] = "Recurse into submodules for fetch/pull/push (true, false)",

        // ── transfer ──
        ["transfer.fsckObjects"] = "Validate objects on transfer (true, false)",

        // ── receive ──
        ["receive.denyCurrentBranch"] = "Deny pushing to checked-out branch (refuse, warn, ignore, updateInstead)",
    };

    internal sealed class ConfigNameCompleter(IGitConfigService configService) : IArgumentCompleter
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
                // Start with the static dictionary entries.
                var completions = new Dictionary<string, string>(KnownKeys, StringComparer.OrdinalIgnoreCase);

                // Merge in dynamic entries from the repository (dynamic entries
                // only contribute keys not already in the static dictionary).
                try
                {
                    var repositoryPath = CompletionHelper.ResolveRepositoryPath(fakeBoundParameters);
                    var entries = configService.GetConfigEntries(repositoryPath);

                    foreach (var entry in entries)
                    {
                        completions.TryAdd(entry.Name, $"{entry.Name}={entry.Value}");
                    }
                }
                catch
                {
                    // Dynamic lookup failed — proceed with static entries only.
                }

                return completions
                    .Where(kv => kv.Key.StartsWith(wordToComplete, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => new CompletionResult(
                        kv.Key,
                        kv.Key,
                        CompletionResultType.ParameterValue,
                        kv.Value));
            }
            catch
            {
                return [];
            }
        }
    }
}
