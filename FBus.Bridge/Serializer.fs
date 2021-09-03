namespace FBus.Serializers
open FBus
open System

[<RequireQualifiedAccess>]
type BridgeMessage =
    { Type: string
      Message: string }

type BridgeSerializer() =
    interface IBusSerializer with
        member _.Serialize (v: obj) =
            match v with
            | :? BridgeMessage as msg -> let body = System.Text.Encoding.UTF8.GetBytes(msg.Message)
                                         msg.Type, ReadOnlyMemory(body)
            | _ -> failwith "Expecting BridgeMessage"

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            failwith "BridgeSerializer is not designed to deserialize"
