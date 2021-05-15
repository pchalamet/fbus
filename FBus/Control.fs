namespace FBus.Control
open System
open FBus
open FBus.Headers


type Bus(busConfig: BusConfiguration) =
    do busConfig.Handlers |> Map.iter (fun _ v -> busConfig.Container.Register v)

    let initLock = obj()
    let doExclusive = lock initLock
    let mutable busTransport : IBusTransport option = None

    let defaultHeaders = Map [ FBUS_SENDER, busConfig.Name ]

    let getMsgType (t: obj) =
        t.GetType().FullName

    let publish msg headers =
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> let msgtype = msg |> getMsgType
                               let msgHeaders = headers |> Map.add FBUS_MSGTYPE msgtype
                               busConfig.Serializer.Serialize msg |> busTransport.Publish msgHeaders msgtype

    let send client msg headers =
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> let msgtype = msg |> getMsgType
                               let msgHeaders = headers |> Map.add FBUS_MSGTYPE msgtype
                               busConfig.Serializer.Serialize msg |> busTransport.Send msgHeaders client msgtype

    let msgCallback activationContext headers content =
        let mutable msg: obj = null
        let ctx = 
            let conversationHeaders () = 
                defaultHeaders |> Map.add FBUS_MESSAGE_ID (Guid.NewGuid().ToString())
                               |> Map.add FBUS_CONVERSATION_ID (headers |> Map.find FBUS_CONVERSATION_ID)

            { new IBusConversation with
                    member _.ConversationId: string = headers |> Map.find FBUS_CONVERSATION_ID
                    member _.MessageId: string = headers |> Map.find FBUS_MESSAGE_ID
                    member _.Sender: string = headers |> Map.find FBUS_SENDER
                    member this.Reply msg = conversationHeaders() |> send this.Sender msg
                    member _.Publish msg = conversationHeaders() |> publish msg
                    member _.Send client msg = conversationHeaders() |> send client msg }

        use hookState = busConfig.Hook |> Option.map (fun hook -> hook.OnBeforeProcessing ctx) |> Option.defaultValue null

        try
            let msgType = headers |> Map.find FBUS_MSGTYPE
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
        defaultHeaders |> Map.add FBUS_CONVERSATION_ID (Guid.NewGuid().ToString())
                       |> Map.add FBUS_MESSAGE_ID (Guid.NewGuid().ToString())

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
        member _.Publish msg = newConversationHeaders() |> publish msg
        member _.Send client msg = newConversationHeaders() |> send client msg

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
