namespace FBus.Containers
open FBus


type Activator() =
    interface IBusContainer with
        member _.Register handlerInfo = ()

        member _.NewScope ctx = null

        member _.Resolve activationContext handlerInfo = System.Activator.CreateInstance handlerInfo.Handler
