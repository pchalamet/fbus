module FBus.Json
open FBus
open FBus.Builder

let useDefaults = FBus.Serializers.Json() :> IBusSerializer |> withSerializer

let useWith options = FBus.Serializers.Json(?initOptions = options) :> IBusSerializer |> withSerializer
