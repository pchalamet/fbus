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

type InMemoryHandlerFail1() =
    static member val HandledInvoked: IHandlerInvoked option = None with get, set

    interface FBus.IBusConsumer<InMemoryMessage1> with
        member _.Handle ctx msg = 
            { Content2 = msg.Content1 } |> ctx.Send "InMemoryHandler2"
            InMemoryHandlerFail1.HandledInvoked |> Option.iter (fun callback -> callback.HasBeenInvoked())
            failwith "Test failure"

type InMemoryHandler2() =
    static member val HandledInvoked: IHandlerInvoked option = None with get, set

    interface FBus.IBusConsumer<InMemoryMessage2> with
        member _.Handle ctx msg = 
            InMemoryHandler2.HandledInvoked |> Option.iter (fun callback -> callback.HasBeenInvoked())

let startServer<'t> (session: FBus.Testing.Session) name =
    let checkErrorHook = {
        new IBusHook with
            member _.OnError ctx msg exn =
                failwithf "No error shall be raised: %A" exn
    }

    let serverBus = session.Configure() |> withName name
                                        |> withConsumer<'t>
                                        |> withHook checkErrorHook
                                        |> FBus.Builder.build
    serverBus.Start() |> ignore
    serverBus

// We are expecting everything to be OK here
[<Test>]
let ``check inmemory message exchange`` () =
    use session = new FBus.Testing.Session()

    let mutable serverHasBeenInvoked1 = 0
    let mutable serverHasBeenInvoked2 = 0
    let callback1 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked1) |> ignore }
    let callback2 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked2) |> ignore }
    InMemoryHandler1.HandledInvoked <- Some callback1
    InMemoryHandler2.HandledInvoked <- Some callback2
    use bus1 = startServer<InMemoryHandler1> session "InMemoryHandler1"
    use bus2 = startServer<InMemoryHandler2> session "InMemoryHandler2"

    use clientBus = session.Configure() |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    session.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal 1
    serverHasBeenInvoked2 |> should equal 1

// We are expecting both handlers to be called
// As we know handler1 will fail, waitForCompletion() shall still exit
[<Test>]
let ``check inmemory message exchange with handler failure`` () =
    use session = new FBus.Testing.Session()

    let mutable serverHasBeenInvoked1 = 0
    let mutable serverHasBeenInvoked2 = 0
    let callback1 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked1) |> ignore }
    let callback2 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked2) |> ignore }
    InMemoryHandlerFail1.HandledInvoked <- Some callback1
    InMemoryHandler2.HandledInvoked <- Some callback2
    use bus1 = startServer<InMemoryHandlerFail1> session "InMemoryHandler1"
    use bus2 = startServer<InMemoryHandler2> session "InMemoryHandler2"

    use clientBus = session.Configure() |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    session.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal 1
    serverHasBeenInvoked2 |> should equal 1

// We are expecting both handlers to be called
// Message shall be dispatched to all handler1
// Message sent by handler1 will have be received by handler2
[<Test>]
let ``check inmemory message exchange with multiple subscribers`` () =
    use session = new FBus.Testing.Session()

    let mutable serverHasBeenInvoked1 = 0
    let mutable serverHasBeenInvoked2 = 0
    let callback1 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked1) |> ignore }
    let callback2 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked2) |> ignore }
    InMemoryHandler1.HandledInvoked <- Some callback1
    InMemoryHandler2.HandledInvoked <- Some callback2
    use bus1 = startServer<InMemoryHandler1> session "InMemoryHandler1-1"
    use bus2 = startServer<InMemoryHandler1> session "InMemoryHandler1-2"
    use bus3 = startServer<InMemoryHandler1> session "InMemoryHandler1-3"
    use bus4 = startServer<InMemoryHandler2> session "InMemoryHandler2"

    use clientBus = session.Configure() |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    session.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal 3
    serverHasBeenInvoked2 |> should equal 3
