module FBus.Json
open FBus
open FBus.Builder

let useSerializer = FBus.Serializers.Json() :> IBusSerializer |> withSerializer
