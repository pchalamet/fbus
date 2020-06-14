module FBus.Control
open System

type BusControl(busBuilder: BusBuilder) =
    do busBuilder.Handlers |> Map.iter (fun _ v -> busBuilder.Container.Register v)
    let mutable busTransport : IBusTransport option = None
    let defaultHeaders = Map [ "fbus:sender", busBuilder.Name ]

    let publish msg headers =
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> busBuilder.Serializer.Serialize msg |> busTransport.Publish headers (msg.GetType())

    let send client msg headers =
        match busTransport with
        | None -> failwith "Bus is not started"
        | Some busTransport -> busBuilder.Serializer.Serialize msg |> busTransport.Send headers client (msg.GetType())

    let msgCallback activationContext headers msgType content =
        let handlerInfo = match busBuilder.Handlers |> Map.tryFind msgType with
                          | Some handlerInfo -> handlerInfo
                          | _ -> failwithf "Unknown message type [%s]" msgType
        let handler = busBuilder.Container.Resolve activationContext handlerInfo
        if handler |> isNull then failwith "No handler found"

        let callsite = handlerInfo.InterfaceType.GetMethod("Handle")
        if callsite |> isNull then failwith "Handler method not found"

        let conversationHeaders () = 
            defaultHeaders |> Map.add "fbus:message-id" (Guid.NewGuid().ToString())
                           |> Map.add "fbus:conversation-id" (headers |> Map.find "fbus:conversation-id")

        let ctx = { new IBusConversation with
                        member _.ConversationId: string = headers |> Map.find "fbus:conversation-id"
                        member _.MessageId: string = headers |> Map.find "fbus:message-id"
                        member _.Sender: string = headers |> Map.find "fbus:sender"
                        member this.Reply msg = conversationHeaders() |> send this.Sender msg
                        member _.Publish msg = conversationHeaders() |> publish msg
                        member _.Send client msg = conversationHeaders() |> send client msg }

        let msg = busBuilder.Serializer.Deserialize handlerInfo.MessageType content
        callsite.Invoke(handler, [| ctx; msg |]) |> ignore

    let newConversationHeaders () =
        defaultHeaders |> Map.add "fbus:conversation-id" (Guid.NewGuid().ToString())
                       |> Map.add "fbus:message-id" (Guid.NewGuid().ToString())

    interface IBusInitiator with
        member _.Publish msg = newConversationHeaders() |> publish msg
        member _.Send client msg = newConversationHeaders() |> send client msg

    interface IBusControl with
        member this.Start activationContext =
            match busTransport with
            | Some _ -> failwith "Bus is already started"
            | None -> busTransport <- Some (busBuilder.Transport busBuilder (msgCallback activationContext))
                      this :> IBusInitiator

        member _.Stop() = 
            match busTransport with
            | None -> failwith "Bus is already stopped"
            | Some dispose -> dispose.Dispose()
                              busTransport <- None

        member _.Dispose() =
            match busTransport with
            | None -> ()
            | Some transport -> transport.Dispose()
                                busTransport <- None
