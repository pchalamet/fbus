namespace FBus.Containers
open FBus

type Activator() =
    interface IBusContainer with
        member _.Register handlerInfo = ()

        member _.Resolve activationContext handlerInfo =
            match handlerInfo.Handler with
            | Class (implementationType, _) -> System.Activator.CreateInstance(implementationType)
            | Instance (target) -> // FIXME
                                   target
