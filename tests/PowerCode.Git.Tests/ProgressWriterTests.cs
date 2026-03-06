using System.Collections.Generic;
using System.Management.Automation;

namespace PowerCode.Git.Tests;

[TestClass]
public sealed class ProgressWriterTests
{
    [TestMethod]
    public void AsCallback_InvokesWriteProgress_WithCorrectRecord()
    {
        var records = new List<ProgressRecord>();
        using var writer = new ProgressWriter(records.Add, 1, "TestActivity");

        var callback = writer.AsCallback();
        callback(42, "receiving objects");

        Assert.HasCount(1, records);
        Assert.AreEqual(1, records[0].ActivityId);
        Assert.AreEqual("TestActivity", records[0].Activity);
        Assert.AreEqual("receiving objects", records[0].StatusDescription);
        Assert.AreEqual(42, records[0].PercentComplete);
        Assert.AreEqual(ProgressRecordType.Processing, records[0].RecordType);
    }

    [TestMethod]
    public void Dispose_WritesCompletedRecord()
    {
        var records = new List<ProgressRecord>();
        var writer = new ProgressWriter(records.Add, 1, "TestActivity");

        writer.Dispose();

        Assert.HasCount(1, records);
        Assert.AreEqual(ProgressRecordType.Completed, records[0].RecordType);
        Assert.AreEqual(1, records[0].ActivityId);
        Assert.AreEqual("TestActivity", records[0].Activity);
    }

    [TestMethod]
    public void Dispose_IsIdempotent_WritesOnlyOneCompletedRecord()
    {
        var records = new List<ProgressRecord>();
        var writer = new ProgressWriter(records.Add, 1, "TestActivity");

        writer.Dispose();
        writer.Dispose();

        Assert.HasCount(1, records);
    }

    [TestMethod]
    public void AsCallback_ThenDispose_WritesProgressThenCompleted()
    {
        var records = new List<ProgressRecord>();
        var writer = new ProgressWriter(records.Add, 1, "TestActivity");
        var callback = writer.AsCallback();

        callback(50, "halfway");
        writer.Dispose();

        Assert.HasCount(2, records);
        Assert.AreEqual(ProgressRecordType.Processing, records[0].RecordType);
        Assert.AreEqual(ProgressRecordType.Completed, records[1].RecordType);
    }
}
