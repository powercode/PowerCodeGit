using System;
using System.Management.Automation;

namespace PowerCode.Git;

/// <summary>
/// Wraps <see cref="ProgressRecord"/> writes so that a
/// <see cref="ProgressRecordType.Completed"/> record is always emitted when
/// the instance is disposed, preventing hanging progress bars.
/// </summary>
internal sealed class ProgressWriter : IDisposable
{
    private readonly Action<ProgressRecord> writeProgress;
    private readonly int activityId;
    private readonly string activity;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressWriter"/> class.
    /// </summary>
    /// <param name="writeProgress">
    /// Delegate that writes a <see cref="ProgressRecord"/> to the host,
    /// typically <c>WriteProgress</c> on a <see cref="PSCmdlet"/>.
    /// </param>
    /// <param name="activityId">The activity identifier for the progress bar.</param>
    /// <param name="activity">The activity description shown in the progress bar.</param>
    internal ProgressWriter(Action<ProgressRecord> writeProgress, int activityId, string activity)
    {
        this.writeProgress = writeProgress ?? throw new ArgumentNullException(nameof(writeProgress));
        this.activityId = activityId;
        this.activity = activity;
    }

    /// <summary>
    /// Returns an <see cref="Action{T1,T2}"/> callback compatible with the
    /// <c>onProgress</c> parameter of <c>IGitRemoteService</c> methods.
    /// </summary>
    internal Action<int, string> AsCallback() => (percent, message) =>
    {
        var record = new ProgressRecord(activityId, activity, message)
        {
            PercentComplete = percent,
        };
        writeProgress(record);
    };

    /// <summary>
    /// Writes a <see cref="ProgressRecordType.Completed"/> record to dismiss the progress bar.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        var completedRecord = new ProgressRecord(activityId, activity, "Complete")
        {
            RecordType = ProgressRecordType.Completed,
        };
        writeProgress(completedRecord);
    }
}
