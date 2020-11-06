module FBus.RabbitMQ
open FBus
open FBus.Builder

let private createTransport (busConfig: BusConfiguration) msgCallback =
    new Transports.RabbitMQ(busConfig, msgCallback) :> IBusTransport

let useTransport (busBuilder: BusBuilder) =
    let uri = System.Uri("amqp://guest:guest@localhost")

    busBuilder |> withEndpoint uri
               |> withTransport createTransport
