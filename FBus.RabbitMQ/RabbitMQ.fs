module FBus.RabbitMQ
open FBus
open FBus.Builder

let useTransport (busBuilder: BusBuilder) =
    let uri = System.Uri("amqp://guest:guest@localhost")

    let createTransport (busConfig: BusConfiguration) msgCallback =
        new Transports.RabbitMQ(busConfig, msgCallback) :> IBusTransport

    busBuilder |> withEndpoint uri
               |> withTransport createTransport
