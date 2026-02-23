using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;
using PowerCode.Git.Completers;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Lists tags in a git repository (git tag -l).
/// <example>
/// <code>Get-GitTag</code>
/// </example>
/// <example>
/// <code>Get-GitTag -Pattern "v1.*"</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitTag", DefaultParameterSetName = TagParameterSet)]
[OutputType(typeof(GitTagInfo))]
public sealed class GetGitTagCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitTagCmdlet"/> class.
    /// </summary>
    public GetGitTagCmdlet()
        : this(ServiceFactory.CreateGitTagService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGitTagCmdlet"/> class.
    /// </summary>
    /// <param name="tagService">The tag service used by the cmdlet.</param>
    internal GetGitTagCmdlet(IGitTagService tagService)
    {
        this.tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
    }

    private readonly IGitTagService tagService;

    private const string TagParameterSet = "Tag";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets a glob pattern to filter tag names.
    /// </summary>
    [Parameter(ParameterSetName = TagParameterSet)]
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the sort order. Accepts "name" or "version".
    /// </summary>
    [Parameter(ParameterSetName = TagParameterSet)]
    [ValidateSet("name", "version", IgnoreCase = false)]
    public string? SortBy { get; set; }

    /// <summary>
    /// Gets or sets a committish to filter only tags that contain the specified commit.
    /// </summary>
    [Parameter(ParameterSetName = TagParameterSet)]
    [GitCommittishCompleter]
    public string? ContainsCommit { get; set; }

    /// <summary>
    /// Gets or sets pre-built options.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitTagListOptions? Options { get; set; }

    /// <summary>
    /// Builds the <see cref="GitTagListOptions"/> from current parameter values.
    /// </summary>
    internal GitTagListOptions BuildOptions(string currentFileSystemPath)
    {
        if (Options is not null)
        {
            return Options;
        }

        return new GitTagListOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Pattern = Pattern,
            SortBy = SortBy,
            ContainsCommit = ContainsCommit,
        };
    }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        try
        {
            var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);
            var tags = tagService.GetTags(options);

            foreach (var tag in tags)
            {
                WriteObject(tag);
            }
        }
        catch (Exception exception)
        {
            var errorRecord = new ErrorRecord(
                exception,
                "GetGitTagFailed",
                ErrorCategory.InvalidOperation,
                RepoPath);

            WriteError(errorRecord);
        }
    }
}
