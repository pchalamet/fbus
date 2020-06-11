module FBus.Serializer.Json
open FBus
open System
open System.Text.Json
open System.Text.Json.Serialization

let inline (!>) (x : ^a) : ^b = (((^a or ^b) : (static member op_Explicit : ^a -> ^b) x))

type Serializer() =
    interface ISerializer with
        member _.Serialize (v: obj) =
            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())
            let body = JsonSerializer.SerializeToUtf8Bytes(v, options).AsMemory()
            (!> body) : ReadOnlyMemory<byte>

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            let options = JsonSerializerOptions()
            options.Converters.Add(JsonFSharpConverter())
            JsonSerializer.Deserialize(body.Span, t, options)
