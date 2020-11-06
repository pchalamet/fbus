module Json
open FBus
open FBus.Builder

let private defaultSerializer = FBus.Json.Serializer() :> IBusSerializer

let useSerializer busBuilder =
    busBuilder |> withSerializer defaultSerializer
