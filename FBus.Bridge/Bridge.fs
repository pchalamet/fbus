namespace FBus
open FBus

[<AbstractClass; Sealed>]
type Bridge =
    [<CompiledName("UseDefaults")>]
    static member useDefaults =
        FBus.Serializers.BridgeSerializer() :> IBusSerializer |> Builder.withSerializer
