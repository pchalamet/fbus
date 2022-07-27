namespace FBus.Extensions

open System
open System.Runtime.CompilerServices
open FBus

[<Extension; Sealed; AbstractClass>]
type BusBuilderExtensions =
    [<Extension>]
    static member UseJson(busBuilder) =
        busBuilder |> Json.useDefaults

    [<Extension>]
    static member UseJson(busBuilder, (initOptions: Action<Text.Json.JsonSerializerOptions>)) =
        busBuilder |> Json.useWith (fun options -> initOptions.Invoke(options))
