namespace FBus.Extensions

open System.Runtime.CompilerServices
open FBus
open System
open FBus.Testing

[<Extension; Sealed; AbstractClass>]
type BusBuilderExtensions =
    [<Extension>]
    static member WithName(busBuilder, name) =
        busBuilder |> Builder.withName name

    [<Extension>]
    static member WithShard(busBuilder, name) =
        busBuilder |> Builder.withShard name

    [<Extension>]
    static member WithContainer(busBuilder, container) =
        busBuilder |> Builder.withContainer container

    [<Extension>]
    static member WithConsumer<'t>(busBuilder) =
        busBuilder |> Builder.withConsumer<'t>

    [<Extension>]
    static member WithConsumer<'t>(busBuilder, action: Action<IBusConversation, 't>) =
        busBuilder |> Builder.withFunConsumer (FuncConvert.FromAction action)

    [<Extension>]
    static member WithRecovery(busBuilder) =
        busBuilder |> Builder.withRecovery

    [<Extension>]
    static member WithHook(busBuilder, hook) =
        busBuilder |> Builder.withHook hook

    [<Extension>]
    static member Build(busBuilder) =
        busBuilder |> Builder.build

    [<Extension>]
    static member UseSession(busBuilder, session: Session) =
        session.Use(busBuilder)
