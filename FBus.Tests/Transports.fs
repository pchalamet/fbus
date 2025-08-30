module FBus.Transports.Tests
open NUnit.Framework
open FsUnit

open FBus

open System
open System.Threading
open System.Collections.Concurrent

type InMemoryMessage1 =
    { Content1: string } 
    interface FBus.IMessageEvent

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

    interface FBus.IAsyncBusConsumer<InMemoryMessage2> with
        member _.HandleAsync ctx msg =
            task {
                do! System.Threading.Tasks.Task.Delay(100)
                InMemoryHandler2.HandledInvoked |> Option.iter (fun callback -> callback.HasBeenInvoked())
            }

type InMemoryHandlerFail2() =
    static member val HandledInvoked: IHandlerInvoked option = None with get, set

    interface FBus.IAsyncBusConsumer<InMemoryMessage1> with
        member _.HandleAsync ctx msg = 
            task {
                { Content2 = msg.Content1 } |> ctx.Send "InMemoryHandler2"
                do! System.Threading.Tasks.Task.Delay(100)
                InMemoryHandlerFail2.HandledInvoked |> Option.iter (fun callback -> callback.HasBeenInvoked())
                failwith "Test failure"
            }

let startServer<'t> (session: FBus.Testing.Session) name =
    let checkErrorHook = {
        new IBusHook with
            member _.OnStart initiator = ()
            member _.OnStop initiator = ()
            member _.OnBeforeProcessing ctx = null
            member _.OnError ctx msg exn = failwithf "No error shall be raised: %A" exn
    }

    let serverBus = Builder.configure() |> session.Use
                                        |> Builder.withName name
                                        |> Builder.withConsumer<'t>
                                        |> Builder.withHook checkErrorHook
                                        |> Builder.build
    serverBus.Start() |> ignore
    serverBus

// We are expecting everything to be OK here
[<Test>]
let ``check inmemory message exchange`` () =
    let session = FBus.Testing.Session()

    let mutable serverHasBeenInvoked1 = 0
    let mutable serverHasBeenInvoked2 = 0
    let callback1 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked1) |> ignore }
    let callback2 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked2) |> ignore }
    InMemoryHandler1.HandledInvoked <- Some callback1
    InMemoryHandler2.HandledInvoked <- Some callback2
    use bus1 = startServer<InMemoryHandler1> session "InMemoryHandler1"
    use bus2 = startServer<InMemoryHandler2> session "InMemoryHandler2"

    use clientBus = FBus.Builder.configure() |> session.Use |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    session.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal 1
    serverHasBeenInvoked2 |> should equal 1

// We are expecting both handlers to be called
// As we know handler1 will fail, waitForCompletion() shall still exit
[<Test>]
let ``check inmemory message exchange with handler failure`` () =
    let session = FBus.Testing.Session()

    let mutable serverHasBeenInvoked1 = 0
    let mutable serverHasBeenInvoked2 = 0
    let callback1 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked1) |> ignore }
    let callback2 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked2) |> ignore }
    InMemoryHandlerFail1.HandledInvoked <- Some callback1
    InMemoryHandler2.HandledInvoked <- Some callback2
    use bus1 = startServer<InMemoryHandlerFail1> session "InMemoryHandler1"
    use bus2 = startServer<InMemoryHandler2> session "InMemoryHandler2"

    use clientBus = FBus.Builder.configure() |> session.Use |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    session.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal 1
    serverHasBeenInvoked2 |> should equal 1

// We are expecting both handlers to be called
// As we know handler1 will fail, waitForCompletion() shall still exit
[<Test>]
let ``check inmemory async message exchange with handler failure`` () =
    let session = FBus.Testing.Session()

    let mutable serverHasBeenInvoked1 = 0
    let mutable serverHasBeenInvoked2 = 0
    let callback1 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked1) |> ignore }
    let callback2 = { new IHandlerInvoked with member _.HasBeenInvoked() = System.Threading.Interlocked.Increment(&serverHasBeenInvoked2) |> ignore }
    InMemoryHandlerFail2.HandledInvoked <- Some callback1
    InMemoryHandler2.HandledInvoked <- Some callback2
    use bus1 = startServer<InMemoryHandlerFail2> session "InMemoryHandler1"
    use bus2 = startServer<InMemoryHandler2> session "InMemoryHandler2"

    use clientBus = FBus.Builder.configure() |> session.Use |> FBus.Builder.build
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
    let session = FBus.Testing.Session()

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

    use clientBus = FBus.Builder.configure() |> session.Use |> FBus.Builder.build
    let clientInitiator = clientBus.Start() 

    { Content1 = "Hello InMemory" } |> clientInitiator.Publish

    session.WaitForCompletion()

    serverHasBeenInvoked1 |> should equal 3
    serverHasBeenInvoked2 |> should equal 3

// -------------------------------------------------------------------------------------------------
// Concurrency test: ensure control/handler pipeline supports concurrent activation
// Uses a custom transport that immediately delivers two messages in parallel.
// -------------------------------------------------------------------------------------------------

type ConcurrentMessage =
    { Id: int }
    interface FBus.IMessageEvent

type ConcurrentHandler() =
    // Static sync primitives are set by the test before starting the bus
    static member val Started: CountdownEvent = new CountdownEvent(2) with get, set
    static member val Release: ManualResetEventSlim = new ManualResetEventSlim(false) with get, set
    static member val StartedLog: ConcurrentBag<int> = new ConcurrentBag<int>() with get, set

    interface FBus.IBusConsumer<ConcurrentMessage> with
        member _.Handle ctx msg =
            ConcurrentHandler.StartedLog.Add msg.Id
            ConcurrentHandler.Started.Signal() |> ignore
            // Wait until test signals both handlers have started
            ConcurrentHandler.Release.Wait()

[<Test>]
let ``handlers can be activated concurrently`` () =
    let started = new CountdownEvent(2)
    use release = new ManualResetEventSlim(false)
    let startedLog = new ConcurrentBag<int>()

    // Make handler use our sync primitives
    ConcurrentHandler.Started <- started
    ConcurrentHandler.Release <- release
    ConcurrentHandler.StartedLog <- startedLog

    // Custom transport that triggers two concurrent deliveries on creation
    let createTransport (busConfig: BusConfiguration) (cb: Map<string,string> -> ReadOnlyMemory<byte> -> unit) : IBusTransport =
        // Build headers/body for our test message
        let mkDelivery id =
            let msg = { Id = id } :> obj
            let msgType, body = busConfig.Serializer.Serialize msg
            let headers =
                [ "fbus:msg-type", msgType
                  "fbus:conversation-id", Guid.NewGuid().ToString()
                  "fbus:msg-id", Guid.NewGuid().ToString()
                  "fbus:sender", "test" ]
                |> Map.ofList
            fun () -> cb headers body

        // Fire two deliveries concurrently
        let _ = System.Threading.Tasks.Task.Run(fun () -> mkDelivery 1 () )
        let _ = System.Threading.Tasks.Task.Run(fun () -> mkDelivery 2 () )

        { new IBusTransport with
            member _.Publish _ _ _ _ = ()
            member _.Send _ _ _ _ _ = ()
            member _.Dispose() = () }

    let bus = Builder.configure()
               |> Builder.withName "concurrent-test"
               |> Builder.withContainer (FBus.Containers.Activator() :> IBusContainer)
               |> Builder.withSerializer (FBus.Serializers.Json() :> IBusSerializer)
               |> Builder.withConsumer<ConcurrentHandler>
               |> Builder.withTransport createTransport
               |> Builder.withConcurrency 2
               |> Builder.build

    // Start bus which constructs transport and triggers two concurrent deliveries
    let initiator = bus.Start(null)

    // Both handlers should start within the timeout, proving concurrent activation
    started.Wait(TimeSpan.FromSeconds(5.0)) |> should equal true

    // Make sure both ids have started processing (order not important)
    let arr = startedLog.ToArray() |> Array.sort
    arr |> should equal [| 1; 2 |]

    // Release handlers to complete
    release.Set()

    // Stop bus explicitly (preferred over Dispose here)
    bus.Stop()
