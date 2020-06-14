module FBus.InMemory
open FBus


type Transport(name, msgCallback) =
    static let initLock = obj()
    static let doExclusive = lock initLock
    static let mutable transports = Map.empty

    static member Create (busBuilder: BusBuilder) msgCallback =
        let transport = new Transport(busBuilder.Name, msgCallback)
        doExclusive (fun () -> transports <- transports |> Map.add busBuilder.Name transport)
        transport :> IBusTransport

    member _.Dispatch headers msgType body =
        async { msgCallback headers msgType body } |> Async.Start

    interface IBusTransport with
        member _.Publish headers msgType body =
            doExclusive (fun () -> transports |> Map.iter (fun client transport -> if client <> name then transport.Dispatch headers msgType body))

        member _.Send headers client msgType body =
            doExclusive (fun () -> transports |> Map.tryFind client 
                                              |> Option.iter (fun transport -> transport.Dispatch headers msgType body))

        member _.Dispose() =
            doExclusive (fun () -> transports <- transports |> Map.remove name)
