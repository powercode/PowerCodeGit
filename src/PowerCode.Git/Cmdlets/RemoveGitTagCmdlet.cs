using System;
using System.Management.Automation;
using PowerCode.Git.Abstractions.Models;
using PowerCode.Git.Abstractions.Services;

namespace PowerCode.Git.Cmdlets;

/// <summary>
/// Deletes a git tag (git tag -d).
/// <example>
/// <code>Remove-GitTag -Name v1.0.0</code>
/// </example>
/// </summary>
[Cmdlet(VerbsCommon.Remove, "GitTag", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium, DefaultParameterSetName = DeleteParameterSet)]
public sealed class RemoveGitTagCmdlet : GitCmdlet
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitTagCmdlet"/> class.
    /// </summary>
    public RemoveGitTagCmdlet()
        : this(ServiceFactory.CreateGitTagService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveGitTagCmdlet"/> class.
    /// </summary>
    /// <param name="tagService">The tag service used by the cmdlet.</param>
    internal RemoveGitTagCmdlet(IGitTagService tagService)
    {
        this.tagService = tagService ?? throw new ArgumentNullException(nameof(tagService));
    }

    private readonly IGitTagService tagService;

    private const string DeleteParameterSet = "Delete";
    private const string OptionsParameterSet = "Options";

    /// <summary>
    /// Gets or sets the name of the tag to delete.
    /// </summary>
    [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true, ParameterSetName = DeleteParameterSet)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a pre-built options object for full control over tag deletion.
    /// </summary>
    [Parameter(Mandatory = true, ParameterSetName = OptionsParameterSet)]
    public GitTagDeleteOptions Options { get; set; } = null!;

    /// <summary>
    /// Executes the cmdlet operation.
    /// </summary>
    protected override void ProcessRecord()
    {
        var options = BuildOptions(SessionState.Path.CurrentFileSystemLocation.Path);

        if (!ShouldProcess(options.RepositoryPath, $"Delete tag '{options.Name}'"))
        {
            return;
        }

        try
        {
            tagService.DeleteTag(options);
        }
        catch (Exception exception) when (exception is not PipelineStoppedException)
        {
            WriteError(new ErrorRecord(
                exception,
                "RemoveGitTagFailed",
                ErrorCategory.InvalidOperation,
                RepoPath));
        }
    }

    /// <summary>
    /// Builds a <see cref="GitTagDeleteOptions"/> from the current cmdlet parameters.
    /// </summary>
    /// <param name="currentFileSystemPath">
    /// The current file-system path, used to resolve <see cref="GitCmdlet.RepoPath"/> when not
    /// explicitly provided.
    /// </param>
    /// <returns>The resolved options object.</returns>
    internal GitTagDeleteOptions BuildOptions(string currentFileSystemPath)
    {
        if (ParameterSetName == OptionsParameterSet)
        {
            return Options;
        }

        return new GitTagDeleteOptions
        {
            RepositoryPath = ResolveRepositoryPath(currentFileSystemPath),
            Name = Name,
        };
    }
}
