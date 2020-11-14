module FBus.EventHubs
open FBus
open FBus.Builder

let useWith connstr busname (busBuilder: BusBuilder) =
    let createTransport (busConfig: BusConfiguration) msgCallback =
        new Transports.EventHubs(connstr, busname, busConfig, msgCallback) :> IBusTransport

    busBuilder |> withTransport createTransport

