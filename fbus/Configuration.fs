module FBus.Configuration
open System

let defaultConfig =
    FBus.Builder.init () |> FBus.RabbitMQ.useTransport
                         |> FBus.Json.useSerializer
