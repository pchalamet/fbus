namespace FBus.Containers
open FBus

type InMemory() =
    interface IBusContainer with
        member _.Register handlerInfo = ()

        member _.Resolve activationContext handlerInfo =
            System.Activator.CreateInstance(handlerInfo.ImplementationType)
