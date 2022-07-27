module FBus.Bridge
open FBus

let useDefaults =
    Serializers.BridgeSerializer() :> IBusSerializer |> Builder.withSerializer
