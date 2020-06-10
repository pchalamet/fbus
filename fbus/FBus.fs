namespace FBus
open System
open System.Threading.Tasks

type IBusTransport =
    inherit IDisposable
    abstract Publish: Type -> string -> Task
    abstract Send: Type -> string -> Task

type IBusSender =
    abstract Publish: 't -> Task
    abstract Send: 't -> Task

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

