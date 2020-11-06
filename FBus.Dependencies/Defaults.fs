module FBus.Defaults

let init () =

    FBus.Builder.init() |> FBus.RabbitMQ.useTransport
                        |> FBus.Json.useSerializer
