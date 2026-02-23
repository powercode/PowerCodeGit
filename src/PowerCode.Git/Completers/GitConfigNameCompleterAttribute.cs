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
        ["user.name"] = "Your full name attached to every commit and tag",
        ["user.email"] = "Your email address attached to every commit and tag",
        ["user.signingkey"] = "GPG key ID for signing commits and tags (used with commit.gpgsign / git tag -s)",

        // ── core ──
        ["core.autocrlf"] = "Cross-platform line-ending conversion: true = LF→CRLF on checkout (Windows); input = CRLF→LF on commit only; false = no conversion",
        ["core.eol"] = "Line ending style written to the working tree for text files (lf, crlf, native)",
        ["core.editor"] = "Editor launched for commit/tag messages and interactive rebase; falls back to $VISUAL, $EDITOR, or vi",
        ["core.pager"] = "Pager for long output such as log and diff; defaults to less; set to empty string to disable paging",
        ["core.excludesfile"] = "Path to a global gitignore file whose patterns are excluded across all repositories on this machine",
        ["core.attributesfile"] = "Path to a global gitattributes file applied to all repositories on this machine",
        ["core.whitespace"] = "Comma-separated whitespace checks to detect/fix: blank-at-eol, blank-at-eof, space-before-tab (on by default); indent-with-non-tab, tab-in-indent, cr-at-eol (opt-in); prefix with - to disable",
        ["core.filemode"] = "Track executable permission changes on files (true, false)",
        ["core.ignorecase"] = "Ignore case differences in file names; set automatically when the repo is created (true, false)",
        ["core.bare"] = "Whether this repository is bare with no working tree (true, false)",
        ["core.logallrefupdates"] = "Record updates to all refs in the reflog (true, false, always)",
        ["core.symlinks"] = "Create symbolic links on checkout when the platform supports them (true, false)",
        ["core.longpaths"] = "Allow paths longer than 260 characters on Windows (true, false)",
        ["core.fsmonitor"] = "Enable a file-system monitor daemon to speed up commands like status (true, false, or hook path)",
        ["core.untrackedcache"] = "Persist the untracked-file cache between status calls for improved performance (true, false)",
        ["core.compression"] = "Zlib compression level for loose objects and pack files (0-9; -1 to use system default)",
        ["core.quotepath"] = "Quote non-ASCII and special characters in file paths displayed by status/diff (true, false)",
        ["core.safecrlf"] = "Guard against irreversible CRLF conversion: true = error, warn = warning, false = silent (true, false, warn)",
        ["core.hooksPath"] = "Directory containing client-side hook scripts instead of the default .git/hooks",

        // ── help ──
        ["help.autocorrect"] = "Tenths of a second Git waits before running an autocorrected command; 0 disables autocorrect, 1 runs immediately",

        // ── init ──
        ["init.defaultBranch"] = "Name of the initial branch created in new repositories (e.g. main, master)",

        // ── push ──
        ["push.default"] = "Which refs to push when no refspec is given (nothing, current, upstream, simple, matching)",
        ["push.autoSetupRemote"] = "Automatically create a remote-tracking branch on first push (true, false)",
        ["push.followTags"] = "Also push annotated tags that are ancestors of pushed commits (true, false)",

        // ── pull ──
        ["pull.rebase"] = "Rebase local commits on top of the fetched branch instead of merging (true, false, interactive, merges)",
        ["pull.ff"] = "Fast-forward behaviour when pulling (true = only FF, false = always merge, only = no merge commit)",

        // ── fetch ──
        ["fetch.prune"] = "Delete remote-tracking branches that no longer exist on the remote during fetch (true, false)",
        ["fetch.pruneTags"] = "Delete local remote-tracking tags that no longer exist on the remote during fetch (true, false)",

        // ── merge ──
        ["merge.ff"] = "Allow fast-forward merges (true = allow, false = always create merge commit, only = refuse non-FF)",
        ["merge.conflictstyle"] = "Conflict marker style written to conflicted files (merge = standard <<<<, diff3 = with ancestor, zdiff3 = compacted)",
        ["merge.tool"] = "GUI or CLI tool launched by git mergetool to resolve conflicts",

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
        ["color.ui"] = "Master switch for colored terminal output: auto = color when writing to a terminal, always = force color codes, never = disable",
        ["color.diff"] = "Colorize diff output (auto, always, never)",
        ["color.status"] = "Colorize status output (auto, always, never)",
        ["color.branch"] = "Colorize branch listing output (auto, always, never)",

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
        ["commit.gpgsign"] = "Sign all commits with the GPG key in user.signingkey by default (true, false)",
        ["commit.template"] = "Path to a file whose contents are used as the initial commit message template in the editor",
        ["commit.verbose"] = "Include the full diff of changes below the commit message in the editor (true, false)",

        // ── status ──
        ["status.showUntrackedFiles"] = "Show untracked files in status (no, normal, all)",
        ["status.submoduleSummary"] = "Show submodule summary in status (true, false)",

        // ── submodule ──
        ["submodule.recurse"] = "Recurse into submodules for fetch/pull/push (true, false)",

        // ── transfer ──
        ["transfer.fsckObjects"] = "Validate objects on transfer (true, false)",

        // ── receive ──
        ["receive.fsckObjects"] = "Validate SHA-1 checksums and object graph integrity on every push; can slow large repositories (true, false)",
        ["receive.denyNonFastForwards"] = "Reject force-pushes that rewrite history on the server (true, false)",
        ["receive.denyDeletes"] = "Prevent deletion of branches or tags via push; branches can only be removed directly on the server (true, false)",
        ["receive.denyCurrentBranch"] = "Deny pushing to the currently checked-out branch on a non-bare repository (refuse, warn, ignore, updateInstead)",
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
