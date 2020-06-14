namespace FBus
open System

type IBusTransport =
    inherit IDisposable
    abstract Publish: headers:Map<string, string> -> msgType:Type -> body:ReadOnlyMemory<byte> -> unit
    abstract Send: headers:Map<string, string> -> target:string -> msgType:Type -> body:ReadOnlyMemory<byte> -> unit

type IBusSender =
    abstract Publish: msg:'t -> unit
    abstract Send: string -> 't -> unit

type IBusControl =
    inherit IDisposable
    abstract Start: obj -> IBusSender
    abstract Stop: unit -> unit

type IBusSerializer =
    abstract Serialize: obj -> ReadOnlyMemory<byte>
    abstract Deserialize: Type -> ReadOnlyMemory<byte> -> obj

type HandlerInfo =
    { MessageType: Type
      InterfaceType: Type
      ImplementationType: Type }

type IBusContainer =
    abstract Register: HandlerInfo -> unit
    abstract Resolve: obj -> HandlerInfo -> obj

type IContext =
    abstract Publish: msg:'t -> unit
    abstract Send: string -> 't -> unit
    abstract Reply: msg:'t -> unit
    abstract Sender: string
    abstract ConversationId: string
    abstract MessageId: string

type IBusConsumer<'t> =
    abstract Handle: IContext -> 't -> unit


type BusBuilder =
    { Name: string
      IsEphemeral: bool
      Uri: Uri
      Container: IBusContainer
      Serializer: IBusSerializer
      Transport: BusBuilder -> (Map<string,string> -> string -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : Map<string, HandlerInfo> }


type BusControl(busBuilder: BusBuilder) =
    do
        busBuilder.Handlers |> Map.iter (fun _ v -> busBuilder.Container.Register v)

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

        let flowHeaders () = 
            defaultHeaders |> Map.add "fbus:message-id" (Guid.NewGuid().ToString())
                           |> Map.add "fbus:conversation-id" (headers |> Map.find "fbus:conversation-id")

        let ctx = { new IContext with
                        member _.ConversationId: string = headers |> Map.find "fbus:conversation-id"
                        member _.MessageId: string = headers |> Map.find "fbus:message-id"
                        member _.Sender: string = headers |> Map.find "fbus:sender"
                        member this.Reply msg = flowHeaders() |> send this.Sender msg
                        member _.Publish msg = flowHeaders() |> publish msg
                        member _.Send client msg = flowHeaders() |> send client msg }

        let msg = busBuilder.Serializer.Deserialize handlerInfo.MessageType content
        callsite.Invoke(handler, [| ctx; msg |]) |> ignore

    let startHeaders () =
        defaultHeaders |> Map.add "fbus:conversation-id" (Guid.NewGuid().ToString())
                       |> Map.add "fbus:message-id" (Guid.NewGuid().ToString())

    interface IBusSender with
        member _.Publish msg = 
            startHeaders() |> publish msg

        member _.Send client msg = 
            startHeaders() |> send client msg

    interface IBusControl with
        member this.Start activationContext =
            match busTransport with
            | Some _ -> failwith "Bus is already started"
            | None -> busTransport <- Some (busBuilder.Transport busBuilder (msgCallback activationContext))
                      this :> IBusSender

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
