module FBus.Testing
open FBus.InMemory

type Session() =
    let ctx = FBus.Transports.InMemoryContext()
    let serializer = FBus.Serializers.InMemory()

    member _.Use busBuilder = 
        busBuilder |> useTransport ctx
                   |> useContainer
                   |> FBus.Builder.withSerializer (serializer :> FBus.IBusSerializer)

    member _.WaitForCompletion() = ctx.WaitForCompletion()

    member _.ClearCache() = serializer.Clear()
