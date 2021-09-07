namespace FBus
open FBus

[<AbstractClass; Sealed>]
type RabbitMQ =
    [<CompiledName("UseWith")>]
    static member useWith uri busBuilder =
        let createTransport (busConfig: BusConfiguration) msgCallback =
            new Transports.RabbitMQ(uri, busConfig, msgCallback) :> IBusTransport

        busBuilder |> Builder.withTransport createTransport

    [<CompiledName("UseDefaults")>]
    static member useDefaults busBuilder =
        RabbitMQ.useWith (System.Uri("amqp://guest:guest@localhost")) busBuilder
