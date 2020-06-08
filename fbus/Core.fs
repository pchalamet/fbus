module FBus.Core
open System.Text.Json
open System

type IBusSender =
    inherit IDisposable
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
      Transport: BusBuilder -> (HandlerInfo -> string -> unit) -> IBusSender
      Handlers : HandlerInfo list }


let deserializeMessage (t: System.Type) (json: string) =
    let options = JsonSerializerOptions()
    JsonSerializer.Deserialize(json, t, options)

type BusControl(busBuilder: BusBuilder) =

    let mutable busSender = None

    interface IBusControl with
        member _.Sender: IBusSender =
            match busSender with
            | Some busSender -> busSender
            | None -> failwith "Bus is not started"
 
        member this.Start (context: obj) =
            match busSender with
            | Some _ -> ()
            | None ->
                let msgCallback handlerInfo content =
                    let handler = busBuilder.Activator context handlerInfo.InterfaceType
                    if handler |> isNull then failwith "No handler found"

                    let callsite = handler.GetType().GetMethod("Handle")
                    if callsite |> isNull then failwith "Handler method not found"

                    let msg = deserializeMessage handlerInfo.MessageType content

                    callsite.Invoke(handler, [| msg |]) |> ignore

                busSender <- busBuilder.Transport busBuilder msgCallback |> Some

        member this.Stop() = 
            match busSender with
            | None -> ()
            | Some dispose -> dispose.Dispose()
                              busSender <- None
