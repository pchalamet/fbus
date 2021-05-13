namespace FBus.Control
open System
open FBus

type Bus(busConfig: BusConfiguration) =
    do busConfig.Handlers |> Map.iter (fun _ v -> busConfig.Container.Register v)

    let initLock = obj()
    let doExclusive = lock initLock
    let mutable busTransport : IBusTransport option = None

    let defaultHeaders = Map [ "fbus:sender", busConfig.Name ]

    let getMsgType (t: obj) =
        t.GetType().FullName

    let publish msg headers =
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> busConfig.Serializer.Serialize msg |> busTransport.Publish headers (msg |> getMsgType)

    let send client msg headers =
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> busConfig.Serializer.Serialize msg |> busTransport.Send headers client (msg |> getMsgType)

    let msgCallback activationContext headers msgType content =
        let mutable msg: obj = null
        let ctx = 
            let conversationHeaders = 
                defaultHeaders |> Map.add "fbus:message-id" (Guid.NewGuid().ToString())
                               |> Map.add "fbus:conversation-id" (headers |> Map.find "fbus:conversation-id")

            { new IBusConversation with
                    member _.ConversationId: string = headers |> Map.find "fbus:conversation-id"
                    member _.MessageId: string = headers |> Map.find "fbus:message-id"
                    member _.Sender: string = headers |> Map.find "fbus:sender"
                    member this.Reply msg = doExclusive (fun () -> send this.Sender msg conversationHeaders)
                    member _.Publish msg = doExclusive (fun () -> publish msg conversationHeaders)
                    member _.Send client msg = doExclusive (fun () -> send client msg conversationHeaders) }

        use hookState = busConfig.Hook |> Option.map (fun hook -> hook.OnBeforeProcessing ctx) |> Option.defaultValue null

        try
            let handlerInfo = match busConfig.Handlers |> Map.tryFind msgType with
                              | Some handlerInfo -> handlerInfo
                              | _ -> failwithf "Unknown message type [%s]" msgType
            let handler = busConfig.Container.Resolve activationContext handlerInfo
            if handler |> isNull then failwith "No handler found"

            msg <- busConfig.Serializer.Deserialize handlerInfo.MessageType content
            handlerInfo.CallSite.Invoke(handler, [| ctx; msg |]) |> ignore
        with
            | :? Reflection.TargetInvocationException as tie -> busConfig.Hook |> Option.iter (fun hook -> hook.OnError ctx msg tie.InnerException)
                                                                reraise()
            | exn -> busConfig.Hook |> Option.iter (fun hook -> hook.OnError ctx msg exn)
                     reraise()

    let newConversationHeaders () =
        defaultHeaders |> Map.add "fbus:conversation-id" (Guid.NewGuid().ToString())
                       |> Map.add "fbus:message-id" (Guid.NewGuid().ToString())

    let start activationContext =
        match busTransport with
        | Some _ -> failwith "Bus is already started"
        | None -> busTransport <- Some (busConfig.Transport busConfig (msgCallback activationContext))

    let stop initiator =
        match busTransport with
        | None -> failwith "Bus is already stopped"
        | Some transport -> busConfig.Hook |> Option.iter (fun hook -> hook.OnStop initiator)
                            transport.Dispose()
                            busTransport <- None

    let dispose initiator =
        match busTransport with
        | None -> ()
        | Some transport -> busConfig.Hook |> Option.iter (fun hook -> hook.OnStop initiator)
                            transport.Dispose()
                            busTransport <- None

    interface IBusInitiator with
        member _.Publish msg = doExclusive (fun () -> newConversationHeaders() |> publish msg)
        member _.Send client msg = doExclusive (fun () -> newConversationHeaders() |> send client msg)

    interface IBusControl with
        member this.Start activationContext =
            doExclusive (fun() -> start activationContext)
            let initiator = this :> IBusInitiator
            busConfig.Hook |> Option.iter (fun hook -> hook.OnStart initiator)
            initiator

        member this.Stop() = 
            let initiator = this :> IBusInitiator
            doExclusive (fun () -> stop initiator)

        member this.Dispose() =
            let initiator = this :> IBusInitiator
            doExclusive (fun () -> dispose initiator)
