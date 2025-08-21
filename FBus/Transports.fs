namespace FBus.Transports
open FBus


type private ProcessingAgentMessage =
    | Message of (Map<string, string> * string * System.ReadOnlyMemory<byte>)
    | Exit

type InMemoryContext() =
    let initLock = obj()
    let mutable transports: Map<string, InMemory> = Map.empty
    let mutable msgInFlight = 0
    let doneHandle = new System.Threading.ManualResetEvent(false)

    // NOTE: WaitForCompletion is really best effort
    //       It's theorically possible to complete since messages are processed asynchronously
    let newMsgInFlight() =
        doneHandle.Reset() |> ignore
        System.Threading.Interlocked.Increment(&msgInFlight) |> ignore

    let doneMsgInFlight() =
        let msgInFlight = System.Threading.Interlocked.Decrement(&msgInFlight)
        if msgInFlight = 0 then doneHandle.Set() |> ignore

    member _.WaitForCompletion() =
        doneHandle.WaitOne() |> ignore

    member _.Register name transport =
        lock initLock (fun () -> transports <- transports |> Map.add name transport)

    member _.Unregister name =
        lock initLock (fun () -> transports <- transports |> Map.remove name)

    member _.Publish headers msgType body =
        transports |> Map.filter (fun _ transport -> transport.Accept msgType)
                   |> Map.iter (fun _ transport -> newMsgInFlight()
                                                   transport.Dispatch headers msgType body)

    member _.Send headers client msgType body =
        transports |> Map.tryFind client 
                   |> Option.iter (fun transport -> newMsgInFlight()
                                                    transport.Dispatch headers msgType body)

    member _.Dispatch msgCallback headers body =
        try msgCallback headers body
        with exn -> printfn "FAILURE: Dispatch failure %A" exn
        doneMsgInFlight()

and InMemory(context: InMemoryContext, busConfig, msgCallback) =
    let processingAgent = MailboxProcessor.Start (fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()
            match msg with
            | Message (headers, _, body) -> context.Dispatch msgCallback headers body
                                            return! messageLoop() 
            | Exit -> ()
        }

        messageLoop()
    )

    static member Create context (busConfig: BusConfiguration) msgCallback =
        let transport = new InMemory(context, busConfig, msgCallback)
        context.Register busConfig.Name transport
        transport :> IBusTransport

    member _.Accept msgType = busConfig.Handlers |> Map.containsKey msgType

    member _.Dispatch headers msgtype body = (headers, msgtype, body) |> Message |> processingAgent.Post

    interface IBusTransport with
        member _.Publish headers msgType body key =
            context.Publish headers msgType body

        member _.Send headers client msgType body key =
            context.Send headers client msgType body

        member _.Dispose() =
            context.Unregister busConfig.Name
            Exit |> processingAgent.Post
