module FBus.Json
open FBus

let useDefaults busBuilder = 
    Builder.withSerializer (Serializers.Json() :> IBusSerializer) busBuilder

let useWith options =
    Serializers.Json(options) :> IBusSerializer |> Builder.withSerializer
