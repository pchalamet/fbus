module FBus.InMemory.Test
open System
open NUnit.Framework
open FsUnit

open FBus
open FBus.Builder
open Microsoft.Extensions.DependencyInjection


type InMemoryMessage = {
    Content: string
}


type IHandlerInvoked =
    abstract HasBeenInvoked: unit -> unit

type InMemoryHandler1(handlerInvoked: IHandlerInvoked) =
    interface FBus.IBusConsumer<InMemoryMessage> with
        member this.Handle ctx msg = 
            ctx.Send "InMemoryHandler2" msg
            handlerInvoked.HasBeenInvoked()

type InMemoryHandler2(handlerInvoked: IHandlerInvoked) =
    interface FBus.IBusConsumer<InMemoryMessage> with
        member this.Handle ctx msg = 
            handlerInvoked.HasBeenInvoked()


let startServer<'t> name callback =
    let handledInvoked = {
        new IHandlerInvoked with
        member this.HasBeenInvoked(): unit = 
            printfn "Handler invoked on server [%s]" name
            callback()
    }

    let svcCollection = ServiceCollection() :> IServiceCollection
    svcCollection.AddSingleton(handledInvoked) |> ignore
    let serverBus = FBus.Builder.init() |> withName name
                                         |> withContainer (FBus.Hosting.AspNetCoreContainer(svcCollection))
                                         |> withTransport FBus.InMemory.Transport.Create
                                         |> withConsumer<'t>
                                         |> FBus.Builder.build
    svcCollection.BuildServiceProvider() |> serverBus.Start |> ignore
    serverBus

[<Test>]
let ``check inmemory message exchange`` () =
    let mutable serverHasBeenInvoked1 = false
    let mutable serverHasBeenInvoked2 = false
    use bus1 = startServer<InMemoryHandler1> "InMemoryHandler1" (fun () -> serverHasBeenInvoked1 <- true)
    use bus2 = startServer<InMemoryHandler2> "InMemoryHandler2" (fun () -> serverHasBeenInvoked2 <- true)

    use clientBus = FBus.Builder.init() |> withTransport FBus.InMemory.Transport.Create |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content = "Hello InMemory" } |> clientInitiator.Send "InMemoryHandler1"

    FBus.InMemory.Transport.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal true
    serverHasBeenInvoked2 |> should equal true

[<Test>]
let ``check inmemory message exchange again`` () =
    let mutable serverHasBeenInvoked1 = false
    let mutable serverHasBeenInvoked2 = false
    use bus1 = startServer<InMemoryHandler1> "InMemoryHandler1" (fun () -> serverHasBeenInvoked1 <- true)
    use bus2 = startServer<InMemoryHandler2> "InMemoryHandler2" (fun () -> serverHasBeenInvoked2 <- true)

    use clientBus = FBus.Builder.init() |> withTransport FBus.InMemory.Transport.Create |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content = "Hello InMemory" } |> clientInitiator.Send "InMemoryHandler1"

    FBus.InMemory.Transport.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal true
    serverHasBeenInvoked2 |> should equal true
