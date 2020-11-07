module FBus.QuickStart

let configure () =

    FBus.Builder.configure() |> FBus.RabbitMQ.useDefaults
                             |> FBus.Json.useDefaults
                             |> FBus.InMemory.useContainer
