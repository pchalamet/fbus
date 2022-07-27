namespace FBus.Extensions

open System.Runtime.CompilerServices
open FBus

[<Extension; Sealed; AbstractClass>]
type BusBuilderExtensions =
    [<Extension>]
    static member UseRabbitMQWith(busBuilder, uri) =
        busBuilder |> RabbitMQ.useWith uri

    [<Extension>]
    static member UseRabbitMQDefaults(busBuilder) =
        busBuilder |> RabbitMQ.useDefaults
