namespace FBus.Serializers
open System
open FBus
open System.Collections.Concurrent

type InMemory() =
    static let refs = ConcurrentDictionary<Guid, obj>()

    interface IBusSerializer with
        member _.Serialize (v: obj) =
            let id = Guid.NewGuid()
            let body = id.ToByteArray() |> ReadOnlyMemory
            if refs.TryAdd(id, v) |> not then failwith "Failed to store message"
            body

        member _.Deserialize (t: System.Type) (body: ReadOnlyMemory<byte>) =
            let id = Guid(body.ToArray())
            match refs.TryGetValue(id) with
            | true, v -> v
            | _ -> failwith "Failed to retrieve message"
