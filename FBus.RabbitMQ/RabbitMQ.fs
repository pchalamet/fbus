module RabbitMQ
open FBus
open FBus.RabbitMQ
open FBus.Builder
open RabbitMQ.Client
open RabbitMQ.Client.Events

let private defaultUri = System.Uri("amqp://guest:guest@localhost")

let private defaultTransport (busConfig: BusConfiguration) msgCallback =
    new Transport(busConfig, msgCallback) :> IBusTransport

let useTransport (busBuilder: BusBuilder) =
    busBuilder |> withEndpoint defaultUri
               |> withTransport defaultTransport
