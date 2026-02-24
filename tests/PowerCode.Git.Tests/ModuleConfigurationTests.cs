namespace PowerCode.Git.Tests;

[TestClass]
public sealed class ModuleConfigurationTests
{
    [TestInitialize]
    public void ResetConfiguration()
    {
        ModuleConfiguration.Current.Reset();
    }

    [TestMethod]
    public void Current_ReturnsSameInstance()
    {
        var first = ModuleConfiguration.Current;
        var second = ModuleConfiguration.Current;

        Assert.AreSame(first, second);
    }

    [TestMethod]
    public void LogMaxCount_DefaultsToNull()
    {
        Assert.IsNull(ModuleConfiguration.Current.LogMaxCount);
    }

    [TestMethod]
    public void DiffContext_DefaultsToNull()
    {
        Assert.IsNull(ModuleConfiguration.Current.DiffContext);
    }

    [TestMethod]
    public void BranchReferenceBranch_DefaultsToNull()
    {
        Assert.IsNull(ModuleConfiguration.Current.BranchReferenceBranch);
    }

    [TestMethod]
    public void LogMaxCount_SetAndGet_RetainsValue()
    {
        ModuleConfiguration.Current.LogMaxCount = 42;

        Assert.AreEqual(42, ModuleConfiguration.Current.LogMaxCount);
    }

    [TestMethod]
    public void DiffContext_SetAndGet_RetainsValue()
    {
        ModuleConfiguration.Current.DiffContext = 10;

        Assert.AreEqual(10, ModuleConfiguration.Current.DiffContext);
    }

    [TestMethod]
    public void BranchReferenceBranch_SetAndGet_RetainsValue()
    {
        ModuleConfiguration.Current.BranchReferenceBranch = "origin/main";

        Assert.AreEqual("origin/main", ModuleConfiguration.Current.BranchReferenceBranch);
    }

    [TestMethod]
    public void Reset_ClearsAllValues()
    {
        var config = ModuleConfiguration.Current;
        config.LogMaxCount = 100;
        config.DiffContext = 5;
        config.BranchReferenceBranch = "origin/main";

        config.Reset();

        Assert.IsNull(config.LogMaxCount);
        Assert.IsNull(config.DiffContext);
        Assert.IsNull(config.BranchReferenceBranch);
    }
}
