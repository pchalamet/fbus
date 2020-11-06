module FBus.Hosting.Tests
open System
open NUnit.Framework
open FsUnit

open FBus
open FBus.Builder
open Microsoft.Extensions.DependencyInjection


type InMemoryMessage1 =
    { Content1: string } 
    interface FBus.IMessageCommand

type InMemoryMessage2 =
    { Content2: string } 
    interface FBus.IMessageCommand

type IHandlerInvoked =
    abstract HasBeenInvoked: unit -> unit

type InMemoryHandler1(handlerInvoked: IHandlerInvoked) =
    interface FBus.IBusConsumer<InMemoryMessage1> with
        member this.Handle ctx msg = 
            { Content2 = msg.Content1 } |> ctx.Send "InMemoryHandler2"
            handlerInvoked.HasBeenInvoked()

type InMemoryHandler2(handlerInvoked: IHandlerInvoked) =
    interface FBus.IBusConsumer<InMemoryMessage2> with
        member this.Handle ctx msg = 
            handlerInvoked.HasBeenInvoked()

let startServer<'t> name callback =
    let handledInvoked = {
        new IHandlerInvoked with
            member this.HasBeenInvoked(): unit = 
                printfn "Handler invoked on server [%s]" name
                callback()
    }

    let checkErrorHook = {
        new IBusHook with
            member this.OnError ctx msg exn =
                failwithf "No error shall be raised: %A" exn
    }

    let svcCollection = ServiceCollection() :> IServiceCollection
    svcCollection.AddSingleton(handledInvoked) |> ignore
    let serverBus = FBus.Builder.init() |> withName name
                                        |> withContainer (FBus.Hosting.AspNetCoreContainer(svcCollection))
                                        |> FBus.InMemory.useTransport
                                        |> FBus.InMemory.useSerializer
                                        |> FBus.InMemory.useContainer
                                        |> withConsumer<'t>
                                        |> withHook checkErrorHook
                                        |> FBus.Builder.build
    svcCollection.BuildServiceProvider() |> serverBus.Start |> ignore
    serverBus

[<Test>]
let ``check inmemory message exchange`` () =
    let mutable serverHasBeenInvoked1 = false
    let mutable serverHasBeenInvoked2 = false
    use bus1 = startServer<InMemoryHandler1> "InMemoryHandler1" (fun () -> serverHasBeenInvoked1 <- true)
    use bus2 = startServer<InMemoryHandler2> "InMemoryHandler2" (fun () -> serverHasBeenInvoked2 <- true)

    use clientBus = FBus.Builder.init() |> FBus.InMemory.useTransport
                                        |> FBus.InMemory.useSerializer
                                        |> FBus.InMemory.useContainer
                                        |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    FBus.InMemory.Transport.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal true
    serverHasBeenInvoked2 |> should equal true

[<Test>]
let ``check inmemory message exchange again`` () =
    let mutable serverHasBeenInvoked1 = false
    let mutable serverHasBeenInvoked2 = false
    use bus1 = startServer<InMemoryHandler1> "InMemoryHandler1" (fun () -> serverHasBeenInvoked1 <- true)
    use bus2 = startServer<InMemoryHandler2> "InMemoryHandler2" (fun () -> serverHasBeenInvoked2 <- true)

    use clientBus = FBus.Builder.init() |> FBus.InMemory.useTransport 
                                        |> FBus.InMemory.useSerializer
                                        |> FBus.InMemory.useContainer
                                        |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    FBus.InMemory.Transport.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal true
    serverHasBeenInvoked2 |> should equal true



