module FBus.Defaults

let init () =

    FBus.Builder.configure() |> FBus.RabbitMQ.useTransport
                             |> FBus.Json.useSerializer
