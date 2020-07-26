namespace FBus
open System

type IBusInitiator =
    abstract Publish: msg:'t -> unit
    abstract Send: client:string -> msg:'t -> unit

type IBusControl =
    inherit IDisposable
    abstract Start: obj -> IBusInitiator
    abstract Stop: unit -> unit

type IBusConversation =
    abstract Publish: msg:'t -> unit
    abstract Send: client:string -> msg:'t -> unit
    abstract Reply: msg:'t -> unit
    abstract Sender: string
    abstract ConversationId: string
    abstract MessageId: string

type IBusConsumer<'t> =
    abstract Handle: IBusConversation -> msg:'t -> unit

type HandlerInfo =
    { MessageType: Type
      InterfaceType: Type
      ImplementationType: Type }

type IBusContainer =
    abstract Register: HandlerInfo -> unit
    abstract Resolve: context:obj -> HandlerInfo -> obj

type IBusTransport =
    inherit IDisposable
    abstract Publish: headers:Map<string, string> -> msgType:string -> body:ReadOnlyMemory<byte> -> unit
    abstract Send: headers:Map<string, string> -> target:string -> msgType:string -> body:ReadOnlyMemory<byte> -> unit

type IBusSerializer =
    abstract Serialize: msg:obj -> ReadOnlyMemory<byte>
    abstract Deserialize: Type -> ReadOnlyMemory<byte> -> obj

type BusBuilder =
    { Name: string
      IsEphemeral: bool
      IsRecovery: bool
      Uri: Uri
      Container: IBusContainer
      Serializer: IBusSerializer
      ExceptionHandler: IBusConversation -> obj -> Exception -> unit
      Transport: BusBuilder -> (Map<string,string> -> string -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : Map<string, HandlerInfo> }
