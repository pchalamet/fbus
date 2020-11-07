module FBus.Transports.Tests
open System
open NUnit.Framework
open FsUnit

open FBus
open FBus.Builder


type InMemoryMessage1 =
    { Content1: string } 
    interface FBus.IMessageCommand

type InMemoryMessage2 =
    { Content2: string } 
    interface FBus.IMessageCommand

type IHandlerInvoked =
    abstract HasBeenInvoked: unit -> unit

type InMemoryHandler1() =
    static member val HandledInvoked: IHandlerInvoked option = None with get, set

    interface FBus.IBusConsumer<InMemoryMessage1> with
        member _.Handle ctx msg = 
            { Content2 = msg.Content1 } |> ctx.Send "InMemoryHandler2"
            InMemoryHandler1.HandledInvoked |> Option.iter (fun callback -> callback.HasBeenInvoked())

type InMemoryHandler2() =
    static member val HandledInvoked: IHandlerInvoked option = None with get, set

    interface FBus.IBusConsumer<InMemoryMessage2> with
        member _.Handle ctx msg = 
            InMemoryHandler2.HandledInvoked |> Option.iter (fun callback -> callback.HasBeenInvoked())

let startServer<'t> name =
    let checkErrorHook = {
        new IBusHook with
            member _.OnError ctx msg exn =
                failwithf "No error shall be raised: %A" exn
    }

    let serverBus = FBus.Testing.configure() |> withName name
                                             |> withConsumer<'t>
                                             |> withHook checkErrorHook
                                             |> FBus.Builder.build
    serverBus.Start() |> ignore
    serverBus

[<Test>]
let ``check inmemory message exchange`` () =
    let mutable serverHasBeenInvoked1 = false
    let mutable serverHasBeenInvoked2 = false
    let callback1 = { new IHandlerInvoked with member _.HasBeenInvoked() = serverHasBeenInvoked1 <- true }
    let callback2 = { new IHandlerInvoked with member _.HasBeenInvoked() = serverHasBeenInvoked2 <- true }
    InMemoryHandler1.HandledInvoked <- Some callback1
    InMemoryHandler2.HandledInvoked <- Some callback2
    use bus1 = startServer<InMemoryHandler1> "InMemoryHandler1"
    use bus2 = startServer<InMemoryHandler2> "InMemoryHandler2"

    use clientBus = FBus.Testing.configure() |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    Testing.waitForCompletion()

    serverHasBeenInvoked1 |> should equal true
    serverHasBeenInvoked2 |> should equal true

[<Test>]
let ``check inmemory message exchange again`` () =
    ``check inmemory message exchange`` ()
