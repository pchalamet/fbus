module FBus.Core

type IBusSender =
    abstract Publish: 't -> Async<Unit>
    abstract Send: 't -> Async<Unit>

type IBusControl =
    abstract Start: unit -> Async<Unit>
    abstract Stop: unit -> Async<Unit>
    abstract Sender: IBusSender

type IConsumer<'t> =
    abstract Handle: 't -> unit

type BusBuilder = class end

val init : unit -> unit

val withEndpoint: uri : System.Uri
                     -> busBuilder: BusBuilder
                     -> BusBuilder

val inline withHandler<'t when 't :> IConsumer<'t>> : busBuilder: BusBuilder
                                                   -> BusBuilder

val build : busBuilder: BusBuilder
         -> IBusControl

