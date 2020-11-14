module FBus.RabbitMQ
open FBus
open FBus.Builder


let useWith uri (busBuilder: BusBuilder) =
    let createTransport (busConfig: BusConfiguration) msgCallback =
        new Transports.RabbitMQ(uri, busConfig, msgCallback) :> IBusTransport

    busBuilder |> withTransport createTransport

let useDefaults =
    System.Uri("amqp://guest:guest@localhost") |> useWith

