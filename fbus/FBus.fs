namespace FBus
open System

type IBusInitiator =
    abstract Publish: msg:'t -> unit
    abstract Send: string -> 't -> unit

type IBusControl =
    inherit IDisposable
    abstract Start: obj -> IBusInitiator
    abstract Stop: unit -> unit

type IBusConversation =
    abstract Publish: msg:'t -> unit
    abstract Send: string -> 't -> unit
    abstract Reply: msg:'t -> unit
    abstract Sender: string
    abstract ConversationId: string
    abstract MessageId: string

type IBusConsumer<'t> =
    abstract Handle: IBusConversation -> 't -> unit

type HandlerInfo =
    { MessageType: Type
      InterfaceType: Type
      ImplementationType: Type }

type IBusContainer =
    abstract Register: HandlerInfo -> unit
    abstract Resolve: obj -> HandlerInfo -> obj

type IBusTransport =
    inherit IDisposable
    abstract Publish: headers:Map<string, string> -> msgType:string -> body:ReadOnlyMemory<byte> -> unit
    abstract Send: headers:Map<string, string> -> target:string -> msgType:string -> body:ReadOnlyMemory<byte> -> unit

type IBusSerializer =
    abstract Serialize: obj -> ReadOnlyMemory<byte>
    abstract Deserialize: Type -> ReadOnlyMemory<byte> -> obj

type BusBuilder =
    { Name: string
      IsEphemeral: bool
      Uri: Uri
      Container: IBusContainer
      Serializer: IBusSerializer
      Transport: BusBuilder -> (Map<string,string> -> string -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : Map<string, HandlerInfo> }
