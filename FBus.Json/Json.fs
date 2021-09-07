namespace FBus
open FBus

[<AbstractClass; Sealed>]
type Json =
    [<CompiledName("UseDefaults")>]
    static member useDefaults busBuilder = 
        Builder.withSerializer (FBus.Serializers.Json() :> IBusSerializer) busBuilder

    [<CompiledName("UseWith")>]
    static member useWith options =
        FBus.Serializers.Json(options) :> IBusSerializer |> Builder.withSerializer
