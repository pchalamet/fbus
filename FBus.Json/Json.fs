module FBus.Json
open FBus
open FBus.Builder

let useSerializer busBuilder =
    let defaultSerializer = FBus.Serializers.Json() :> IBusSerializer
    busBuilder |> withSerializer defaultSerializer
