module FBus.Configuration
open System

let defaultConfig =
    FBus.Builder.init () |> RabbitMQ.useTransport
                         |> Json.useSerializer
