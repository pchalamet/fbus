module FBus.Bridge
open FBus
open FBus.Builder

let useDefaults = FBus.Serializers.BridgeSerializer() :> IBusSerializer |> withSerializer
