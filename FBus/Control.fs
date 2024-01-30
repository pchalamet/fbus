namespace FBus.Control
open System
open FBus


type Bus(busConfig: BusConfiguration) =

    [<Literal>]
    let FBUS_MSGTYPE = "fbus:msg-type"

    [<Literal>]
    let FBUS_CONVERSATION_ID = "fbus:conversation-id"

    [<Literal>]
    let FBUS_MESSAGE_ID = "fbus:msg-id"

    [<Literal>]
    let FBUS_SENDER = "fbus:sender"

    do busConfig.Handlers |> Map.iter (fun _ v -> busConfig.Container.Register v)

    let initLock = obj()
    let doExclusive = lock initLock
    let mutable busTransport : IBusTransport option = None

    let defaultHeaders = Map [ FBUS_SENDER, busConfig.Name ]

    let getMessageKey (msg: obj) =
        match msg with
        | :? IMessageKey as key -> key.Key
        | _ -> ""

    let publish msg headers =
        let routing = msg |> getMessageKey
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> let msgtype, body = busConfig.Serializer.Serialize msg
                               let msgHeaders = headers |> Map.add FBUS_MSGTYPE msgtype
                               busTransport.Publish msgHeaders msgtype body routing

    let send client msg headers =
        let routing = msg |> getMessageKey
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> let msgtype, body = busConfig.Serializer.Serialize msg
                               let msgHeaders = headers |> Map.add FBUS_MSGTYPE msgtype
                               busTransport.Send msgHeaders client msgtype body routing

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
                    member this.Reply (msg: 't) = conversationHeaders() |> send this.Sender msg
                    member _.Publish msg = conversationHeaders() |> publish msg
                    member _.Send client msg = conversationHeaders() |> send client msg }

        use hookState = busConfig.Hook |> Option.map (fun hook -> hook.OnBeforeProcessing ctx) |> Option.defaultValue null

        let dynamicFunction (fn:obj) (args:obj list) =
            let rec dynamicFunctionInternal (next:obj) (args:obj list) =
                match args with
                | head :: tail -> let fType = next.GetType()
                                  if Reflection.FSharpType.IsFunction fType then
                                      let methodInfo = 
                                          fType.GetMethods()
                                              |> Seq.find (fun x -> x.Name = "Invoke" && x.GetParameters().Length = 1)
                                      let partalResult = methodInfo.Invoke(next, [| head |])
                                      dynamicFunctionInternal partalResult tail
                                  else
                                      failwithf "Expecting FSharpFunc"
                | _ -> ()
            dynamicFunctionInternal fn (args |> List.ofSeq )

        try
            let msgType = headers |> Map.find FBUS_MSGTYPE

            let handlerInfo = match busConfig.Handlers |> Map.tryFind msgType with
                              | Some handlerInfo -> handlerInfo
                              | _ -> failwithf "Unknown message type [%s]" msgType

            msg <- busConfig.Serializer.Deserialize handlerInfo.MessageType content

            let itfType = typedefof<IBusConsumer<_>>.MakeGenericType(handlerInfo.MessageType)
            let callsite = itfType.GetMethod("Handle")
            if callsite |> isNull then failwith "Handler method not found"

            use newActivationContext = busConfig.Container.NewScope activationContext
            let scope =
                if newActivationContext |> isNull then activationContext
                else newActivationContext :> obj
            let handler = busConfig.Container.Resolve scope handlerInfo
            if handler |> isNull then failwith "No handler found"
            callsite.Invoke(handler, [| ctx; msg |]) |> ignore
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
