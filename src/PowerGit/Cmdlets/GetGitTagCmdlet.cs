using System;
using System.Management.Automation;
using PowerGit.Abstractions.Models;
using PowerGit.Abstractions.Services;

namespace PowerGit.Cmdlets;

/// <summary>
/// Lists tags in a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitTag")]
[OutputType(typeof(GitTagInfo))]
public sealed class GetGitTagCmdlet : PSCmdlet
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

    /// <summary>
    /// Gets or sets the repository path. Defaults to the current location.
    /// </summary>
    [Parameter]
    public string? Path { get; set; }

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolvePath();

        try
        {
            var tags = tagService.GetTags(repositoryPath);

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
                repositoryPath);

            WriteError(errorRecord);
        }
    }

    /// <summary>
    /// Resolves the repository path from the <see cref="Path"/> parameter or the current location.
    /// </summary>
    /// <param name="currentFileSystemPath">The current PowerShell file system path.</param>
    /// <returns>The resolved repository path.</returns>
    internal string ResolvePath(string? currentFileSystemPath = null)
    {
        if (!string.IsNullOrWhiteSpace(Path))
        {
            return Path!;
        }

        return currentFileSystemPath ?? SessionState.Path.CurrentFileSystemLocation.Path;
    }
}
