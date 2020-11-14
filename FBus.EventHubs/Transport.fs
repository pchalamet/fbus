namespace FBus.Transports
open FBus
open Azure.Messaging.EventHubs
open Azure.Messaging.EventHubs.Producer

type EventHubs(connstr, busname, busConfig: BusConfiguration, msgCallback) =

    interface IBusTransport with
        member _.Publish headers msgType body =
            ()

        member _.Send headers client msgType body =
            ()

        member _.Dispose() =
            ()
