namespace FBus.Extensions.Tests;

using System;
using FBus.Extensions;

public class FBusRabbitMQExtensionsTests
{
    [Test]
    public void CheckCompile()
    {
        var busControl = FBus.Builder.Configure()
                             .UseRabbitMQ()
                             .UseRabbitMQ(new Uri("http://toto.com"));

        Assert.Pass();
    }
}
