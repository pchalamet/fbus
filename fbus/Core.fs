module FBus.Core
open FBus
open System

type BusControl(busBuilder: BusBuilder) =

    let mutable busTransport : IBusTransport option = None

    interface IBusSender with
        member this.Publish (msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> let body = busBuilder.Serializer.Serialize msg
                                   busTransport.Publish typeof<'t> body

        member this.Send (destination: string) (msg: 't) = 
            match busTransport with
            | None -> failwith "Bus is not started"
            | Some busTransport -> let body = busBuilder.Serializer.Serialize msg
                                   busTransport.Send destination typeof<'t> body

    interface IDisposable with
        member this.Dispose() =
            match busTransport with
            | Some transport -> transport.Dispose()
                                busTransport <- None
            | _ -> ()

    interface IBusControl with
        member this.Start (context: obj) =
            match busTransport with
            | Some _ -> failwith "Bus is already started"
            | None ->
                let msgCallback handlerInfo content =
                    let handler = busBuilder.Activator context handlerInfo.InterfaceType
                    if handler |> isNull then failwith "No handler found"

                    let callsite = handlerInfo.InterfaceType.GetMethod("Handle")
                    if callsite |> isNull then failwith "Handler method not found"

                    let msg = busBuilder.Serializer.Deserialize handlerInfo.MessageType content

                    callsite.Invoke(handler, [| msg |]) |> ignore

                busTransport <- Some (busBuilder.Transport busBuilder msgCallback)
                this :> IBusSender

        member this.Stop() = 
            match busTransport with
            | None -> failwith "Bus is already stopped"
            | Some dispose -> dispose.Dispose()
                              busTransport <- None
