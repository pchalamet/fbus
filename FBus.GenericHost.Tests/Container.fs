module FBus.Hosting.Tests
open NUnit.Framework
open FsUnit

open FBus
open Microsoft.Extensions.DependencyInjection
open System


type InMemoryMessage1 =
    { Content1: string } 
    interface FBus.IMessageEvent

type InMemoryMessage2 =
    { Content2: string } 
    interface FBus.IMessageCommand

type IHandlerInvoked =
    abstract HasBeenInvoked: unit -> unit

type IScopedDependencyDisposed =
    abstract HasBeenDisposed: unit -> unit

type ScopedDependency(disposeInvoked: IScopedDependencyDisposed) =
    interface IDisposable with
        member this.Dispose() =
            disposeInvoked.HasBeenDisposed()

type SingletonDependency(disposeInvoked: IScopedDependencyDisposed) =
    interface IDisposable with
        member this.Dispose() =
            disposeInvoked.HasBeenDisposed()

type InMemoryHandler1(handlerInvoked: IHandlerInvoked, scopedDependency: ScopedDependency, singletonDependency: SingletonDependency) =
    interface FBus.IBusConsumer<InMemoryMessage1> with
        member this.Handle ctx msg = 
            { Content2 = msg.Content1 } |> ctx.Send "InMemoryHandler2"
            handlerInvoked.HasBeenInvoked()

type InMemoryHandler2(handlerInvoked: IHandlerInvoked, scopedDependency: ScopedDependency) =
    interface FBus.IBusConsumer<InMemoryMessage2> with
        member this.Handle ctx msg = 
            handlerInvoked.HasBeenInvoked()

let startServer<'t> (session: FBus.Testing.Session) name callback callbackdispose =
    let handledInvoked = {
        new IHandlerInvoked with
            member this.HasBeenInvoked(): unit = 
                printfn "Handler invoked on server [%s]" name
                callback()
    }

    let disposeInvoked = {
        new IScopedDependencyDisposed with
            member this.HasBeenDisposed(): unit = 
                printfn "Handler invoked on server [%s]" name
                callbackdispose()
    }

    let checkErrorHook = {
        new IBusHook with
            member _.OnStart initiator = ()
            member _.OnStop initiator = ()

            member _.OnBeforeProcessing ctx = null

            member _.OnError ctx msg exn =
                failwithf "No error shall be raised: %A" exn
    }

    let svcCollection = ServiceCollection() :> IServiceCollection
    svcCollection.AddSingleton(handledInvoked) |> ignore
    svcCollection.AddSingleton(disposeInvoked) |> ignore
    svcCollection.AddScoped<ScopedDependency>() |> ignore
    svcCollection.AddSingleton<SingletonDependency>() |> ignore
    let serverBus = Builder.configure() |> session.Use
                                        |> Builder.withName name
                                        |> Builder.withContainer (FBus.Containers.GenericHost(svcCollection))
                                        |> Builder.withConsumer<'t>
                                        |> Builder.withHook checkErrorHook
                                        |> Builder.build
    svcCollection.BuildServiceProvider() |> serverBus.Start |> ignore
    serverBus

[<Test>]
let ``check inmemory message exchange`` () =
    let session = FBus.Testing.Session()

    let mutable serverHasBeenInvoked1 = 0
    let mutable serverHasBeenInvoked2 = 0
    let mutable serverHasBeenDisposed1 = 0
    let mutable serverHasBeenDisposed2 = 0
    use bus1 = startServer<InMemoryHandler1> session "InMemoryHandler1" (fun () -> serverHasBeenInvoked1 <- serverHasBeenInvoked1 + 1) (fun () -> serverHasBeenDisposed1 <- serverHasBeenDisposed1 + 1)
    use bus2 = startServer<InMemoryHandler2> session "InMemoryHandler2" (fun () -> serverHasBeenInvoked2 <- serverHasBeenInvoked2 + 1) (fun () -> serverHasBeenDisposed2 <- serverHasBeenDisposed2 + 1)

    use clientBus = FBus.Builder.configure() |> session.Use |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish
    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    session.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal 2
    serverHasBeenInvoked2 |> should equal 2
    serverHasBeenDisposed1 |> should equal 2
    serverHasBeenDisposed2 |> should equal 2
