module FBus.InMemory
open FBus
open FBus.Builder
open System

let private defaultSerializer = FBus.Serializers.InMemory() :> IBusSerializer

let private defaultContainer = FBus.Containers.Activator() :> IBusContainer

let useTransport (busBuilder: BusBuilder) =
    let uri = Guid.NewGuid() |> sprintf "in-memory://%A" |> Uri
    busBuilder |> withEndpoint uri
               |> withTransport FBus.Transports.InMemory.Create

let useSerializer = withSerializer defaultSerializer

let useContainer = withContainer defaultContainer
