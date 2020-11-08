module FBus.Testing
open FBus.InMemory

type Context() =
    let ctx = FBus.Transports.InMemoryContext()
    let serializer = FBus.Serializers.InMemory()

    member _.Configure() = 
        FBus.Builder.configure() |> useTransport ctx
                                 |> useContainer
                                 |> FBus.Builder.withSerializer (serializer :> FBus.IBusSerializer)

    member _.WaitForCompletion() = ctx.WaitForCompletion()

    member _.ClearCache() = serializer.Clear()

    interface System.IDisposable with
        member _.Dispose() =
            ()
