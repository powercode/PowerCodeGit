using System.Linq;
using System.Management.Automation;
using PowerCode.Git.Cmdlets;

namespace PowerCode.Git.Tests.Cmdlets;

[TestClass]
public sealed class InvokeGitRepositoryCmdletTests
{
    [TestMethod]
    public void Cmdlet_HasInvokeVerb_AndGitRepositoryNoun()
    {
        var attr = typeof(InvokeGitRepositoryCmdlet)
            .GetCustomAttributes(typeof(CmdletAttribute), inherit: false)
            .Cast<CmdletAttribute>()
            .Single();

        Assert.AreEqual(VerbsLifecycle.Invoke, attr.VerbName);
        Assert.AreEqual("GitRepository", attr.NounName);
    }

    [TestMethod]
    public void Cmdlet_ExtendsGitCmdlet()
    {
        Assert.IsTrue(typeof(GitCmdlet).IsAssignableFrom(typeof(InvokeGitRepositoryCmdlet)));
    }

    [TestMethod]
    public void Action_Parameter_IsMandatory()
    {
        var paramAttr = typeof(InvokeGitRepositoryCmdlet)
            .GetProperty(nameof(InvokeGitRepositoryCmdlet.Action))!
            .GetCustomAttributes(typeof(ParameterAttribute), inherit: false)
            .Cast<ParameterAttribute>()
            .Single();

        Assert.IsTrue(paramAttr.Mandatory);
    }

    [TestMethod]
    public void Action_Parameter_IsAtPosition0()
    {
        var paramAttr = typeof(InvokeGitRepositoryCmdlet)
            .GetProperty(nameof(InvokeGitRepositoryCmdlet.Action))!
            .GetCustomAttributes(typeof(ParameterAttribute), inherit: false)
            .Cast<ParameterAttribute>()
            .Single();

        Assert.AreEqual(0, paramAttr.Position);
    }

    [TestMethod]
    public void Cmdlet_HasOutputType_OfPSObject()
    {
        var outputAttr = typeof(InvokeGitRepositoryCmdlet)
            .GetCustomAttributes(typeof(OutputTypeAttribute), inherit: false)
            .Cast<OutputTypeAttribute>()
            .Single();

        Assert.IsTrue(outputAttr.Type.Any(t => t.Type == typeof(PSObject)));
    }
}
