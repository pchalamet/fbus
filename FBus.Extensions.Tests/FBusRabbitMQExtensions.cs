namespace FBus.Extensions.Tests;

using System;
using FBus;
using FBus.Extensions;

public class FBusRabbitMQExtensionsTests
{

    [Test]
    public void CheckCompile()
    {
        var busControl = FBus.Builder.configure()
                             .UseRabbitMQDefaults()
                             .UseRabbitMQWith(new Uri("http://toto.com"))
                             .Build();

        Assert.Pass();
    }
}
