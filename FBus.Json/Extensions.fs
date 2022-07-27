namespace FBus.Extensions

open System
open System.Runtime.CompilerServices
open FBus

[<Extension; Sealed; AbstractClass>]
type BusBuilderExtensions =
    [<Extension>]
    static member UseJsonDefaults(busBuilder) =
        busBuilder |> Json.useDefaults

    [<Extension>]
    static member UseJsonWith(busBuilder, (initOptions: Action<System.Text.Json.JsonSerializerOptions>)) =
        busBuilder |> Json.useWith (fun options -> initOptions.Invoke(options))
