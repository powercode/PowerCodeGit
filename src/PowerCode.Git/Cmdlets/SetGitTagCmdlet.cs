using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Creates or replaces a git tag (git tag [-a] [-f] &lt;name&gt; [&lt;target&gt;]).
/// When <see cref="Message"/> is supplied an annotated tag is created; otherwise a lightweight tag
/// is created. Use <see cref="Force"/> to overwrite an existing tag with the same name.
/// <example>
/// <code>Set-GitTag -Name v1.0.0</code>
/// </example>
/// <example>
/// <code>Set-GitTag -Name v2.0.0 -Message "Release v2.0.0"</code>
/// </example>
/// <example>
/// <code>Set-GitTag -Name v1.0.0 -Target abc1234 -Force</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Set, "GitTag", SupportsShouldProcess = true, DefaultParameterSetName = TagParameterSet)]
[OutputType(typeof(GitTagInfo))]
public sealed class SetGitTagCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitTagCmdlet"/> class.
    /// </summary>
    public SetGitTagCmdlet()
        : this(ServiceFactory.CreateGitTagService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SetGitTagCmdlet"/> class.
    /// </summary>
    /// <param name="tagService">The tag service used by the cmdlet.</param>
    internal SetGitTagCmdlet(IGitTagService tagService)
    {
        this.tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
    }

    private readonly IGitTagService tagService;

    private const string TagParameterSet = "Tag";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the tag name.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ParameterSetName = TagParameterSet)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target committish (branch, tag, or SHA) to tag.
    /// Defaults to HEAD when not specified.
    /// </summary>
    [Parameter(Position = 1, ParameterSetName = TagParameterSet)]
    [GitCommittishCompleter]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the annotation message. When provided, creates an annotated tag
    /// (equivalent to <c>git tag -a -m &lt;message&gt;</c>). When omitted, a lightweight
    /// tag is created.
    /// </summary>
    [Parameter(ParameterSetName = TagParameterSet)]
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite an existing tag with the same name.
    /// Equivalent to <c>git tag -f</c>.
    /// </summary>
    [Parameter(ParameterSetName = TagParameterSet)]
    public SwitchParameter Force { get; set; }

    /// <summary>
    /// Gets or sets a pre-built options object for full control over tag creation.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitTagCreateOptions Options { get; set; } = null!;

    /// <summary>
    /// Builds a <see cref="GitTagCreateOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path used to resolve the repository when <see cref="GitCmdlet.RepoPath"/>
    /// is not explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitTagCreateOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitTagCreateOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
            Target = Target,
            Message = Message,
            Force = Force.IsPresent,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
        var tagKind = string.IsNullOrWhiteSpace(options.Message) ? "lightweight tag" : "annotated tag";
        var target = options.Target ?? "HEAD";

        if (!ShouldProcess(options.RepositoryPath, $"Create {tagKind} '{options.Name}' at {target}"))
        {
            return;
        }

        try
        {
            var tag = tagService.CreateTag(options);
            WriteObject(tag);
        }
        catch (Exception exception)
        {
            WriteError(new ErrorRecord(
                exception,
                "SetGitTagFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }
}
