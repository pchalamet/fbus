module FBus.Json
open FBus
open System
open System.Text.Json

type Serializer(?initOptions: JsonSerializerOptions -> unit) =

    let initOptions = defaultArg initOptions ignore

    let options =
        let options = JsonSerializerOptions()
        options |> initOptions
        options

    interface IBusSerializer with
        member _.Serialize (v: obj) =
            let body = JsonSerializer.SerializeToUtf8Bytes(v, options)
            ReadOnlyMemory(body)

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            JsonSerializer.Deserialize(body.Span, t, options)
