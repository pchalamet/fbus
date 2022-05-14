namespace FBus.Containers
open FBus


type Activator() =
    interface IBusContainer with
        member _.Register handlerInfo = ()

        member _.NewScope ctx = null

        member _.Resolve activationContext handlerInfo =
            match handlerInfo.Handler with
            | Class implementationType -> System.Activator.CreateInstance(implementationType)
            | Instance target -> target
