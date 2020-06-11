module FBus.Serializer.Json
open FBus
open System
open System.Text.Json
open System.Text.Json.Serialization

type Serializer() =
    interface ISerializer with
        member _.Serialize (v: obj) =
            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())
            let body = JsonSerializer.SerializeToUtf8Bytes(v, options)
            ReadOnlyMemory(body)

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())
            JsonSerializer.Deserialize(body.Span, t, options)
