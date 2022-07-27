namespace FBus.Extensions.Tests;

using FBus.Extensions;

public class FBusBridgeExtensionsTests
{
    [Test]
    public void CheckCompile()
    {
        var busBuilder = FBus.Builder.Configure()
                             .UseBridgeDefaults();

        Assert.Pass();
    }
}
