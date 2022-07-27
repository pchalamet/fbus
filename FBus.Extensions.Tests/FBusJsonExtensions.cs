namespace FBus.Extensions.Tests;

using System;
using FBus;
using FBus.Extensions;
using Microsoft.FSharp.Core;
using System.Text.Json;

public class FBusJsonExtensionsTests
{

    [Test]
    public void CheckCompile()
    {
        var busControl = FBus.Builder.configure()
                             .UseJsonDefaults()
                             .UseJsonWith(configure)
                             .Build();

        Assert.Pass();


        void configure(JsonSerializerOptions options)
        {
        }
    }
}
