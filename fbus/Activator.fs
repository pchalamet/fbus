namespace FBus.Container
open FBus

type Activator() =
    interface IBusContainer with
        member this.Register handlerInfo = ()

        member this.Resolve ctx t =
            System.Activator.CreateInstance(t)
