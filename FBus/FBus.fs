namespace FBus
open System

type IMessageCommand = interface end

type IMessageEvent = interface end

type IMessageKey = 
    abstract Key: string with get

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
    inherit IBusInitiator
    abstract Reply<'t when 't :> IMessageCommand> : msg:'t -> unit

type IBusConsumer<'t> =
    abstract Handle: IBusConversation -> msg:'t -> unit

type IFunConsumer<'t> = IBusConversation -> 't -> unit

type Handler = 
    | Class of implementationType:Type
    | Instance of obj

type HandlerInfo =
    { MessageType: Type
      Handler: Handler }

type IBusContainer =
    abstract Register: HandlerInfo -> unit
    abstract NewScope: context:obj -> IDisposable
    abstract Resolve: context:obj -> HandlerInfo -> obj

type IBusTransport =
    inherit IDisposable
    abstract Publish: headers:Map<string, string> -> msgType:string -> body:ReadOnlyMemory<byte> -> routing:string -> unit
    abstract Send: headers:Map<string, string> -> target:string -> msgType:string -> body:ReadOnlyMemory<byte> -> routing:string -> unit

type IBusSerializer =
    abstract Serialize: msg:obj -> string * ReadOnlyMemory<byte>
    abstract Deserialize: Type -> ReadOnlyMemory<byte> -> obj

type IBusHook =
    abstract OnStart: ctx:IBusInitiator -> unit
    abstract OnStop: ctx:IBusInitiator -> unit
    abstract OnBeforeProcessing: ctx:IBusConversation -> IDisposable
    abstract OnError: ctx:IBusConversation -> msg:obj -> exn: Exception -> unit

type BusConfiguration =
    { Name: string
      IsEphemeral: bool
      ShardName: string option
      IsRecovery: bool
      Container: IBusContainer
      Serializer: IBusSerializer
      Hook: IBusHook option
      Transport: BusConfiguration -> (Map<string,string> -> ReadOnlyMemory<byte> -> unit) -> IBusTransport
      Handlers : Map<string, HandlerInfo> }

[<RequireQualifiedAccessAttribute>]
type BusBuilder =
    { Name: string
      ShardName: string option
      IsEphemeral: bool
      IsRecovery: bool
      Container: IBusContainer option
      Serializer: IBusSerializer option
      Hook: IBusHook option
      Transport: (BusConfiguration -> (Map<string,string> -> ReadOnlyMemory<byte> -> unit) -> IBusTransport) option
      Handlers : Map<string, HandlerInfo> }
