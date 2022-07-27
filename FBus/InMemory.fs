module FBus.InMemory
open FBus

let useTransport context (busBuilder: BusBuilder) =
    busBuilder |> Builder.withTransport (Transports.InMemory.Create context)

let useSerializer = Serializers.InMemory() :> IBusSerializer |> Builder.withSerializer

let useContainer = Containers.Activator() :> IBusContainer |> Builder.withContainer
