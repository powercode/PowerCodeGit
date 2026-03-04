using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Rewrites git repository history using PowerShell ScriptBlock filters, offering a
/// strongly-typed alternative to <c>git filter-branch</c> and <c>git filter-repo</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Warning:</b> history rewriting changes commit SHAs for every rewritten commit and
/// all its descendants. If the repository has already been pushed to a remote, all
/// collaborators must re-clone or perform a <c>git pull --rebase</c>. Use <c>-WhatIf</c>
/// to preview what would change before committing to a rewrite.
/// </para>
/// <para>
/// Backup refs are created under <see cref="BackupNamespace"/> (default:
/// <c>refs/original/</c>) before any rewriting begins. The rewrite can be
/// undone by resetting branches to the backup refs.
/// </para>
/// <para>
/// At least one of <see cref="CommitFilter"/>, <see cref="TreeFilter"/>,
/// <see cref="ParentsRewriter"/>, or <see cref="TagNameRewriter"/> must be provided.
/// </para>
/// </remarks>
[Cmdlet(VerbsData.Edit, "GitHistory", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(GitRewrittenCommitInfo))]
public sealed class EditGitHistoryCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditGitHistoryCmdlet"/> class.
    /// </summary>
    public EditGitHistoryCmdlet()
        : this(ServiceFactory.CreateGitHistoryRewriteService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EditGitHistoryCmdlet"/> class
    /// with an explicit service (used in unit tests).
    /// </summary>
    /// <param name="historyRewriteService">The history rewrite service to use.</param>
    internal EditGitHistoryCmdlet(IGitHistoryRewriteService historyRewriteService)
    {
        this.historyRewriteService = historyRewriteService
            ?? throw new ArgumentNullException(nameof(historyRewriteService));
    }

    private readonly IGitHistoryRewriteService historyRewriteService;
    private CancellationTokenSource? cts;
    private CancellationToken cancellationToken;

    // ─── Filter parameters ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets a ScriptBlock to rewrite commit metadata (author, committer, message).
    /// </summary>
    /// <remarks>
    /// The ScriptBlock receives the commit as <c>$args[0]</c> and as the <c>$commit</c>
    /// variable. It should return:
    /// <list type="bullet">
    ///   <item><description>
    ///     <c>$null</c> — keep the commit metadata unchanged.
    ///   </description></item>
    ///   <item><description>
    ///     A hashtable with any combination of these keys:
    ///     <c>Message</c> (string),
    ///     <c>Author</c> (a <c>LibGit2Sharp.Signature</c> or a hashtable
    ///     <c>@{Name=''; Email=''; When=[DateTimeOffset]}</c>),
    ///     <c>Committer</c> (same shape as Author).
    ///   </description></item>
    /// </list>
    /// </remarks>
    /// <example>
    ///   <code>
    ///     Edit-GitHistory -CommitFilter {
    ///         if ($commit.Author.Email -eq 'wrong@old.com') {
    ///             @{ Author = @{ Name = $commit.Author.Name; Email = 'correct@new.com'; When = $commit.Author.When } }
    ///         }
    ///     } -Force
    ///   </code>
    /// </example>
    [Parameter]
    [GitScriptBlockCompleter]
    public ScriptBlock? CommitFilter { get; set; }

    /// <summary>
    /// Gets or sets a ScriptBlock to filter tree entries from each commit's tree.
    /// </summary>
    /// <remarks>
    /// The ScriptBlock receives a context object as <c>$args[0]</c> and as <c>$_</c>
    /// with the following properties:
    /// <list type="bullet">
    ///   <item><description><c>Path</c> — full repository-relative path (e.g. <c>src/secret.xml</c>).</description></item>
    ///   <item><description><c>Mode</c> — git file mode string (e.g. <c>NonExecutableFile</c>).</description></item>
    ///   <item><description><c>ObjectId</c> — the blob's SHA hash.</description></item>
    /// </list>
    /// Return <see langword="$true"/> to keep the entry, <see langword="$false"/> to remove it.
    /// Combine with <see cref="PruneEmptyCommits"/> to discard commits that become empty after filtering.
    /// </remarks>
    /// <example>
    ///   <code>
    ///     Edit-GitHistory -TreeFilter { -not ($_.Path -like '*.zip' -or $_.Path -like '*.exe') } -Force
    ///   </code>
    /// </example>
    [Parameter]
    [GitScriptBlockCompleter]
    public ScriptBlock? TreeFilter { get; set; }

    /// <summary>
    /// Gets or sets a ScriptBlock to rewrite the parent links of each commit.
    /// </summary>
    /// <remarks>
    /// The ScriptBlock receives the commit as <c>$args[0]</c> and as the <c>$commit</c>
    /// variable. Return <see langword="$null"/> to keep the original parents, or an
    /// array of commit SHA strings (or objects with a <c>Sha</c> property) to replace them.
    /// </remarks>
    [Parameter]
    [GitScriptBlockCompleter]
    public ScriptBlock? ParentsRewriter { get; set; }

    /// <summary>
    /// Gets or sets a ScriptBlock to rename tags that point to rewritten commits.
    /// </summary>
    /// <remarks>
    /// The ScriptBlock receives three positional arguments:
    /// <c>$args[0]</c> — old tag name,
    /// <c>$args[1]</c> — whether the tag is annotated (<see cref="bool"/>),
    /// <c>$args[2]</c> — old target identifier (SHA for direct refs, canonical name for symbolic refs).
    /// Return the new tag name as a string, or <see langword="$null"/> to keep the original.
    /// </remarks>
    [Parameter]
    [GitScriptBlockCompleter]
    public ScriptBlock? TagNameRewriter { get; set; }

    // ─── Control parameters ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the refs whose reachable commits should be rewritten.
    /// Accepts branch names, full ref names (<c>refs/heads/…</c>), tags, and commit SHAs.
    /// When omitted, all local branches are targeted.
    /// </summary>
    [Parameter]
    [GitCommittishCompleter(IncludeBranches = true, IncludeTags = true)]
    public string[]? Refs { get; set; }

    /// <summary>
    /// Gets or sets the namespace used for backup refs created before rewriting.
    /// Defaults to <c>refs/original/</c>.
    /// </summary>
    [Parameter]
    public string BackupNamespace { get; set; } = "refs/original/";

    /// <summary>
    /// Gets or sets a value that removes commits whose tree is empty after
    /// <see cref="TreeFilter"/> removes all their file entries.
    /// </summary>
    [Parameter]
    public SwitchParameter PruneEmptyCommits { get; set; }

    /// <summary>
    /// Gets or sets a value that confirms execution of the destructive history rewrite.
    /// Without <c>-Force</c>, the cmdlet reports an error unless <c>-WhatIf</c> is also specified.
    /// </summary>
    [Parameter]
    public SwitchParameter Force { get; set; }

    // ─── Lifecycle ──────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void BeginProcessing()
    {
        base.BeginProcessing();

        if (CommitFilter is null && TreeFilter is null && ParentsRewriter is null && TagNameRewriter is null)
        {
            ThrowTerminatingError(new ErrorRecord(
                new ArgumentException(
                    "At least one filter parameter must be specified: -CommitFilter, -TreeFilter, -ParentsRewriter, or -TagNameRewriter."),
                "NoFilterSpecified",
                ErrorCategory.InvalidArgument,
                null));
        }
    }

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        cts = new CancellationTokenSource();
        cancellationToken = cts.Token;

        try
        {
            var cwd = SessionState.Path.CurrentFileSystemLocation.Path;

            // Detect -WhatIf by checking whether it was explicitly bound.
            // SupportsShouldProcess exposes -WhatIf as a common parameter, so it
            // appears in BoundParameters when the user specifies it.
            var isWhatIf = MyInvocation.BoundParameters.ContainsKey("WhatIf");

            if (isWhatIf)
            {
                // Dry-run: simulate the rewrite and emit the per-commit report without
                // modifying the repository. This gives richer output than PowerShell's
                // default "What if: Performing the operation…" message.
                WriteVerbose("WhatIf: running dry-run simulation only — repository will not be modified.");
                var dryRunOptions = BuildOptions(cwd, dryRun: true);
                RunService(dryRunOptions);
            }
            else if (!Force.IsPresent)
            {
                // Neither -Force nor -WhatIf: refuse to execute.
                ThrowTerminatingError(new ErrorRecord(
                    new RuntimeException(
                        "Edit-GitHistory permanently rewrites repository history and changes commit SHAs. " +
                        "Specify -Force to execute (backup refs are created automatically), " +
                        "or use -WhatIf to preview what would be changed without modifying the repository."),
                    "ForceRequired",
                    ErrorCategory.OperationStopped,
                    null));
            }
            else
            {
                // -Force is present: use ShouldProcess (ConfirmImpact.High causes a prompt unless
                // -Confirm:$false is passed). The prompt lists the backup namespace so the user
                // knows how to recover.
                var repoPath = ResolveRepositoryPath(cwd);
                if (!ShouldProcess(
                    repoPath,
                    $"Rewrite git history (backup refs → {BackupNamespace})"))
                {
                    return;
                }

                var options = BuildOptions(cwd, dryRun: false);
                RunService(options);
            }
        }
        catch (OperationCanceledException) { }
        catch (ArgumentException exception)
        {
            WriteError(new ErrorRecord(
                exception, "EditGitHistoryInvalidArgument",
                ErrorCategory.InvalidArgument, RepoPath));
        }
        catch (Exception exception) when (exception is not PipelineStoppedException
                                       && exception is not TerminateException)
        {
            WriteError(new ErrorRecord(
                exception, "EditGitHistoryFailed",
                ErrorCategory.InvalidOperation, RepoPath));
        }
        finally
        {
            cts?.Dispose();
            cts = null;
        }
    }

    /// <inheritdoc/>
    protected override void StopProcessing()
    {
        // Called on a separate thread when the pipeline is stopped (e.g. Ctrl+C).
        // Signals the CancellationToken so the service checks ThrowIfCancellationRequested()
        // at the next commit boundary and terminates cleanly.
        cts?.Cancel();
    }

    // ─── Options builder ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds <see cref="GitHistoryRewriteOptions"/> from the current parameter values.
    /// </summary>
    internal GitHistoryRewriteOptions BuildOptions(string currentFileSystemPath, bool dryRun = false)
    {
        return new GitHistoryRewriteOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Refs = Refs,
            BackupRefsNamespace = BackupNamespace,
            PruneEmptyCommits = PruneEmptyCommits.IsPresent,
            DryRun = dryRun,
        };
    }

    // ─── Service invocation ─────────────────────────────────────────────────────

    private void RunService(GitHistoryRewriteOptions options)
    {
        var commitFilterDelegate = BuildCommitFilterDelegate();
        var treeFilterDelegate = TreeFilter?.ToPredicate("entry");
        var parentsRewriterDelegate = ParentsRewriter?.ToParentsRewriter();
        var tagNameRewriterDelegate = TagNameRewriter?.ToTagNameRewriter();

        var results = historyRewriteService.Rewrite(
            options,
            commitFilterDelegate,
            treeFilterDelegate,
            parentsRewriterDelegate,
            tagNameRewriterDelegate,
            cancellationToken);

        foreach (var result in results)
        {
            WriteObject(result);
        }
    }

    // ─── Delegate builders ──────────────────────────────────────────────────────

    /// <summary>
    /// Wraps <see cref="CommitFilter"/> in a <c>Func&lt;object, GitCommitRewriteOverrides?&gt;</c>
    /// delegate. The returned delegate invokes the ScriptBlock and interprets the result as
    /// a set of metadata overrides, returning <see langword="null"/> when the commit should
    /// remain unchanged.
    /// </summary>
    internal Func<object, GitCommitRewriteOverrides?>? BuildCommitFilterDelegate()
    {
        if (CommitFilter is null)
        {
            return null;
        }

        var objectFunc = CommitFilter.ToObjectFunc("commit");
        return commit =>
        {
            var result = objectFunc(commit);
            return ParseCommitOverrides(result);
        };
    }

    // ─── Commit overrides parsing ───────────────────────────────────────────────

    /// <summary>
    /// Interprets the ScriptBlock return value as a <see cref="GitCommitRewriteOverrides"/>.
    /// </summary>
    /// <remarks>
    /// The ScriptBlock may return:
    /// <list type="bullet">
    ///   <item><description>
    ///     <see langword="$null"/> or no output — returns <see langword="null"/>, meaning "keep unchanged".
    ///   </description></item>
    ///   <item><description>
    ///     A <see cref="Hashtable"/> with keys <c>Message</c>, <c>Author</c>, and/or <c>Committer</c>.
    ///   </description></item>
    ///   <item><description>
    ///     A <see cref="PSObject"/> with the same property names.
    ///   </description></item>
    /// </list>
    /// <c>Author</c> and <c>Committer</c> values may themselves be a <see cref="Hashtable"/>
    /// <c>@{Name=''; Email=''; When=…}</c> or any object with <c>Name</c>, <c>Email</c>,
    /// and <c>When</c> properties (e.g. a <c>LibGit2Sharp.Signature</c>).
    /// </remarks>
    internal static GitCommitRewriteOverrides? ParseCommitOverrides(PSObject? result)
    {
        if (result is null)
        {
            return null;
        }

        string? message = null;
        string? authorName = null, authorEmail = null;
        DateTimeOffset? authorWhen = null;
        string? committerName = null, committerEmail = null;
        DateTimeOffset? committerWhen = null;

        if (result.BaseObject is Hashtable ht)
        {
            message = ht["Message"]?.ToString();
            if (ht["Author"] is { } authorValue)
            {
                ExtractSignatureProperties(authorValue, out authorName, out authorEmail, out authorWhen);
            }

            if (ht["Committer"] is { } committerValue)
            {
                ExtractSignatureProperties(committerValue, out committerName, out committerEmail, out committerWhen);
            }
        }
        else
        {
            var psObj = result;
            message = psObj.Properties["Message"]?.Value?.ToString();

            if (psObj.Properties["Author"]?.Value is { } authorValue)
            {
                ExtractSignatureProperties(authorValue, out authorName, out authorEmail, out authorWhen);
            }

            if (psObj.Properties["Committer"]?.Value is { } committerValue)
            {
                ExtractSignatureProperties(committerValue, out committerName, out committerEmail, out committerWhen);
            }
        }

        // Return null when there's nothing to override.
        if (message is null
            && authorName is null && authorEmail is null && authorWhen is null
            && committerName is null && committerEmail is null && committerWhen is null)
        {
            return null;
        }

        return new GitCommitRewriteOverrides
        {
            Message = message,
            AuthorName = authorName,
            AuthorEmail = authorEmail,
            AuthorWhen = authorWhen,
            CommitterName = committerName,
            CommitterEmail = committerEmail,
            CommitterWhen = committerWhen,
        };
    }

    private static void ExtractSignatureProperties(
        object obj,
        out string? name,
        out string? email,
        out DateTimeOffset? when)
    {
        name = null;
        email = null;
        when = null;

        if (obj is Hashtable ht)
        {
            name = ht["Name"]?.ToString();
            email = ht["Email"]?.ToString();
            when = ht["When"] is DateTimeOffset dto ? dto : null;
        }
        else
        {
            // Accept LibGit2Sharp.Signature or any other object with Name/Email/When properties.
            var ps = PSObject.AsPSObject(obj);
            name = ps.Properties["Name"]?.Value?.ToString();
            email = ps.Properties["Email"]?.Value?.ToString();

            if (ps.Properties["When"]?.Value is DateTimeOffset dto)
            {
                when = dto;
            }
        }
    }
}
