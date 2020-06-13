module FBus.Container
open FBus

type Activator() =
    interface IBusContainer with
        member _.Register handlerInfo = ()

        member _.Resolve activationContext handlerInfo =
            System.Activator.CreateInstance(handlerInfo.ImplementationType)
