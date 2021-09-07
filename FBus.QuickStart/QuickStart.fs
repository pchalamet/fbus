namespace FBus

[<AbstractClass; Sealed>]
type QuickStart =
    [<CompiledName("Configure")>]
    static member configure () =
        Builder.configure() |> RabbitMQ.useDefaults
                            |> Json.useDefaults
                            |> InMemory.useContainer
