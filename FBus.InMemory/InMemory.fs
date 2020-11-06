module InMemory
open FBus
open FBus.Builder
open System

let private defaultUri = Uri("inmemory://")

let private defaultSerializer = FBus.Serializers.InMemory() :> IBusSerializer

let private defaultContainer = FBus.Containers.InMemory() :> IBusContainer

let useTransport (busBuilder: BusBuilder) =
    busBuilder |> withEndpoint defaultUri
               |> withTransport FBus.Transports.InMemory.Create

let useSerializer (busBuilder: BusBuilder) =
    busBuilder |> withSerializer defaultSerializer

let useContainer (busBuilder: BusBuilder) =
    busBuilder |> withContainer defaultContainer

let waitForCompletion = FBus.Transports.InMemory.WaitForCompletion
