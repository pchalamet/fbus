module FBus.Json
open FBus
open System
open System.Text.Json
open System.Text.Json.Serialization

type Serializer(?options: JsonSerializerOptions) =

    let defaultOptions =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter())
        options

    let options = defaultArg options defaultOptions

    interface IBusSerializer with
        member _.Serialize (v: obj) =
            let body = JsonSerializer.SerializeToUtf8Bytes(v, options)
            ReadOnlyMemory(body)

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            JsonSerializer.Deserialize(body.Span, t, options)
