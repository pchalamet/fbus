namespace FBus
open System

type IBusTransport =
    inherit IDisposable
    abstract Publish: Type -> string -> Async<Unit>
    abstract Send: Type -> string -> Async<Unit>

type IBusSender =
    abstract Publish: 't -> Async<Unit>
    abstract Send: 't -> Async<Unit>

type IBusControl =
    abstract Start: obj -> unit
    abstract Stop: unit -> unit

type IConsumer<'t> =
    abstract Handle: 't -> unit

type HandlerInfo = {
    MessageType: System.Type
    InterfaceType: System.Type
    ImplementationType: System.Type
}

type BusBuilder =
    { Name: string option
      Uri : System.Uri
      AutoDelete: bool
      Registrant: HandlerInfo -> unit
      Activator: obj -> System.Type -> obj
      Transport: BusBuilder -> (HandlerInfo -> string -> unit) -> IBusTransport
      Handlers : HandlerInfo list }

