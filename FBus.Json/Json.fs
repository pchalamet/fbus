module Json
open FBus
open FBus.Builder

let private defaultSerializer = FBus.Serializers.Json() :> IBusSerializer

let useSerializer busBuilder =
    busBuilder |> withSerializer defaultSerializer
