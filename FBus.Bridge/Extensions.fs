namespace FBus.Extensions

open System.Runtime.CompilerServices
open FBus

[<Extension; Sealed; AbstractClass>]
type BusBuilderExtensions =
    [<Extension>]
    static member UseBridgeDefaults(busBuilder) =
        busBuilder |> Bridge.useDefaults
