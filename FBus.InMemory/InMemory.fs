module FBus.InMemory
open FBus
open FBus.Builder
open System.Collections.Concurrent
open System


type Activator() =
    interface IBusContainer with
        member _.Register handlerInfo = ()

        member _.Resolve activationContext handlerInfo =
            System.Activator.CreateInstance(handlerInfo.ImplementationType)


type Serializer() =
    static let refs = ConcurrentDictionary<Guid, obj>()

    interface IBusSerializer with
        member _.Serialize (v: obj) =
            let id = Guid.NewGuid()
            let body = id.ToByteArray() |> ReadOnlyMemory
            if refs.TryAdd(id, v) |> not then failwith "Failed to store object"
            body

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            let id = Guid(body.ToArray())
            match refs.TryRemove(id) with
            | true, v -> v
            | _ -> failwith "Failed to retrieve object"


type private ProcessingAgentMessage =
    | Message of (Map<string, string> * string * System.ReadOnlyMemory<byte>)
    | Exit

type Transport(busConfig, msgCallback) =
    static let initLock = obj()
    static let mutable transports = Map.empty
    static let mutable msgInFlight = 0
    static let doneHandle = new System.Threading.ManualResetEvent(false)

    static let newMsgInFlight() =
        let msgInFlight = System.Threading.Interlocked.Increment(&msgInFlight)
        if msgInFlight = 1 then doneHandle.Reset() |> ignore

    static let doneMsgInFlight() =
        let msgInFlight = System.Threading.Interlocked.Decrement(&msgInFlight)
        if msgInFlight = 0 then doneHandle.Set() |> ignore

    let processingAgent = MailboxProcessor.Start (fun inbox ->
        let rec messageLoop() = async {
            let! msg = inbox.Receive()
            match msg with
            | Message (headers, msgType, body) -> msgCallback headers msgType body
                                                  doneMsgInFlight()
                                                  return! messageLoop() 
            | Exit -> ()
        }

        messageLoop()
    )

    static member Create (busConfig: BusConfiguration) msgCallback =
        doneHandle.Reset() |> ignore
        let transport = new Transport(busConfig, msgCallback)
        lock initLock (fun () -> transports <- transports |> Map.add busConfig.Name transport)
        transport :> IBusTransport

    static member WaitForCompletion() =
        doneHandle.WaitOne() |> ignore

    member private _.Accept msgType =
        busConfig.Handlers |> Map.containsKey msgType

    member private _.Dispatch headers msgType body =
        (headers, msgType, body) |> Message |> processingAgent.Post 

    interface IBusTransport with
        member _.Publish headers msgType body =
            transports |> Map.filter (fun _ transport -> transport.Accept msgType)
                       |> Map.iter (fun _ transport -> newMsgInFlight()
                                                       transport.Dispatch headers msgType body)

        member _.Send headers client msgType body =
            transports |> Map.tryFind client 
                       |> Option.iter (fun transport -> newMsgInFlight()
                                                        transport.Dispatch headers msgType body)

        member _.Dispose() =
            lock initLock (fun () -> transports <- transports |> Map.remove busConfig.Name)
            Exit |> processingAgent.Post

let private defaultUri = Uri("inmemory://")

let private defaultTransport (busConfig: BusConfiguration) msgCallback =
    new Transport(busConfig, msgCallback) :> IBusTransport

let private defaultSerializer = Serializer() :> IBusSerializer

let private defaultContainer = Activator() :> IBusContainer

let useTransport (busBuilder: BusBuilder) =
    busBuilder |> withEndpoint defaultUri
               |> withTransport defaultTransport

let useSerializer (busBuilder: BusBuilder) =
    busBuilder |> withSerializer defaultSerializer

let useContainer (busBuilder: BusBuilder) =
    busBuilder |> withContainer defaultContainer
