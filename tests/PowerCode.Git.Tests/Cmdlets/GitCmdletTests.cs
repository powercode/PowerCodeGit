using System.Management.Automation;
using System.Reflection;
using PowerCode.Git.Cmdlets;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class GitCmdletTests
{
    /// <summary>
    /// Provides control-flow exceptions and ordinary failures with supported wrappers.
    /// </summary>
    public static IEnumerable<(Exception Exception, bool Expected)> ExceptionCases()
    {
        (Exception Exception, bool Expected)[] cases =
        [
            (CreateEngineException(nameof(BreakException)), true),
            (CreateEngineException(nameof(ContinueException)), true),
            (CreateEngineException("ReturnException", null), true),
            (CreateEngineException(nameof(ExitException)), true),
            (new TerminateException(), true),
            (new PipelineStoppedException(), true),
            (new ActionPreferenceStopException(), true),
            (new HaltCommandException(), true),
            (new InvalidOperationException("ordinary failure"), false),
            (new ArgumentException("invalid argument"), false),
            (new RuntimeException("script failure"), false),
            (new PSInvalidOperationException("PowerShell failure"), false),
            (new TargetInvocationException("no inner exception", null), false),
            (new OperationCanceledException(), false),
        ];

        foreach (var (exception, expected) in cases)
        {
            yield return (exception, expected);
            yield return (new TargetInvocationException(exception), expected);
            yield return (new RuntimeException("runtime wrapper", exception), expected);
            yield return (new RuntimeException("nested wrapper",
                new TargetInvocationException(new RuntimeException("inner wrapper", exception))), expected);
            yield return (new TargetInvocationException(
                new RuntimeException("runtime wrapper", new TargetInvocationException(exception))), expected);
        }
    }

    /// <summary>
    /// Verifies the exception families and wrapper semantics used by catch filters.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ExceptionCases))]
    public void IsPowerShellControlFlowException_ExceptionKindsAndWrappers_ReturnsExpectedResult(
        Exception exception, bool expected)
    {
        Assert.AreEqual(expected, GitCmdlet.IsPowerShellControlFlowException(exception));
    }

    /// <summary>
    /// Ensures arbitrary inner exceptions are not treated as transparent wrappers.
    /// </summary>
    [TestMethod]
    public void IsPowerShellControlFlowException_UnsupportedWrappers_ReturnsFalse()
    {
        var controlFlow = new PipelineStoppedException();

        Assert.IsFalse(GitCmdlet.IsPowerShellControlFlowException(
            new InvalidOperationException("ordinary failure", controlFlow)));
        Assert.IsFalse(GitCmdlet.IsPowerShellControlFlowException(new AggregateException(controlFlow)));
        Assert.IsFalse(GitCmdlet.IsPowerShellControlFlowException(
            new RuntimeException("runtime wrapper",
                new InvalidOperationException("ordinary failure", controlFlow))));
    }

    /// <summary>
    /// Verifies the helper rejects null rather than hiding a caller error.
    /// </summary>
    [TestMethod]
    public void IsPowerShellControlFlowException_NullException_ThrowsArgumentNullException()
    {
        var exception = Assert.ThrowsExactly<ArgumentNullException>(
            () => GitCmdlet.IsPowerShellControlFlowException(null!));

        Assert.AreEqual("exception", exception.ParamName);
    }

    /// <summary>
    /// Ensures deeply nested wrappers are inspected without recursive stack growth.
    /// </summary>
    [TestMethod]
    public void IsPowerShellControlFlowException_DeeplyNestedWrappers_ReturnsTrue()
    {
        Exception exception = new PipelineStoppedException();
        for (var index = 0; index < 1000; index++)
        {
            exception = new RuntimeException("wrapper", new TargetInvocationException(exception));
        }

        Assert.IsTrue(GitCmdlet.IsPowerShellControlFlowException(exception));
    }

    /// <summary>
    /// Creates test fixtures for engine exceptions with no public constructors.
    /// </summary>
    private static Exception CreateEngineException(string typeName, object? argument = null)
    {
        var type = typeof(PSCmdlet).Assembly.GetType($"System.Management.Automation.{typeName}", throwOnError: true)!;
        var arguments = typeName == "ReturnException" ? new[] { argument } : Array.Empty<object?>();

        return Assert.IsInstanceOfType<Exception>(Activator.CreateInstance(
            type, BindingFlags.Instance | BindingFlags.NonPublic, binder: null, args: arguments, culture: null));
    }
}
