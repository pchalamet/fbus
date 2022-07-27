namespace FBus.Extensions.Tests;

public class FBusQuickStartExtensionsTests
{
    [Test]
    public void CheckCompile()
    {
        var busControl = FBus.QuickStart.Configure().Build();

        Assert.Pass();
    }
}
