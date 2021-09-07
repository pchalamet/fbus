module FBus.InMemory
open FBus

let useTransport context (busBuilder: BusBuilder) =
    busBuilder |> Builder.withTransport (FBus.Transports.InMemory.Create context)

let useSerializer = FBus.Serializers.InMemory() :> IBusSerializer |> Builder.withSerializer

let useContainer = FBus.Containers.Activator() :> IBusContainer |> Builder.withContainer
