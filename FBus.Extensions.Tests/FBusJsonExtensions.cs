namespace FBus.Extensions.Tests;

using FBus.Extensions;
using System.Text.Json;

public class FBusJsonExtensionsTests
{
    [Test]
    public void CheckCompile()
    {
        var busControl = FBus.Builder.Configure()
                             .UseJson()
                             .UseJson(configure);

        Assert.Pass();

        void configure(JsonSerializerOptions options)
        {
        }
    }
}
