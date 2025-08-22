module FBus.RabbitMQ7
open FBus

let useWith uri busBuilder =
    let createTransport (busConfig: BusConfiguration) msgCallback =
        new Transports.RabbitMQ7(uri, busConfig, msgCallback) :> IBusTransport

    busBuilder |> Builder.withTransport createTransport

let useDefaults busBuilder =
    useWith (System.Uri("amqp://guest:guest@localhost")) busBuilder
