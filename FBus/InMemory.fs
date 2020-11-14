module FBus.InMemory
open FBus
open FBus.Builder
open System

let useTransport context (busBuilder: BusBuilder) =
    busBuilder |> withTransport (FBus.Transports.InMemory.Create context)

let useSerializer = FBus.Serializers.InMemory() :> IBusSerializer |> withSerializer

let useContainer = FBus.Containers.Activator() :> IBusContainer |> withContainer
