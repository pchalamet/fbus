namespace FBus
open System

type IBusTransport =
    inherit IDisposable
    abstract Publish: Type -> ReadOnlyMemory<byte> -> unit
    abstract Send: string -> Type -> ReadOnlyMemory<byte> -> unit

type IBusSender =
    abstract Publish: 't -> unit
    abstract Send: string -> 't -> unit

type IBusControl =
    inherit IDisposable
    abstract Start: obj -> IBusSender
    abstract Stop: unit -> unit

type IConsumer<'t> =
    abstract Handle: 't -> unit

type HandlerInfo = {
    MessageType: Type
    InterfaceType: Type
    ImplementationType: Type
}

type BusBuilder =
    { Name: string option
      Uri : Uri
      AutoDelete: bool
      Registrant: HandlerInfo -> unit
      Activator: obj -> Type -> obj
      Transport: BusBuilder -> (HandlerInfo -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : HandlerInfo list }

