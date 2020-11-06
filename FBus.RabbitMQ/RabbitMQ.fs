module RabbitMQ
open FBus
open FBus.Builder

let private defaultUri = System.Uri("amqp://guest:guest@localhost")

let private defaultTransport (busConfig: BusConfiguration) msgCallback =
    new Transports.RabbitMQ(busConfig, msgCallback) :> IBusTransport

let useTransport (busBuilder: BusBuilder) =
    busBuilder |> withEndpoint defaultUri
               |> withTransport defaultTransport
