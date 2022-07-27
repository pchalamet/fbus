module FBus.RabbitMQ
open FBus

let useWith uri busBuilder =
    let createTransport (busConfig: BusConfiguration) msgCallback =
        new Transports.RabbitMQ(uri, busConfig, msgCallback) :> IBusTransport

    busBuilder |> Builder.withTransport createTransport

let useDefaults busBuilder =
    useWith (System.Uri("amqp://guest:guest@localhost")) busBuilder
