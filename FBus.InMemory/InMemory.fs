module InMemory
open FBus
open FBus.InMemory
open FBus.Builder
open System.Collections.Concurrent
open System

let private defaultUri = Uri("inmemory://")

let private defaultTransport (busConfig: BusConfiguration) msgCallback =
    new Transport(busConfig, msgCallback) :> IBusTransport

let private defaultSerializer = Serializer() :> IBusSerializer

let private defaultContainer = Activator() :> IBusContainer

let useTransport (busBuilder: BusBuilder) =
    busBuilder |> withEndpoint defaultUri
               |> withTransport defaultTransport

let useSerializer (busBuilder: BusBuilder) =
    busBuilder |> withSerializer defaultSerializer

let useContainer (busBuilder: BusBuilder) =
    busBuilder |> withContainer defaultContainer

let waitForCompletion = FBus.InMemory.Transport.WaitForCompletion
