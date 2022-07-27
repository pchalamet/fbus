namespace FBus.Extensions.Tests;

using System;
using FBus;
using FBus.Extensions;

public record FakeCommand : IMessageCommand;
public record FakeEvent : IMessageEvent;

public class FakeConsumer : IBusConsumer<FakeCommand>, IBusConsumer<FakeEvent>
{
    void IBusConsumer<FakeCommand>.Handle(IBusConversation value, FakeCommand msg)
    {
        throw new NotImplementedException();
    }

    void IBusConsumer<FakeEvent>.Handle(IBusConversation value, FakeEvent msg)
    {
        throw new NotImplementedException();
    }
}

public class FakeHook : IBusHook
{
    public IDisposable OnBeforeProcessing(IBusConversation ctx)
    {
        throw new NotImplementedException();
    }

    public void OnError(IBusConversation ctx, object msg, Exception exn)
    {
        throw new NotImplementedException();
    }

    public void OnStart(IBusInitiator ctx)
    {
        throw new NotImplementedException();
    }

    public void OnStop(IBusInitiator ctx)
    {
        throw new NotImplementedException();
    }
}

public class FBusExtensionsTests
{
    [Test]
    public void CheckCompile()
    {
        var busBuilder = FBus.Builder.Configure()
                             .WithName("toto")
                             .WithShard("titi")
                             .WithConsumer<FakeConsumer>()
                             .WithConsumer<string>(handler)
                             .WithRecovery()
                             .WithHook(new FakeHook());

        Assert.Pass();

        void handler(IBusConversation conversation, string msg)
        {            
        }
    }
}