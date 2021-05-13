namespace FBus.Transports
open FBus
open Azure.Messaging.EventHubs
open Azure.Messaging.EventHubs.Producer

type EventHubs(connstr, busname, busConfig: BusConfiguration, msgCallback) =

    interface IBusTransport with
        member _.Publish headers body =
            ()

        member _.Send headers client body =
            ()

        member _.Dispose() =
            ()
