namespace FBus
open System

type IMessageCommand = interface end
type IMessageEvent = IMessageCommand

type IBusInitiator =
    abstract Publish<'t when 't :> IMessageEvent> : msg:'t -> unit
    abstract Send<'t when 't :> IMessageCommand> : client:string -> msg:'t -> unit

type IBusControl =
    inherit IDisposable
    abstract Start: obj -> IBusInitiator
    abstract Stop: unit -> unit

type IBusConversationContext =
    abstract Sender: string
    abstract ConversationId: string
    abstract MessageId: string

type IBusConversation =
    inherit IBusConversationContext
    abstract Publish<'t when 't :> IMessageEvent> : msg:'t -> unit
    abstract Send<'t when 't :> IMessageCommand> : client:string -> msg:'t -> unit
    abstract Reply<'t when 't :> IMessageCommand> : msg:'t -> unit

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

type IBusHook =
    abstract OnError: ctx:IBusConversationContext -> msg:obj -> exn: Exception -> unit

type BusBuilder =
    { Name: string
      IsEphemeral: bool
      IsRecovery: bool
      Uri: Uri
      Container: IBusContainer
      Serializer: IBusSerializer
      Hook: IBusHook option
      Transport: BusBuilder -> (Map<string,string> -> string -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : Map<string, HandlerInfo> }
