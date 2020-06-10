module FBus.Core

open FBus
open System
open System.Text.Json
open System.Threading.Tasks

let deserializeMessage (t: System.Type) (body: ReadOnlyMemory<byte>) =
    let options = JsonSerializerOptions()
    JsonSerializer.Deserialize(body.Span, t, options)

let serializeMessage (v: obj) =
    let options = JsonSerializerOptions()
    let body = JsonSerializer.SerializeToUtf8Bytes(v, options)
    ReadOnlyMemory(body)

type BusControl(busBuilder: BusBuilder) =

    let mutable busTransport : IBusTransport option = None

    interface IBusSender with
        member this.Publish(msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> busTransport.Publish typeof<'t> (serializeMessage msg)

        member this.Send(msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> busTransport.Send typeof<'t> (serializeMessage msg)

    interface IBusControl with
        member this.Start (context: obj) =
            match busTransport with
            | Some _ -> failwith "Bus already started"
            | None ->
                let msgCallback handlerInfo content =
                    let handler = busBuilder.Activator context handlerInfo.InterfaceType
                    if handler |> isNull then failwith "No handler found"

                    let callsite = handler.GetType().GetMethod("Handle")
                    if callsite |> isNull then failwith "Handler method not found"

                    let msg = deserializeMessage handlerInfo.MessageType content

                    callsite.Invoke(handler, [| msg |]) |> ignore

                busTransport <- Some (busBuilder.Transport busBuilder msgCallback)

        member this.Stop() = 
            match busTransport with
            | None -> failwith "Bus already stopped"
            | Some dispose -> dispose.Dispose()
                              busTransport <- None
