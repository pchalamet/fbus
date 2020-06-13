namespace FBus
open System

type IBusTransport =
    inherit IDisposable
    abstract Publish: ctx:Map<string, string> -> msgType:Type -> body:ReadOnlyMemory<byte> -> unit
    abstract Send: ctx:Map<string, string> -> target:string -> msgType:Type -> body:ReadOnlyMemory<byte> -> unit

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
    abstract BusSender: IBusSender
    abstract Sender: string
    abstract Reply: msg:'t -> unit

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


type BusContext(busSender, headers) =
    interface IContext with
        member _.BusSender = busSender

        member _.Sender = 
            headers |> Map.find "fbus:sender"
            
        member this.Reply(msg: 't): unit = 
            let me = this :> IContext
            me.BusSender.Send me.Sender msg

type BusControl(busBuilder: BusBuilder) =
    do
        busBuilder.Handlers |> Map.iter (fun _ v -> busBuilder.Container.Register v)

    let mutable busTransport : IBusTransport option = None

    let defaultContext = Map [ "fbus:sender", busBuilder.Name ]

    let msgCallback busSender activationContext headers msgType content =
        let handlerInfo = match busBuilder.Handlers |> Map.tryFind msgType with
                          | Some handlerInfo -> handlerInfo
                          | _ -> failwithf "Unknown message type [%s]" msgType
        let handler = busBuilder.Container.Resolve activationContext handlerInfo
        if handler |> isNull then failwith "No handler found"

        let callsite = handlerInfo.InterfaceType.GetMethod("Handle")
        if callsite |> isNull then failwith "Handler method not found"

        let ctx = BusContext(busSender, headers)
        let msg = busBuilder.Serializer.Deserialize handlerInfo.MessageType content
        callsite.Invoke(handler, [| ctx; msg |]) |> ignore

    interface IBusSender with
        member _.Publish (msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> busBuilder.Serializer.Serialize msg |> busTransport.Publish defaultContext (msg.GetType())

        member _.Send (busName: string) (msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> busBuilder.Serializer.Serialize msg |> busTransport.Send defaultContext busName (msg.GetType())

    interface IBusControl with
        member this.Start (activationContext: obj) =
            match busTransport with
            | Some _ -> failwith "Bus is already started"
            | None -> busTransport <- Some (busBuilder.Transport busBuilder (msgCallback this activationContext))
                      this :> IBusSender

        member _.Stop() = 
            match busTransport with
            | None -> failwith "Bus is already stopped"
            | Some dispose -> dispose.Dispose()
                              busTransport <- None

    interface IDisposable with
        member _.Dispose() =
            match busTransport with
            | None -> ()
            | Some transport -> transport.Dispose()
                                busTransport <- None
