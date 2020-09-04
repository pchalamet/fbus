module FBus.InMemory
open FBus

type private ProcessingAgentMessage =
    | Message of (Map<string, string> * string * System.ReadOnlyMemory<byte>)
    | Exit

type Transport(busBuilder, msgCallback) =
    static let initLock = obj()
    static let doExclusive = lock initLock
    static let mutable transports = Map.empty
    static let mutable msgInFlight = 0
    static let doneHandle = new System.Threading.ManualResetEvent(false)

    let processingAgent = MailboxProcessor.Start (fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()
            match msg with
            | Message (headers, msgType, body) -> msgCallback headers msgType body
                                                  doExclusive (fun () -> msgInFlight <- msgInFlight - 1
                                                                         if msgInFlight = 0 then doneHandle.Set() |> ignore)
                                                  return! messageLoop() 
            | Exit -> ()
        }

        messageLoop()
    )

    static member Create (busBuilder: BusBuilder) msgCallback =
        doneHandle.Reset() |> ignore
        let transport = new Transport(busBuilder, msgCallback)
        doExclusive (fun () -> transports <- transports |> Map.add busBuilder.Name transport)
        transport :> IBusTransport

    static member WaitForCompletion() =
        doneHandle.WaitOne() |> ignore

    member private _.Accept msgType =
        busBuilder.Handlers |> Map.containsKey msgType

    member private _.Dispatch headers msgType body =
        (headers, msgType, body) |> Message |> processingAgent.Post 

    interface IBusTransport with
        member _.Publish headers msgType body =
            doExclusive (fun () -> transports |> Map.filter (fun _ transport -> transport.Accept msgType)
                                              |> Map.iter (fun _ transport -> msgInFlight <- msgInFlight + 1
                                                                              doneHandle.Reset() |> ignore
                                                                              transport.Dispatch headers msgType body))

        member _.Send headers client msgType body =
            doExclusive (fun () -> transports |> Map.tryFind client 
                                              |> Option.iter (fun transport -> msgInFlight <- msgInFlight + 1
                                                                               doneHandle.Reset() |> ignore
                                                                               transport.Dispatch headers msgType body))

        member _.Dispose() =
            doExclusive (fun () -> transports <- transports |> Map.remove busBuilder.Name)
            Exit |> processingAgent.Post
