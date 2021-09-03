namespace FBus.Serializers
open FBus
open System

[<RequireQualifiedAccess>]
type BridgeEventMessage =
    { Type: string
      Message: string }
    interface FBus.IMessageEvent

[<RequireQualifiedAccess>]
type BridgeCommandMessage =
    { Type: string
      Message: string }
    interface FBus.IMessageCommand

type BridgeSerializer() =
    interface IBusSerializer with
        member _.Serialize (v: obj) =
            match v with
            | :? BridgeEventMessage as msg -> let body = System.Text.Encoding.UTF8.GetBytes(msg.Message)
                                              msg.Type, ReadOnlyMemory(body)
            | :? BridgeCommandMessage as msg -> let body = System.Text.Encoding.UTF8.GetBytes(msg.Message)
                                                msg.Type, ReadOnlyMemory(body)
            | _ -> failwith "Expecting BridgeMessage"

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            failwith "BridgeSerializer is not designed to deserialize"
