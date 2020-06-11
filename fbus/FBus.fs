namespace FBus
open System

type IBusTransport =
    inherit IDisposable
    abstract Publish: Type -> ReadOnlyMemory<byte> -> unit
    abstract Send: string -> Type -> ReadOnlyMemory<byte> -> unit

type IBusSender =
    abstract Publish: 't -> unit
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
    abstract Resolve: obj -> Type -> obj

type IContext = interface end

type IBusConsumer<'t> =
    abstract Handle: IContext -> 't -> unit


type BusBuilder =
    { Name: string option
      Uri : Uri
      AutoDelete: bool
      Container: IBusContainer
      Serializer: IBusSerializer
      Transport: BusBuilder -> (HandlerInfo -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : HandlerInfo list }


type BusControl(busBuilder: BusBuilder) =
    let mutable busTransport : IBusTransport option = None

    let msgCallback context handlerInfo content =
        let handler = busBuilder.Container.Resolve context handlerInfo.InterfaceType
        if handler |> isNull then failwith "No handler found"

        let callsite = handlerInfo.InterfaceType.GetMethod("Handle")
        if callsite |> isNull then failwith "Handler method not found"

        let msg = busBuilder.Serializer.Deserialize handlerInfo.MessageType content
        callsite.Invoke(handler, [| msg |]) |> ignore

    interface IBusSender with
        member _.Publish (msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> busBuilder.Serializer.Serialize msg |> busTransport.Publish typeof<'t>

        member _.Send (destination: string) (msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> busBuilder.Serializer.Serialize msg |> busTransport.Send destination typeof<'t>

    interface IBusControl with
        member this.Start (context: obj) =
            match busTransport with
            | Some _ -> failwith "Bus is already started"
            | None -> busTransport <- Some (busBuilder.Transport busBuilder (msgCallback context))
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
