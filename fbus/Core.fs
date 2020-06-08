module FBus.Core
open System.Text.Json
open System

type IBusTransport =
    inherit IDisposable
    abstract Publish: Type -> string -> Async<Unit>
    abstract Send: Type -> string -> Async<Unit>

type IBusSender =
    abstract Publish: 't -> Async<Unit>
    abstract Send: 't -> Async<Unit>

type IBusControl =
    abstract Start: obj -> unit
    abstract Stop: unit -> unit
    abstract Sender: IBusSender

type IConsumer<'t> =
    abstract Handle: 't -> unit

type HandlerInfo = {
    MessageType: System.Type
    InterfaceType: System.Type
    ImplementationType: System.Type
}

type BusBuilder =
    { Name: string option
      Uri : System.Uri
      AutoDelete: bool
      Registrant: HandlerInfo -> unit
      Activator: obj -> System.Type -> obj
      Transport: BusBuilder -> (HandlerInfo -> string -> unit) -> IBusTransport
      Handlers : HandlerInfo list }


let deserializeMessage (t: System.Type) (json: string) =
    let options = JsonSerializerOptions()
    JsonSerializer.Deserialize(json, t, options)

type BusSender(busTransport: IBusTransport) =
    interface IBusSender with
        member this.Publish(arg1: 't): Async<Unit> = 
            failwith "Not Implemented"
        member this.Send(arg1: 't): Async<Unit> = 
            failwith "Not Implemented"


type BusControl(busBuilder: BusBuilder) =

    let mutable busTransport = None

    interface IBusControl with
        member _.Sender: IBusSender =
            match busTransport with
            | Some busTransport -> busTransport |> BusSender :> IBusSender
            | None -> failwith "Bus is not started"
 
        member this.Start (context: obj) =
            match busTransport with
            | Some _ -> ()
            | None ->
                let msgCallback handlerInfo content =
                    let handler = busBuilder.Activator context handlerInfo.InterfaceType
                    if handler |> isNull then failwith "No handler found"

                    let callsite = handler.GetType().GetMethod("Handle")
                    if callsite |> isNull then failwith "Handler method not found"

                    let msg = deserializeMessage handlerInfo.MessageType content

                    callsite.Invoke(handler, [| msg |]) |> ignore

                busTransport <- busBuilder.Transport busBuilder msgCallback |> Some

        member this.Stop() = 
            match busTransport with
            | None -> ()
            | Some dispose -> dispose.Dispose()
                              busTransport <- None
