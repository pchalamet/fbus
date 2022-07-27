namespace FBus.Extensions

open System.Runtime.CompilerServices
open FBus

[<Extension; Sealed; AbstractClass>]
type BusBuilderExtensions =
    [<Extension>]
    static member UseRabbitMQ(busBuilder, uri) =
        busBuilder |> RabbitMQ.useWith uri

    [<Extension>]
    static member UseRabbitMQ(busBuilder) =
        busBuilder |> RabbitMQ.useDefaults
