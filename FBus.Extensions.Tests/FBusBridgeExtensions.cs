namespace FBus.Extensions.Tests;

using FBus.Extensions;

public class FBusBridgeExtensionsTests
{
    [Test]
    public void CheckCompile()
    {
        var busControl = FBus.Builder.configure()
                             .UseBridgeDefaults();

        Assert.Pass();
    }
}
