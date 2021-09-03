namespace FBus.Serializers
open FBus
open System

type NopMessage =
    { Type: string
      Message: string }

type NopSerializer() =
    interface IBusSerializer with
        member _.Serialize (v: obj) =
            match v with
            | :? NopMessage as msg -> let body = System.Text.Encoding.UTF8.GetBytes(msg.Message)
                                      msg.Type, ReadOnlyMemory(body)
            | _ -> failwith "Expecting NopMessage"

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            failwith "NopSerializer is not designed to deserialize"
