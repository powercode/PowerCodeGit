using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Lists tags in a git repository.
/// </summary>
[Cmdlet(VerbsCommon.Get, "GitTag")]
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

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var repositoryPath = ResolveRepositoryPath();

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
}
